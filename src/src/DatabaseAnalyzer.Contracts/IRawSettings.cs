using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Contracts;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IRawSettings<out TSettings>
    where TSettings : class
{
    TSettings ToSettings();
}
