using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.App.Reporting;
using DatabaseAnalyzer.App.Reporting.Html;
using DatabaseAnalyzer.App.Reporting.Json;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Core;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Initialization;
using Serilog.Events;

namespace DatabaseAnalyzer.App;

internal static class Program
{
    [SuppressMessage("Design", "MA0051:Method is too long")]
    private static int Main(string[] args)
    {
        var settingsFileOption = new Option<string>(
            ["-f", "--file"],
            "The settings file.")
        {
            IsRequired = true
        };
        var consoleReportTypeOption = new Option<ReportType[]>(
            ["-crt", "--console-report-type"],
            () => [ReportType.Text],
            $"The console report types to render. Following values are valid: '{nameof(ReportType.Json)}','{nameof(ReportType.JsonSummary)}', '{nameof(ReportType.Html)}' or '{nameof(ReportType.Text)}'.");
        var htmlFileReportOption = new Option<string?>(
            ["-h", "--html-report-file-path"],
            "The path of file to render the html report to.");
        var htmlReportThemeOptions = new Option<ReportTheme>(
            ["-hrt", "--html-report-theme"],
            $"The theme for the HTML report. Default is `{nameof(ReportTheme.Dark)}`");
        var jsonFileReportOption = new Option<string?>(
            ["-j", "--json-report-file-path"],
            "The path of file to render the json report to.");
        var jsonSummaryReportFilePathOption = new Option<string?>(
            ["-s", "--json-summary-report-file-path"],
            "The path of file to render the json summary report to.");
        var textFileReportFilePathOption = new Option<string?>(
            ["-t", "--text-report-file-path"],
            "The path of file to render the text report to.");
        var logFilePathOption = new Option<string?>(
            ["-l", "--log-file-path"],
            "The path of the log file.");
        var minimumLogLevelOption = new Option<LogEventLevel?>(
            ["-m", "--minimum-log-level"],
            "The minimum log level. Default is 'Information'.");

        var rootCommand = new RootCommand("T-SQL script file analyzer");
        var analyzeCommand = new Command("analyze", "Analyze a project");

        analyzeCommand.AddOption(settingsFileOption);
        analyzeCommand.AddOption(consoleReportTypeOption);
        analyzeCommand.AddOption(htmlFileReportOption);
        analyzeCommand.AddOption(htmlReportThemeOptions);
        analyzeCommand.AddOption(jsonFileReportOption);
        analyzeCommand.AddOption(jsonSummaryReportFilePathOption);
        analyzeCommand.AddOption(textFileReportFilePathOption);
        analyzeCommand.AddOption(logFilePathOption);
        analyzeCommand.AddOption(minimumLogLevelOption);

        analyzeCommand.SetHandler(context =>
        {
            var consoleReportTypes = context
                                         .ParseResult
                                         .GetValueForOption(consoleReportTypeOption)
                                         .NullIfEmpty()
                                         ?.Distinct()
                                         .ToList()
                                     ?? [ReportType.Text];

            var filePath = context.ParseResult.GetValueForOption(settingsFileOption)!;
            var htmlReportFilePath = context.ParseResult.GetValueForOption(htmlFileReportOption);
            var jsonReportFilePath = context.ParseResult.GetValueForOption(jsonFileReportOption);
            var jsonSummaryReportFilePath = context.ParseResult.GetValueForOption(jsonSummaryReportFilePathOption);
            var textReportFilePath = context.ParseResult.GetValueForOption(textFileReportFilePathOption);
            var logFilePath = context.ParseResult.GetValueForOption(logFilePathOption);
            var minimumLogLevel = context.ParseResult.GetValueForOption(minimumLogLevelOption);
            var htmlReportTheme = context.ParseResult.GetValueForOption(htmlReportThemeOptions);

            var options = new AnalyzeOptions
            (
                ProjectFilePath: filePath,
                ConsoleReportTypes: consoleReportTypes,
                HtmlReportFilePath: htmlReportFilePath,
                JsonReportFilePath: jsonReportFilePath,
                JsonSummaryReportFilePath: jsonSummaryReportFilePath,
                TextReportFilePath: textReportFilePath,
                LogFilePath: logFilePath,
                MinimumLogLevel: minimumLogLevel,
                HtmlReportTheme: htmlReportTheme
            );
            context.ExitCode = AnalyzeAsync(options).GetAwaiter().GetResult();
        });

        rootCommand.AddCommand(analyzeCommand);

        return rootCommand.Invoke(args);
    }

    private static async Task<int> AnalyzeAsync(AnalyzeOptions options)
    {
        AdjustMinThreadPoolThreads();

        try
        {
            var progressCallback = new ProgressCallbackConsoleWriter();
            var (configuration, settings) = ApplicationSettingsProvider.GetSettings(options.ProjectFilePath);
            var scripts = new ScriptProvider(settings, progressCallback).GetScripts();

            using var analyzerFactory = new AnalyzerFactory(configuration, settings, scripts, progressCallback, options.LogFilePath, options.MinimumLogLevel ?? LogEventLevel.Information);
            var analyzer = analyzerFactory.Create();
            var analysisResult = analyzer.Analyze();

            foreach (var consoleReportType in options.ConsoleReportTypes)
            {
                Console.WriteLine($"{{{{{consoleReportType}-Report-Start}}}}");
                var consoleReportRenderer = ReportRendererFactory.Create(consoleReportType, options.HtmlReportTheme);
                var report = await consoleReportRenderer.RenderReportAsync(analysisResult);
                Console.WriteLine(report.Trim());
                Console.WriteLine($"{{{{{consoleReportType}-Report-End}}}}");
            }

            if (!options.HtmlReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new HtmlReportRenderer(options.HtmlReportTheme), analysisResult, options.HtmlReportFilePath);
            }

            if (!options.JsonReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new JsonFullReportRenderer(), analysisResult, options.JsonReportFilePath);
            }

            if (!options.JsonSummaryReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new JsonSummaryReportRenderer(), analysisResult, options.JsonSummaryReportFilePath);
            }

            if (!options.TextReportFilePath.IsNullOrWhiteSpace())
            {
                await ReportFileRenderer.RenderAsync(new TextReportRenderer(), analysisResult, options.TextReportFilePath);
            }

            var nonInfoIssueCount = analysisResult.Issues.Count(static a => a.DiagnosticDefinition.IssueType != IssueType.Information);
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

    private static void AdjustMinThreadPoolThreads()
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        workerThreads = Math.Max(workerThreads, Environment.ProcessorCount * 2);
        completionPortThreads = Math.Max(completionPortThreads, Environment.ProcessorCount);

        ThreadPool.SetMinThreads(workerThreads, completionPortThreads);
    }

    private sealed record AnalyzeOptions(
        string ProjectFilePath,
        IReadOnlyCollection<ReportType> ConsoleReportTypes,
        string? HtmlReportFilePath,
        string? JsonReportFilePath,
        string? JsonSummaryReportFilePath,
        string? TextReportFilePath,
        string? LogFilePath,
        LogEventLevel? MinimumLogLevel,
        ReportTheme HtmlReportTheme
    );
}
