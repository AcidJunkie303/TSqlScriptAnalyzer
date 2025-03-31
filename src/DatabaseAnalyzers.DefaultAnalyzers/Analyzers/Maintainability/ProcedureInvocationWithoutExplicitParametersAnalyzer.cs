using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class ProcedureInvocationWithoutExplicitParametersAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5059Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ProcedureInvocationWithoutExplicitParametersAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5059Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        foreach (var procedureCall in _script.ParsedScript.GetChildren<ExecutableProcedureReference>(recursive: true))
        {
            AnalyzeProcedureCall(procedureCall);
        }
    }

    private static bool IsObjectNameIgnored(Aj5059Settings settings, string objectName)
        => settings
            .IgnoredProcedureNamePatterns
            .Any(a => a.IsMatch(objectName));

    private void AnalyzeProcedureCall(ExecutableProcedureReference procedureCall)
    {
        if (procedureCall.Parameters.Count == 0)
        {
            return;
        }

        var procedureName = procedureCall.ProcedureReference?.ProcedureReference?.Name;
        if (procedureName is null)
        {
            return;
        }

        var schemaName = procedureName.SchemaIdentifier?.Value ?? _context.DefaultSchemaName;
        var pureProcedureName = procedureName.BaseIdentifier.Value;
        if (pureProcedureName.IsNullOrWhiteSpace())
        {
            return;
        }

        var twoPartProcedureName = $"{schemaName}.{pureProcedureName}";
        if (IsObjectNameIgnored(_settings, twoPartProcedureName))
        {
            return;
        }

        if (procedureCall.Parameters.All(static a => a.Variable is not null))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(procedureCall) ?? DatabaseNames.Unknown;
        var fullObjectName = procedureCall.TryGetFirstClassObjectName(_context, _script);
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, procedureCall.GetCodeRegion(), twoPartProcedureName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5059",
            IssueType.Warning,
            "Procedure Call without explicit Parameter Names",
            "The procedure invocation of `{0}` does not specify explicit parameter names for all arguments.",
            ["Invoked procedure name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
