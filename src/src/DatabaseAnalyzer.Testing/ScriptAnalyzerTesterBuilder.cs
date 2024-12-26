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
    private const string DefaultDatabaseName = "MyDb";

    private readonly Dictionary<string, (string Contents, string DatabaseName)> _additionalScriptsByFilePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object?> _settingsDiagnosticId = new(StringComparer.OrdinalIgnoreCase);
    private string _defaultSchemaName = "dbo";
    private string? _mainScriptContents;
    private string? _mainScriptFileFullPath;

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithMainScriptFile(string mainScriptContents, string mainScriptFileFullPath = "main.sql")
    {
        _mainScriptContents = mainScriptContents;
        _mainScriptFileFullPath = mainScriptFileFullPath;
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

    public ScriptAnalyzerTesterBuilder<TAnalyzer> AddAdditionalScriptFile(string contents, string scriptFileFullPath, string databaseName)
    {
        _additionalScriptsByFilePath.Add(scriptFileFullPath, (contents, databaseName));
        return this;
    }

    public ScriptAnalyzerTester Build()
    {
        if (_mainScriptContents is null)
        {
            throw new InvalidOperationException($"{nameof(WithMainScriptFile)}() has not been called");
        }

        var analyzer = new TAnalyzer();
        var diagnosticSettingsProvider = new FakeDiagnosticSettingsRetriever(_settingsDiagnosticId);
        var diagnosticDefinitionRegistry = new DiagnosticDefinitionRegistry(analyzer.SupportedDiagnostics);
        var (markupFreeSql, expectedIssues) = new TestCodeProcessor(diagnosticDefinitionRegistry).ParseTestCode(_mainScriptContents!);
        var mainScript = ParseScript(_mainScriptFileFullPath!, markupFreeSql, DefaultDatabaseName);

        var otherScripts = _additionalScriptsByFilePath
            .Select(a => ParseScript(a.Key, a.Value.Contents, a.Value.DatabaseName))
            .ToList();

        List<IScriptModel> allScripts = [mainScript, .. otherScripts];
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
            mainScript,
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
