using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace DatabaseAnalyzer.Core.Logging;

internal static class ServiceCollectionExtensions
{
    public static void AddLogging(this IServiceCollection services, string? logFilePath, LogEventLevel minimumLogLevel)
    {
        logFilePath ??= GetDefaultLogFilePath();
        var logFileDirectoryPath = Path.GetDirectoryName(logFilePath);
        if (logFileDirectoryPath is not null)
        {
            EnsureLogDirectoryExists(logFileDirectoryPath);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLogLevel)
            .WriteTo.File(new CompactJsonFormatter(), logFilePath)
            .CreateLogger();

#pragma warning disable CA2000 // disposed within the DI container
        var loggerFactory = new LoggerFactory().AddSerilog(dispose: true);
#pragma warning restore CA2000

        services.AddTransient(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(loggerFactory);
    }

    [SuppressMessage("Major Code Smell", "S6354:Use a testable date/time provider")]
    private static string GetDefaultLogFilePath() => Path.Combine(Path.GetTempPath(), "TSqlScriptAnalyzer", "logs", $"{DateTimeOffset.Now:yyyyMMddhhmmss}.log");

    private static void EnsureLogDirectoryExists(string logDirectoryPath)
    {
        if (!Directory.Exists(logDirectoryPath))
        {
            Directory.CreateDirectory(logDirectoryPath);
        }
    }
}
