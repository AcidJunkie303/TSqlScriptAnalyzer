using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Contracts;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IRawDiagnosticSettings<out TSettings> : IRawSettings<TSettings>
    where TSettings : class;
