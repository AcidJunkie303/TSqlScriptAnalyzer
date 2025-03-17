using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettings
{
    static abstract string DiagnosticId { get; }
}

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
[SuppressMessage("Major Code Smell", "S2743:Static fields should not be used in generic types")]
public interface IDiagnosticSettings<out TSettings> : ISettings<TSettings>, IDiagnosticSettings
    where TSettings : class;
