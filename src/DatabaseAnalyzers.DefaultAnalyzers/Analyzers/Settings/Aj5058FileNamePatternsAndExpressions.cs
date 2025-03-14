using System.Text.RegularExpressions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

internal sealed record Aj5058FileNamePatternsAndExpressions(
    IReadOnlyList<Regex> Expressions,
    string FlatFileNamePatterns,
    string ShortenedStatementName
);
