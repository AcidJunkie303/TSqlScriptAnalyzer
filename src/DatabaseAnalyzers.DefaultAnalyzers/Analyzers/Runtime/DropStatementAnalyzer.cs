using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class DropStatementAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5058Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public DropStatementAnalyzer(IScriptAnalysisContext context, Aj5058Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<TSqlStatement>(recursive: true))
        {
            AnalyzeStatement(statement);
        }
    }

    private void AnalyzeStatement(TSqlStatement statement)
    {
        var expressionsAndPatterns = _settings.AllowedInFilesByDropStatementType.GetValueOrDefault(statement.GetType());
        if (expressionsAndPatterns is null)
        {
            return;
        }

        var (expressions, allowedFileNamePatterns, shortStatementName) = expressionsAndPatterns;
        if (expressions.Any(a => a.IsMatch(_script.RelativeScriptFilePath)))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion(), shortStatementName, allowedFileNamePatterns);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5058",
            IssueType.Warning,
            "Drop statement in non-allowed file",
            "The drop statement `{0}` is not allowed in this file. It is only allowed in files with the following name pattern(s): `{1}`",
            ["Drop Statement Type", "Allowed file name patterns"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
