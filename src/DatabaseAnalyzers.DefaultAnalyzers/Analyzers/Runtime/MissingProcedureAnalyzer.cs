using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingProcedureAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly Aj5044Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingProcedureAnalyzer(IGlobalAnalysisContext context, Aj5044Settings settings)
    {
        _context = context;
        _settings = settings;
    }

    public void Analyze()
    {
        var databasesByName = new DatabaseObjectExtractor(_context.IssueReporter)
            .Extract(_context.ErrorFreeScripts, _context.DefaultSchemaName);
        var procedures = databasesByName
            .SelectMany(a => a.Value.SchemasByName.Values)
            .SelectMany(a => a.ProceduresByName.Values);

        foreach (var procedure in procedures)
        {
            AnalyzeCalls(databasesByName, procedure);
        }
    }

    private void AnalyzeCalls(IReadOnlyDictionary<string, DatabaseInformation> databasesByName, ProcedureInformation callingProcedure)
    {
        if (callingProcedure.CreationStatement.StatementList is null)
        {
            return;
        }

        foreach (var executeStatement in callingProcedure.CreationStatement.StatementList.GetChildren<ExecuteStatement>(recursive: true))
        {
            AnalyzeCall(databasesByName, callingProcedure, executeStatement);
        }
    }

    private void AnalyzeCall(IReadOnlyDictionary<string, DatabaseInformation> databasesByName, ProcedureInformation callingProcedure, ExecuteStatement executeStatement)
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
        var schemaName = procedureObjectName.SchemaIdentifier?.Value.NullIfEmptyOrWhiteSpace() ?? _context.DefaultSchemaName;
        var procedureName = procedureObjectName.BaseIdentifier.Value;

        var schema = databasesByName.GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

        var calledProcedure = schema?.ProceduresByName.GetValueOrDefault(procedureName)
                              ?? (IDatabaseObject?) schema?.SynonymsByName.GetValueOrDefault(procedureName);

        if (calledProcedure is not null)
        {
            return;
        }

        var fullStoredProcedureName = $"{databaseName}.{schemaName}.{procedureName}";
        if (IsIgnored(fullStoredProcedureName))
        {
            return;
        }

        _context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, databaseName, callingProcedure.RelativeScriptFilePath, callingProcedure.FullName, procedureObjectName.GetCodeRegion(),
            "procedure", fullStoredProcedureName);
    }

    private bool IsIgnored(string fullObjectName)
    {
        if (_settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        return _settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
    }
}
