using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzer.Core.Configuration;

internal sealed class PluginsSettingsRaw
{
    public IReadOnlyCollection<string?>? PluginDirectoryPaths { get; set; }

    public PluginsSettings ToSettings() => new
    (
        PluginDirectoryPaths
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpaceOnly()
            .Select(static a => a.Trim())
            .ToImmutableArray()
    );
}

public sealed record PluginsSettings(
    IReadOnlyCollection<string> PluginDirectoryPaths
)
{
    public static PluginsSettings Default { get; } = new([]);
}
