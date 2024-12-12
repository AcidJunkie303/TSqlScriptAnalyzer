namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettingsRetriever
{
    TSettings GetSettings<TSettings>()
        where TSettings : class, ISettings<TSettings>;
}
