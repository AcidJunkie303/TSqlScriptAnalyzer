using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class ProcedureInvocationWithMissingParameterValuesAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly IScriptModel _script;
    private readonly Aj5062Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ProcedureInvocationWithMissingParameterValuesAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, IObjectProvider objectProvider, Aj5062Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _objectProvider = objectProvider;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        if (!IsAnalysisNecessary())
        {
            return;
        }

        foreach (var procedureCall in _script.ParsedScript.GetChildren<ExecutableProcedureReference>(recursive: true))
        {
            AnalyzeProcedureCall(procedureCall);
        }
    }

    private bool IsAnalysisNecessary() => _settings.ValueRequiredForNullableParameters || _settings.ValueRequiredForParametersWithDefaultValue;

    private void AnalyzeProcedureCall(ExecutableProcedureReference procedureCall)
    {
        if (procedureCall.Parameters.Count == 0)
        {
            return;
        }

        // if not all procedure parameter names are specified, we cannot analyze it
        // there's another analyzer for this case
        if (procedureCall.Parameters.All(static a => a.Variable is null))
        {
            return;
        }

        var procedureName = procedureCall.ProcedureReference?.ProcedureReference?.Name;
        if (procedureName is null)
        {
            return;
        }

        var (databaseName, schemaName, pureProcedureName) = procedureName.GetIdentifierParts();
        databaseName ??= _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(procedureCall) ?? _script.DatabaseName;
        schemaName ??= _context.DefaultSchemaName;

        var procedure = _objectProvider.GetProcedure(databaseName, schemaName, pureProcedureName);
        if (procedure is null)
        {
            return;
        }

        var parametersByName = procedureCall.Parameters
            .Where(static a => a.Variable?.Name is not null)
            .ToDictionary(
                static a => a.Variable!.Name,
                static a => a,
                StringComparer.OrdinalIgnoreCase);

        var parametersToReport = new List<string>();

        foreach (var parameter in procedure.Parameters)
        {
            if (parametersByName.ContainsKey(parameter.Name))
            {
                continue;
            }

            if (!IsArgumentRequired(parameter))
            {
                continue;
            }

            parametersToReport.Add(parameter.Name);
        }

        if (parametersToReport.Count == 0)
        {
            return;
        }

        var fullObjectName = procedureCall.TryGetFirstClassObjectName(_context, _script);
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, procedureCall.GetCodeRegion(),
            $"{databaseName}.{schemaName}.{pureProcedureName}", parametersToReport.StringJoin(", "));
    }

    private bool IsArgumentRequired(ParameterInformation parameter)
    {
        if (parameter is { IsNullable: false, HasDefaultValue: false })
        {
            return true;
        }

        if (_settings.ValueRequiredForParametersWithDefaultValue && parameter.HasDefaultValue)
        {
            return true;
        }

        if (_settings.ValueRequiredForNullableParameters && parameter.IsNullable)
        {
            return true;
        }

        return false;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5062",
            IssueType.Warning,
            "Procedure Invocation without explicit Parameter Names",
            "The procedure invocation of `{0}` does not provide a value for the parameter(s) `{1}`.",
            ["Invoked procedure name", "Parameter name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
