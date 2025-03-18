using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Security;

public sealed class WeakHashingAlgorithmAnalyzer : IScriptAnalyzer
{
    private static readonly FrozenSet<string> InsecureHashingAlgorithms = new[]
    {
        "MD2",
        "MD4",
        "MD5",
        "SHA",
        "SHA1"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public WeakHashingAlgorithmAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var functionCall in _script.ParsedScript.GetChildren<FunctionCall>(recursive: true))
        {
            Analyze(functionCall);
        }
    }

    private void Analyze(FunctionCall functionCall)
    {
        if (functionCall.Parameters.Count != 2)
        {
            return;
        }

        if (!functionCall.FunctionName.Value.EqualsOrdinalIgnoreCase("HASHBYTES"))
        {
            return;
        }

        var algorithmArgument = functionCall.Parameters[0];
        var hashAlgorithmName = algorithmArgument.GetSql();

        if (!InsecureHashingAlgorithms.Contains(hashAlgorithmName))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(algorithmArgument) ?? DatabaseNames.Unknown;
        var fullObjectName = algorithmArgument.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, algorithmArgument.GetCodeRegion(), hashAlgorithmName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5020",
            IssueType.Warning,
            "Usage of weak hashing algorithm",
            "The hashing algorithm `{0}` is considered weak and should not be used anymore.",
            ["Algorithm name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
