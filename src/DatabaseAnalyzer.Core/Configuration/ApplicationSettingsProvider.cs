using Microsoft.Extensions.Configuration;

namespace DatabaseAnalyzer.Core.Configuration;

public static class ApplicationSettingsProvider
{
    public static ApplicationSettings GetSettings(string settingsFilePath)
    {
        var configuration = GetConfiguration(settingsFilePath);
        var rawSettings = configuration.Get<ApplicationSettingsRaw>() ?? throw new ConfigurationException("Invalid settings file");
        return rawSettings.ToSettings();
    }

    private static IConfiguration GetConfiguration(string settingsFilePath)
    {
        var path = Path.IsPathRooted(settingsFilePath)
            ? settingsFilePath
            : Path.GetFullPath(settingsFilePath, Environment.CurrentDirectory);

        return new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile(path, optional: false)
            .Build();
    }
}
