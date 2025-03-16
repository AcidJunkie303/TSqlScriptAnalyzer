using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MultipleVariableDeclarationAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public MultipleVariableDeclarationAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<DeclareVariableStatement>(recursive: true))
        {
            Analyze(statement);
        }
    }

    private void Analyze(DeclareVariableStatement statement)
    {
        if (statement.Declarations.Count <= 1)
        {
            return;
        }

        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5024",
            IssueType.Formatting,
            "Multiple variable declaration on same line",
            "Multiple variables should be declared on separate lines using a separate `DECLARE` statement.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
