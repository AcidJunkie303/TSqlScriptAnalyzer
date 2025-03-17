namespace DatabaseAnalyzer.Core.Models;

internal sealed record AnalyzerTypes(
    IReadOnlyList<Type> ScriptAnalyzers,
    IReadOnlyList<Type> GlobalAnalyzers
);
