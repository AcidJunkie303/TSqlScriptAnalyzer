using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutOrAlterAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ObjectCreationWithoutOrAlterAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        AnalyzeFragments(_script.ParsedScript.GetChildren<CreateViewStatement>(recursive: true));
        AnalyzeFragments(_script.ParsedScript.GetChildren<CreateProcedureStatement>(recursive: true));
        AnalyzeFragments(_script.ParsedScript.GetChildren<CreateFunctionStatement>(recursive: true));
        AnalyzeFragments(_script.ParsedScript.GetChildren<CreateTriggerStatement>(recursive: true));
    }

    private void AnalyzeFragments(IEnumerable<TSqlFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            var fullObjectName = fragment.TryGetFirstClassObjectName(_context, _script);
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? DatabaseNames.Unknown;
            Report(databaseName, _script.RelativeScriptFilePath, fullObjectName, fragment);
        }
    }

    private void Report(string databaseName, string relativeScriptFilePath, string? fullObjectName, TSqlFragment fragment)
        => _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, relativeScriptFilePath, fullObjectName, fragment.GetCodeRegion());

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5009",
            IssueType.Warning,
            "Object creation without `OR ALTER` clause",
            "Object creation without `OR ALTER` clause.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
