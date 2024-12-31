namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettingsProvider
{
    TSettings GetSettings<TSettings>()
        where TSettings : class, ISettings<TSettings>;
}
