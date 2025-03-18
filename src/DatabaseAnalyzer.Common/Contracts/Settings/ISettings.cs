using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Common.Contracts.Settings;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types")]
public interface ISettings<out TSettings>
    where TSettings : class
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    static abstract TSettings Default { get; }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
