using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettingsProvider
{
    [SuppressMessage("Minor Code Smell", "S4018:All type parameters should be used in the parameter list to enable type inference")]
    TSettings GetSettings<TSettings>()
        where TSettings : class, ISettings<TSettings>;
}
