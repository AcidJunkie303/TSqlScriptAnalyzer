using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class Aj5030SettingsProvider : DiagnosticSettingsProviderBase<Aj5030SettingsRaw, Aj5030Settings>
{
    public override string DiagnosticId => "AJ5030";
}
