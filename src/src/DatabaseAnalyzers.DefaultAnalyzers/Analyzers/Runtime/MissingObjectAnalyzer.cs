using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingObjectAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5044Settings>();
        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName);

        AnalyzeProcedureCalls(context, settings, databasesByName);
        AnalyzeTableReferences(context, settings, databasesByName);
        AnalyzeColumnReferences(context, settings, databasesByName);
    }

    private static void AnalyzeColumnReferences(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var columnReferencesAndScripts = context.Scripts
            .SelectMany(a => a
                .ParsedScript
                .GetChildren<ColumnReferenceExpression>(recursive: true)
                .Select(b => (ColumnReference: b, Script: a))
            );

        foreach (var (columnReference, script) in columnReferencesAndScripts)
        {
            AnalyzeColumnReference(context, settings, databasesByName, script, columnReference);
        }
    }

    private static void AnalyzeColumnReference(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, IScriptModel script, ColumnReferenceExpression columnReference)
    {
        var resolver = new TableColumnResolver(new IssueReporter(), script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);
        var column = resolver.Resolve(columnReference);
        if (column is null)
        {
            return;
        }

        if (DoesTableColumnOrViewColumnExist(databasesByName, column.DatabaseName, column.SchemaName, column.TableName, column.ColumnName))
        {
            return;
        }

        if (IsIgnored(settings, column.FullName))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(columnReference) ?? DatabaseNames.Unknown;
        var fullObjectName = columnReference.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), "column", column.FullName);
    }

    private static void AnalyzeTableReferences(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var namedTableReferencesAndScripts = context.Scripts
            .SelectMany(a => a
                .ParsedScript
                .GetChildren<NamedTableReference>(recursive: true)
                .Select(b => (NamedTableReference: b, Script: a))
            );

        foreach (var (tableReference, script) in namedTableReferencesAndScripts)
        {
            AnalyzeTableReference(context, settings, databasesByName, script, tableReference);
        }
    }

    private static void AnalyzeTableReference(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, IScriptModel script, NamedTableReference tableReference)
    {
        var databaseName = tableReference.SchemaObject.DatabaseIdentifier?.Value.NullIfEmptyOrWhiteSpace()
                           ?? script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(tableReference)
                           ?? DatabaseNames.Unknown;
        var schemaName = tableReference.SchemaObject.SchemaIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? context.DefaultSchemaName;
        var tableName = tableReference.SchemaObject.BaseIdentifier.Value;

        if (DoesTableOrViewExist(databasesByName, databaseName, schemaName, tableName))
        {
            return;
        }

        var fullTableName = $"{databaseName}.{schemaName}.{tableName}";
        if (IsIgnored(settings, fullTableName))
        {
            return;
        }

        var fullObjectName = tableReference.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, tableReference.GetCodeRegion(), "table", fullTableName);
    }

    private static void AnalyzeProcedureCalls(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var procedures = databasesByName
            .SelectMany(a => a.Value.SchemasByName.Values)
            .SelectMany(a => a.ProceduresByName.Values);

        foreach (var procedure in procedures)
        {
            AnalyzeProcedureCalls(context, settings, databasesByName, procedure);
        }
    }

    private static void AnalyzeProcedureCalls(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, ProcedureInformation callingProcedure)
    {
        if (callingProcedure.CreationStatement.StatementList is null)
        {
            return;
        }

        foreach (var executeStatement in callingProcedure.CreationStatement.StatementList.GetChildren<ExecuteStatement>(recursive: true))
        {
            AnalyzeProcedureCall(context, settings, databasesByName, callingProcedure, executeStatement);
        }
    }

    private static void AnalyzeProcedureCall(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, ProcedureInformation callingProcedure, ExecuteStatement executeStatement)
    {
        if (executeStatement.ExecuteSpecification?.ExecutableEntity is not ExecutableProcedureReference executableProcedureReference)
        {
            return;
        }

        var procedureObjectName = executableProcedureReference.ProcedureReference?.ProcedureReference?.Name;
        if (procedureObjectName is null)
        {
            return;
        }

        var databaseName = procedureObjectName.DatabaseIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? callingProcedure.DatabaseName;
        var schemaName = procedureObjectName.SchemaIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? context.DefaultSchemaName;
        var procedureName = procedureObjectName.BaseIdentifier.Value;

        var calledProcedure = databasesByName.GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName)
            ?.ProceduresByName.GetValueOrDefault(procedureName);

        if (calledProcedure is not null)
        {
            return;
        }

        var calledProcedureName = $"{databaseName}.{schemaName}.{procedureName}";
        if (IsIgnored(settings, calledProcedureName))
        {
            return;
        }

        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, callingProcedure.RelativeScriptFilePath, callingProcedure.FullName, procedureObjectName.GetCodeRegion(), "procedure", calledProcedureName);
    }

    private static bool DoesTableColumnOrViewColumnExist(IReadOnlyDictionary<string, DatabaseInformation> databasesByName, string databaseName, string schemaName, string tableOrViewName, string columnName)
    {
        var schema = databasesByName.GetValueOrDefault(databaseName)?.SchemasByName.GetValueOrDefault(schemaName);
        if (schema is null)
        {
            return false;
        }

        var table = schema.TablesByName.GetValueOrDefault(tableOrViewName);
        if (table is not null)
        {
            if (table.Columns.Any(a => columnName.EqualsOrdinalIgnoreCase(a.ObjectName)))
            {
                return true;
            }
        }

        var view = schema.ViewsByName.GetValueOrDefault(tableOrViewName);
        if (view is not null)
        {
            if (view.Columns.Any(columnName.EqualsOrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DoesTableOrViewExist(IReadOnlyDictionary<string, DatabaseInformation> databasesByName, string databaseName, string schemaName, string tableOrViewName)
    {
        var schema = databasesByName.GetValueOrDefault(databaseName)?.SchemasByName.GetValueOrDefault(schemaName);
        if (schema is null)
        {
            return false;
        }

        return schema.TablesByName.ContainsKey(tableOrViewName) || schema.ViewsByName.ContainsKey(tableOrViewName);
    }

    private static bool IsIgnored(Aj5044Settings settings, string procedureName)
        => settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(procedureName));

    private sealed class IssueReporter : IIssueReporter
    {
        public void Report(IDiagnosticDefinition rule, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
        {
        }

        public IReadOnlyList<IIssue> Issues => [];
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5044",
            IssueType.Warning,
            "Missing Object",
            "The referenced {0} '{1}' was not found.",
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
