using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

public sealed class BannedFunctionAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5040Settings _settings;

    public BannedFunctionAnalyzer(IScriptAnalysisContext context, Aj5040Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        var schemaObjectFunctions = _script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true);
        var globalFunctions = _script.ParsedScript.GetChildren<GlobalFunctionTableReference>(recursive: true);
        var scalarFunctions = _script.ParsedScript
            .GetChildren<SelectScalarExpression>(recursive: true)
            .Where(static a => a.Expression is FunctionCall);

        AnalyzeFunctionInvocation(
            globalFunctions,
            static x => x.Name.Value,
            static x => x.Name.GetCodeRegion());

        AnalyzeFunctionInvocation(
            scalarFunctions,
            static x => ((FunctionCall) x.Expression).FunctionName.Value,
            static x => ((FunctionCall) x.Expression).FunctionName.GetCodeRegion());

        AnalyzeFunctionInvocation(
            schemaObjectFunctions,
            static x => x.SchemaObject.Identifiers.Select(static a => a.Value).StringJoin('.'),
            static x => x.SchemaObject.GetCodeRegion());
    }

    private void AnalyzeFunctionInvocation<T>(IEnumerable<T> functions, Func<T, string> functionNameGetter, Func<T, CodeRegion> nameLocationGetter)
        where T : TSqlFragment
    {
        foreach (var function in functions)
        {
            AnalyzeFunctionInvocation(function, functionNameGetter, nameLocationGetter);
        }
    }

    private void AnalyzeFunctionInvocation<T>(T function, Func<T, string> functionNameGetter, Func<T, CodeRegion> nameLocationGetter)
        where T : TSqlFragment
    {
        var functionName = functionNameGetter(function);
        if (functionName.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!_settings.BanReasonByFunctionName.TryGetValue(functionName, out var reason))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(function) ?? DatabaseNames.Unknown;
        var fullObjectName = function.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, nameLocationGetter(function), functionName, reason);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5040",
            IssueType.Warning,
            "Usage of banned function",
            "The function `{0}` is banned. {1}",
            ["Function name", "Reason"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
