using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Common.Contracts.Settings;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IRawSettings<out TSettings>
    where TSettings : class
{
    TSettings ToSettings();
}
