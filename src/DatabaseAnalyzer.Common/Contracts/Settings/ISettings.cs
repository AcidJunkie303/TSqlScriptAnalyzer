using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Contracts;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types")]
public interface ISettings<out TSettings>
    where TSettings : class
{
    static abstract TSettings Default { get; }
}
