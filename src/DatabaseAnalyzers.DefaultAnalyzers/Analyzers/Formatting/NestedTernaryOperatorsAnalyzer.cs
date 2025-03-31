using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class NestedTernaryOperatorsAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public NestedTernaryOperatorsAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
    }

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
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, expression.GetCodeRegion());
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
