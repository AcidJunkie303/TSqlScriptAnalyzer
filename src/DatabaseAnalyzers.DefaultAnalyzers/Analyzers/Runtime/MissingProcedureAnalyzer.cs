using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingProcedureAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5044Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingProcedureAnalyzer(IGlobalAnalysisContext context, IIssueReporter issueReporter, Aj5044Settings settings, IObjectProvider objectProvider)
    {
        _context = context;
        _issueReporter = issueReporter;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var procedures = _objectProvider.DatabasesByName
            .SelectMany(static a => a.Value.SchemasByName.Values)
            .SelectMany(static a => a.ProceduresByName.Values);

        foreach (var procedure in procedures)
        {
            AnalyzeCalls(_objectProvider.DatabasesByName, procedure);
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

        _issueReporter.Report(SharedDiagnosticDefinitions.MissingObject, databaseName, callingProcedure.RelativeScriptFilePath, callingProcedure.FullName, procedureObjectName.GetCodeRegion(),
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
