using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DataTypes;
using DatabaseAnalyzers.DefaultAnalyzers.Configuration;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class Aj5015SettingsProvider : DiagnosticSettingsProviderBase<Aj5015SettingsRaw, Aj5015Settings>
{
    public override string DiagnosticId => "Aj5015";
}
