using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
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

    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var functionCall in script.ParsedScript.GetChildren<FunctionCall>(recursive: true))
        {
            Analyze(context, script, functionCall);
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, FunctionCall functionCall)
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

        var databaseName = algorithmArgument.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = algorithmArgument.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, algorithmArgument.GetCodeRegion(), hashAlgorithmName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5020",
            IssueType.Warning,
            "Usage of weak hashing algorithm",
            "The hashing algorithm '{0}' is considered weak and should not be used anymore."
        );
    }
}
