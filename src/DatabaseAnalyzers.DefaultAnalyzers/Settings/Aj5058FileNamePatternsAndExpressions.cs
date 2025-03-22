using System.Text.RegularExpressions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

public sealed record Aj5058FileNamePatternsAndExpressions(
    IReadOnlyList<Regex> Expressions,
    string FlatFileNamePatterns,
    string ShortenedStatementName
);
