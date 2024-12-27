using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;

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
        var diagnosticSettingsProvider = new FakeDiagnosticSettingsRetriever(_settingsDiagnosticId);
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
                a => (IReadOnlyList<IScriptModel>)a.ToList(),
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
            expectedIssues
        );
    }

    private static ScriptModel ParseScript(string relativeScriptFilePath, string scriptContents, string databaseName)
    {
        var script = scriptContents.TryParseSqlScript(out var errors);
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
