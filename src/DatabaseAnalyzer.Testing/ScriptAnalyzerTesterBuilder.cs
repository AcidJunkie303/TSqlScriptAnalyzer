using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public static class ScriptAnalyzerTesterBuilder
{
    public static ScriptAnalyzerTesterBuilder<TAnalyzer> Create<TAnalyzer>()
        where TAnalyzer : class, IScriptAnalyzer
        => new();
}

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class ScriptAnalyzerTesterBuilder<TAnalyzer>
    where TAnalyzer : class, IScriptAnalyzer
{
    private readonly Dictionary<string, (string Contents, string DatabaseName)> _scriptContentsByFilePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(Type InterfaceType, object Implementation)> _services = [];
    private readonly List<object> _settings = [];
    private string _defaultSchemaName = "dbo";
    private ITestOutputHelper? _testOutputHelper;

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithScriptFile(string contents, string databaseName)
    {
        var filePath = $"script_{_scriptContentsByFilePath.Count}.sql";
        _scriptContentsByFilePath.Add(filePath, (contents, databaseName));
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithSettings(object settings)
    {
        _settings.Add(settings);
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithDefaultSchema(string schemaName)
    {
        _defaultSchemaName = schemaName;
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithService<TService>(object implementation)
    {
        _services.Add((typeof(TService), implementation));
        return this;
    }

    public ScriptAnalyzerTester Build()
    {
        if (_scriptContentsByFilePath.Count == 0)
        {
            throw new InvalidOperationException($"{nameof(WithScriptFile)}() has not been called");
        }

        var supportedDiagnostics = SupportedDiagnosticRetriever.GetSupportedDiagnostics(typeof(TAnalyzer));
        var diagnosticDefinitionRegistry = new DiagnosticDefinitionRegistry(supportedDiagnostics);
        var testCodeProcessor = new TestCodeProcessor(diagnosticDefinitionRegistry);

        var processedScripts = _scriptContentsByFilePath
            .Select(a =>
            {
                var testCode = testCodeProcessor.ParseTestCode(a.Value.Contents);

                return new
                {
                    testCode.MarkupFreeSql,
                    testCode.ExpectedIssues,
                    ScriptModel = ParseScript(a.Key, testCode.MarkupFreeSql, a.Value.DatabaseName)
                };
            })
            .ToList();

        var allScripts = processedScripts.ConvertAll(a => a.ScriptModel);
        var expectedIssues = processedScripts.SelectMany(a => a.ExpectedIssues).ToList();
        var allScriptsByDatabaseName = allScripts
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                a => a.Key,
                IReadOnlyList<IScriptModel> (a) => a.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var issueReporter = new IssueReporter();
        var analysisContext = new ScriptAnalysisContext(
            _defaultSchemaName,
            allScripts,
            allScripts[0],
            allScriptsByDatabaseName,
            FrozenSet<string>.Empty);

        var host = CreateHost(analysisContext, issueReporter);
        var analyzer = host.Services.GetRequiredService<IScriptAnalyzer>();

        return new ScriptAnalyzerTester(
            analysisContext,
            analyzer,
            expectedIssues,
            _testOutputHelper,
            issueReporter
        );
    }

    private static ScriptModel ParseScript(string relativeScriptFilePath, string scriptContents, string databaseName)
    {
        var script = scriptContents.TryParseSqlScript(out var errors);
        if (errors.Count > 0)
        {
            var message = $"The script `{relativeScriptFilePath}` has the following error(s):\n" + errors.StringJoin("\n");
            throw new InvalidOperationException(message);
        }

        var diagnosticSuppressions = new DiagnosticSuppressionExtractor().ExtractSuppressions(script);
        var parentFragmentProvider = script.CreateParentFragmentProvider();

        return new ScriptModel
        (
            databaseName,
            relativeScriptFilePath,
            scriptContents,
            script,
            parentFragmentProvider,
            errors,
            diagnosticSuppressions.ToList()
        );
    }

    private IHost CreateHost(ScriptAnalysisContext analysisContext, IIssueReporter issueReporter)
        => Host
            .CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                foreach (var settings in _settings)
                {
                    services.AddSingleton(settings.GetType(), settings);
                }

                services.AddSingleton<IScriptAnalyzer>(sp => ActivatorUtilities.CreateInstance<TAnalyzer>(sp, analysisContext));
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton<ITableResolverFactory, TableResolverFactory>();
                services.AddSingleton<IColumnResolverFactory, ColumnResolverFactory>();
                services.AddSingleton<IAstService>(new AstService(AstServiceSettings.Default));
                services.AddSingleton(issueReporter);

                foreach (var service in _services)
                {
                    services.AddSingleton(service.InterfaceType, service.Implementation);
                }

                var databasesByName = new DatabaseObjectExtractor(new IssueReporter())
                    .Extract(analysisContext.Scripts, analysisContext.DefaultSchemaName);

                services.AddSingleton<IObjectProvider>(new ObjectProvider(databasesByName));
            })
            .Build();
}
