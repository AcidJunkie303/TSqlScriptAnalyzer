using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
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

        AnalyzeProcedureCalls(context, settings, databasesByName);
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

        var schema = databasesByName.GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

        var calledProcedure = schema?.ProceduresByName.GetValueOrDefault(procedureName)
                              ?? (IDatabaseObject?) schema?.SynonymsByName.GetValueOrDefault(procedureName);

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

    private static bool IsIgnored(Aj5044Settings settings, string procedureName)
        => settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(procedureName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5044",
            IssueType.Warning,
            "Missing Object",
            "The referenced `{0}` `{1}` was not found.",
            ["Object Type Name", "Expression"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
