using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.DataTypes;

public sealed class Aj5006SettingsProvider : DiagnosticSettingsProviderBase<Aj5006SettingsRaw, Aj5006Settings>
{
    public override string DiagnosticId => "AJ5006";
}
