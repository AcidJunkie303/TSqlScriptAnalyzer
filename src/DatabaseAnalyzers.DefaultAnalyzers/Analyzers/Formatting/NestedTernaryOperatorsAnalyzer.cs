using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class NestedTernaryOperatorsAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public NestedTernaryOperatorsAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var expression in _script.ParsedScript.GetChildren<IIfCall>(recursive: true))
        {
            Analyze(expression);
        }
    }

    private void Analyze(IIfCall expression)
    {
        if (!expression.GetParents(_script.ParentFragmentProvider).OfType<IIfCall>().Any())
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(expression) ?? DatabaseNames.Unknown;
        var fullObjectName = expression.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5033",
            IssueType.Warning,
            "Ternary operators should not be nested",
            "Ternary operators like `IIF` should not be nested.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
