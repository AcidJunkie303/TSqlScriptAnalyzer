namespace DatabaseAnalyzer.App;

internal record CommandLineOptions(
    CommandType Command,
    string SettingsFilePath,
    string? ErrorMessage
);
