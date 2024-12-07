using DatabaseAnalyzer.Core;
using DatabaseAnalyzer.Core.Configuration;

namespace DatabaseAnalyzer.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            HandleCommandLineArguments(args);
            return 0;
        }
#pragma warning disable CA1031 // Do not catch general exception types -> we want them all
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static void HandleCommandLineArguments(string[] args)
    {
        // TODO: implement a more sophisticated version of command line parsing because we also need to provide additional options like report type (HTML, json) and output type (console, file) for example
        var parsedArguments = CommandLineParser.Parse(args);
        switch (parsedArguments.Command)
        {
            case CommandType.None:
                Console.WriteLine("No command specified. Use '--help' for more information.");
                break;

            case CommandType.Help:
                Console.WriteLine("T-SQL File Analyzer");
                Console.WriteLine("");
                Console.WriteLine("Usage:");
                Console.WriteLine("    TSqlScriptAnalyzer.exe {path-to-settings-file}");
                break;

            case CommandType.Analyze:
                Analyze(parsedArguments.SettingsFilePath);
                break;

            case CommandType.Error:
                Console.WriteLine(parsedArguments.ErrorMessage ?? string.Empty);
                break;

            default:
                throw new ArgumentOutOfRangeException($"Command '{parsedArguments.Command}' is not handled.'");
        }
    }

    private static void Analyze(string settingsFilePath)
    {
        var (configuration, settings) = ApplicationSettingsProvider.GetSettings(settingsFilePath);
        var analyzer = AnalyzerFactory.Create(configuration, settings, new ProgressCallbackConsoleWriter());
        var analysisResult = analyzer.Analyze();

        // TODO: add report type and render the report accordingly
        Console.WriteLine(analysisResult.ToString());
    }
}
