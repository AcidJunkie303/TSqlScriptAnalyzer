using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class Aj5017SettingsProvider : DiagnosticSettingsProviderBase<Aj5017SettingsRaw, Aj5017Settings>
{
    public override string DiagnosticId => "Aj5017";
}
