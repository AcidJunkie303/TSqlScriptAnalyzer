using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class Aj5038SettingsProvider : DiagnosticSettingsProviderBase<Aj5038SettingsRaw, Aj5038Settings>
{
    public override string DiagnosticId => "AJ5038";
}
