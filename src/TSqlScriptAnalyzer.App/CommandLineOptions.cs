namespace TSqlScriptAnalyzer.App;

internal record CommandLineOptions(
    CommandType Command,
    string SettingsFilePath,
    string? ErrorMessage
);
