using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class Aj5026SettingsProvider : DiagnosticSettingsProviderBase<Aj5026SettingsRaw, Aj5026Settings>
{
    public override string DiagnosticId => "AJ5026";
}
