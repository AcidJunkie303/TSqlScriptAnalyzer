using System.ComponentModel;
using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultServices.Settings;

internal sealed class AstServiceSettingsRaw : IRawSettings<AstServiceSettings>
{
    public IReadOnlyList<string?>? ExcludedFilePathPatterns { get; set; }

    public AstServiceSettings ToSettings()
    {
        if (ExcludedFilePathPatterns is null)
        {
            return AstServiceSettings.Default;
        }

        var patterns = ExcludedFilePathPatterns
            .WhereNotNull()
            .Select(static a => a.Trim().ToRegexWithSimpleWildcards(caseSensitive: false, compileRegex: true))
            .ToImmutableArray();

        return new AstServiceSettings(patterns);
    }
}

internal sealed record AstServiceSettings(
    [property: Description("Script file path patterns to exclude. Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Regex> ExcludedFilePathPatterns
) : ISettings<AstServiceSettings>
{
    public static AstServiceSettings Default { get; } = new([]);
    public static string DiagnosticId => "AJ5003";
}
