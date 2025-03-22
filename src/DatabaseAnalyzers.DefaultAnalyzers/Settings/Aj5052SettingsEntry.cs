using DatabaseAnalyzers.DefaultAnalyzers.Model;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

public sealed class Aj5052SettingsEntryRaw
{
    public IndexProperties? IndexProperties { get; set; }
    public string? Pattern { get; set; }
}

public sealed record Aj5052SettingsEntry(
    IndexProperties IndexProperties,
    string Pattern
);
