using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzer.Common.Settings;

public sealed class AstServiceSettingsRaw : IRawSettings<AstServiceSettings>
{
    public IReadOnlyDictionary<string, IReadOnlyCollection<int>?>? EnumerationValueParameterIndicesByFunctionName { get; set; }

    // IsChildOfFunctionEnumParameter
    public AstServiceSettings ToSettings() => new
    (
        EnumerationValueParameterIndicesByFunctionName
            ?.Where(a => a.Value?.Count > 0)
            .ToFrozenDictionary(
                a => a.Key,
                a => a.Value!.ToFrozenSet(),
                StringComparer.OrdinalIgnoreCase)
        ?? AstServiceSettings.Default.EnumerationValueParameterIndicesByFunctionName
    );
}

public sealed record AstServiceSettings(
    [Description("A dictionary where the key represents the function name the value is an array of parameter indices which are enum-like values")]
    IReadOnlyDictionary<string, FrozenSet<int>> EnumerationValueParameterIndicesByFunctionName
) : ISettings<AstServiceSettings>
{
    public static AstServiceSettings Default { get; } = new AstServiceSettingsRaw
    {
        EnumerationValueParameterIndicesByFunctionName = new Dictionary<string, IReadOnlyCollection<int>?>(StringComparer.OrdinalIgnoreCase)
        {
            { "DATEADD", [0] },
            { "DATEPART", [0] },
            { "DATENAME", [0] },
            { "DATEDIFF", [0] }
        }
    }.ToSettings();
}
