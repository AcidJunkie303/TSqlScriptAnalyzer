using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Contracts;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public static class ScriptAnalyzerTesterBuilder
{
    public static ScriptAnalyzerTesterBuilder<TAnalyzer> Create<TAnalyzer>()
        where TAnalyzer : class, IScriptAnalyzer, new()
        => new();
}

[SuppressMessage("maintainability", "CA1515:Consider making public types internal", Justification = "False positive. It is used in the DatabaseAnalyzers.DefaultAnalyzers.Tests project")]
public sealed class ScriptAnalyzerTesterBuilder<TAnalyzer>
    where TAnalyzer : class, IScriptAnalyzer, new()
{
    private readonly Dictionary<string, (string Contents, string DatabaseName)> _scriptContentsByFilePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _settingsDiagnosticId = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultSchemaName = "dbo";
    private ITestOutputHelper? _testOutputHelper;

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithScriptFile(string contents, string databaseName)
    {
        var filePath = $"script_{_scriptContentsByFilePath.Count}.sql";
        _scriptContentsByFilePath.Add(filePath, (contents, databaseName));
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithSettings<TSettings>(TSettings settings)
        where TSettings : class, ISettings<TSettings>
    {
        _settingsDiagnosticId.Add(TSettings.DiagnosticId, settings);
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

    public ScriptAnalyzerTester Build()
    {
        if (_scriptContentsByFilePath.Count == 0)
        {
            throw new InvalidOperationException($"{nameof(WithScriptFile)}() has not been called");
        }

        var analyzer = new TAnalyzer();
        var diagnosticSettingsProvider = new FakeDiagnosticSettingsProvider(_settingsDiagnosticId);
        var diagnosticDefinitionRegistry = new DiagnosticDefinitionRegistry(analyzer.SupportedDiagnostics);
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

        var analysisContext = new AnalysisContext(
            _defaultSchemaName,
            allScripts,
            allScriptsByDatabaseName,
            diagnosticSettingsProvider,
            new IssueReporter());

        return new ScriptAnalyzerTester(
            analysisContext,
            analyzer,
            allScripts[0],
            expectedIssues,
            _testOutputHelper
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
}
