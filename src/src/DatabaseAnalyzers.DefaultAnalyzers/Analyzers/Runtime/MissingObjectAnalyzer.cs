using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
public sealed class MissingObjectAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5044Settings>();
        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName);

        var cteNamesByBatch = context.Scripts
            .SelectMany(a => a.ParsedScript.Batches)
            .ToDictionary(a => a, CteExtractor.ExtractCteNames);

        var cteBatchInformationProvider = new CteBatchInformationProvider(cteNamesByBatch);

        AnalyzeProcedureCalls(context, settings, databasesByName);
        AnalyzeTableReferences(context, settings, databasesByName, cteBatchInformationProvider);
        AnalyzeColumnReferences(context, settings, databasesByName, cteBatchInformationProvider);
    }

    private static void AnalyzeColumnReferences(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, CteBatchInformationProvider cteBatchInformationProvider)
    {
        var columnReferencesAndScripts = context.Scripts
            .SelectMany(a => a
                .ParsedScript
                .GetChildren<ColumnReferenceExpression>(recursive: true)
                .Select(b => (ColumnReference: b, Script: a))
            );

        foreach (var (columnReference, script) in columnReferencesAndScripts)
        {
            AnalyzeColumnReference(context, settings, databasesByName, script, columnReference, cteBatchInformationProvider);
        }
    }

    private static void AnalyzeColumnReference(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, IScriptModel script, ColumnReferenceExpression columnReference, CteBatchInformationProvider cteBatchInformationProvider)
    {
        var resolver = new TableColumnResolver(new IssueReporter(), script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);
        var column = resolver.Resolve(columnReference);
        if (column is null)
        {
            return;
        }

        var isTempTable = column.TableName.StartsWith('#');
        if (isTempTable)
        {
            return;
        }

        var batch = script.ParentFragmentProvider.GetParents(columnReference).OfType<TSqlBatch>().FirstOrDefault();
        if (batch is not null)
        {
            if (cteBatchInformationProvider.IsCte(batch, column.TableName))
            {
                return;
            }
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

    private static void AnalyzeTableReferences(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, CteBatchInformationProvider cteBatchInformationProvider)
    {
        var namedTableReferencesAndScripts = context.Scripts
            .SelectMany(a => a
                .ParsedScript
                .GetChildren<NamedTableReference>(recursive: true)
                .Select(b => (NamedTableReference: b, Script: a))
            );

        foreach (var (tableReference, script) in namedTableReferencesAndScripts)
        {
            AnalyzeTableReference(context, settings, databasesByName, script, tableReference, cteBatchInformationProvider);
        }
    }

    private static void AnalyzeTableReference(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, IScriptModel script, NamedTableReference tableReference, CteBatchInformationProvider cteBatchInformationProvider)
    {
        var tableName = tableReference.SchemaObject.BaseIdentifier.Value;
        var isTempTable = tableName.StartsWith('#');
        if (isTempTable)
        {
            return;
        }

        var batch = script.ParentFragmentProvider.GetParents(tableReference).OfType<TSqlBatch>().FirstOrDefault();
        if (batch is not null)
        {
            if (cteBatchInformationProvider.IsCte(batch, tableReference.SchemaObject.BaseIdentifier.Value))
            {
                return;
            }
        }

        var schemaName = tableReference.SchemaObject.SchemaIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? context.DefaultSchemaName;
        var databaseName = tableReference.SchemaObject.DatabaseIdentifier?.Value.NullIfEmptyOrWhiteSpace()
                           ?? script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(tableReference)
                           ?? DatabaseNames.Unknown;

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

    private sealed class CteBatchInformationProvider
    {
        private readonly IReadOnlyDictionary<TSqlBatch, FrozenSet<string>> _cteNamesByBatch;

        public CteBatchInformationProvider(IReadOnlyDictionary<TSqlBatch, FrozenSet<string>> cteNamesByBatch)
        {
            _cteNamesByBatch = cteNamesByBatch;
        }

        public bool IsCte(TSqlBatch? batch, string? tableName)
        {
            if (batch is null || tableName.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (!_cteNamesByBatch.TryGetValue(batch, out var cteNames))
            {
                return false;
            }

            return cteNames.Contains(tableName);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5044",
            IssueType.Warning,
            "Missing Object",
            "The referenced {0} `{1}` was not found.",
            ["Object type name", "Expression"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
