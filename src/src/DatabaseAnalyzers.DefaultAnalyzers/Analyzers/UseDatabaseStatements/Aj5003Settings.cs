using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UseDatabaseStatements;

public sealed class Aj5003SettingsRaw
{
    public IReadOnlyList<string?>? ExcludedFilePathPatterns { get; set; }

    public Aj5003Settings ToSettings()
    {
        if (ExcludedFilePathPatterns is null)
        {
            return Aj5003Settings.Default;
        }

        var patterns = ExcludedFilePathPatterns
            .WhereNotNull()
            .Select(a => a.Trim().ToRegexWithSimpleWildcards(compileRegex: true))
            .ToImmutableArray();

        return new Aj5003Settings(patterns);
    }
}

public sealed record Aj5003Settings(IReadOnlyList<Regex> ExcludedFilePathPatterns)
{
    public static Aj5003Settings Default { get; } = new([]);
}
