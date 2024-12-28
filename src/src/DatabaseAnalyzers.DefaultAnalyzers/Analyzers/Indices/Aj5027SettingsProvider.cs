using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class Aj5027SettingsProvider : DiagnosticSettingsProviderBase<Aj5027SettingsRaw, Aj5027Settings>
{
    public override string DiagnosticId => "AJ5027";
}
