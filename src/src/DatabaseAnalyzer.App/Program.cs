using System.CommandLine;
using DatabaseAnalyzer.App;
using DatabaseAnalyzer.App.Reporting;
using DatabaseAnalyzer.App.Reporting.Html;
using DatabaseAnalyzer.App.Reporting.Json;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Core;
using DatabaseAnalyzer.Core.Configuration;

namespace DatabaseAnalyzer;

internal static class Program
{
    private static int Main(string[] args)
    {
        var settingsFileOption = new Option<string>(
            ["-f", "--file"],
            "The settings file.")
        {
            IsRequired = true
        };
        var consoleReportTypeOption = new Option<ConsoleReportType>(
            ["-crt", "--console-report-type"],
            () => ConsoleReportType.Text,
            $"The console report type. Either '{nameof(ConsoleReportType.Text)}' or '{nameof(ConsoleReportType.Json)}'.");
        var htmlReportOption = new Option<string?>(
            ["-h", "--html"],
            "The path of file to render the html report to.");
        var jsonReportOption = new Option<string?>(
            ["-j", "--json"],
            "The path of file to render the json report to.");
        var textReportFilePathOption = new Option<string?>(
            ["-t", "--text"],
            "The path of file to render the text report to.");

        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        var analyzeCommand = new Command("analyze", "Analyze a project");

        analyzeCommand.AddOption(settingsFileOption);
        analyzeCommand.AddOption(consoleReportTypeOption);
        analyzeCommand.AddOption(htmlReportOption);
        analyzeCommand.AddOption(jsonReportOption);
        analyzeCommand.AddOption(textReportFilePathOption);

        analyzeCommand.SetHandler(context =>
        {
            var consoleReportType = context.ParseResult.GetValueForOption(consoleReportTypeOption);
            var filePath = context.ParseResult.GetValueForOption(settingsFileOption)!;
            var htmlReportFilePath = context.ParseResult.GetValueForOption(htmlReportOption);
            var jsonReportFilePath = context.ParseResult.GetValueForOption(jsonReportOption);
            var textReportFilePath = context.ParseResult.GetValueForOption(textReportFilePathOption);

            var options = new AnalyzeOptions(filePath, consoleReportType, htmlReportFilePath, jsonReportFilePath, textReportFilePath);
            context.ExitCode = AnalyzeAsync(options).GetAwaiter().GetResult();
        });

        rootCommand.AddCommand(analyzeCommand);

        return rootCommand.Invoke(args);
    }

    private static async Task<int> AnalyzeAsync(AnalyzeOptions options)
    {
        try
        {
            var (configuration, settings) = ApplicationSettingsProvider.GetSettings(options.ProjectFilePath);
            var analyzer = AnalyzerFactory.Create(configuration, settings, new ProgressCallbackConsoleWriter());
            var analysisResult = analyzer.Analyze();

            var consoleReportRenderer = ReportRendererFactory.Create(options.ConsoleReportType);
            var consoleReport = await consoleReportRenderer.RenderReportAsync(analysisResult);

            Console.WriteLine("{{Report-Start}}");
            Console.WriteLine(consoleReport);
            Console.WriteLine("{{Report-End}}");

            Console.WriteLine("{{Mini-Json-Report-Start}}");
            Console.WriteLine(await new JsonMiniReportRenderer().RenderReportAsync(analysisResult));
            Console.WriteLine("{{Mini-Json-Report-Start}}");

            if (!options.HtmlReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new HtmlReportRenderer(), analysisResult, options.HtmlReportFilePath);
            }

            if (!options.JsonReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new JsonFullReportRenderer(), analysisResult, options.JsonReportFilePath);
            }

            if (!options.TextReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new TextReportRenderer(), analysisResult, options.TextReportFilePath);
            }

            var nonInfoIssueCount = analysisResult.Issues.Count(a => a.DiagnosticDefinition.IssueType != IssueType.Info);
            Console.WriteLine();
            Console.WriteLine($"Exiting with exit code {nonInfoIssueCount}");

            return analysisResult.Issues.Count;
        }
#pragma warning disable CA1031 // Do not catch general exception types -> root exception handler
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Console.WriteLine(ex);
            Console.WriteLine("Exiting with exit code 1");
            return 1;
        }
    }

    private sealed record AnalyzeOptions(
        string ProjectFilePath,
        ConsoleReportType ConsoleReportType,
        string? HtmlReportFilePath,
        string? JsonReportFilePath,
        string? TextReportFilePath
    );
}
