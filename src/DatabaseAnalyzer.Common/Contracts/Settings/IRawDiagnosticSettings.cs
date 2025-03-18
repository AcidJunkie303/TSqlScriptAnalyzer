using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Common.Contracts.Settings;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public interface IRawDiagnosticSettings<out TSettings> : IRawSettings<TSettings>
    where TSettings : class;
