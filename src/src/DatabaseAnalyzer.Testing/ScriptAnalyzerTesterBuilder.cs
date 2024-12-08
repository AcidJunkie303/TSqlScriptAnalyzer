using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.Management.SqlParser.Parser;

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
    private const string DatabaseName = "db1";

    private readonly Dictionary<string, string> _additionalScriptContentsByFilePath = new(StringComparer.OrdinalIgnoreCase);
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

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithSettings(string diagnosticId, object? settings)
    {
        _settingsDiagnosticId.Add(diagnosticId, settings);
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> WithDefaultSchema(string schemaName)
    {
        _defaultSchemaName = schemaName;
        return this;
    }

    public ScriptAnalyzerTesterBuilder<TAnalyzer> AddAdditionalScriptFile(string scriptFileContent, string scriptFileFullPath)
    {
        _additionalScriptContentsByFilePath.Add(scriptFileFullPath, scriptFileContent);
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
        var mainScript = ParseScript(markupFreeSql, _mainScriptFileFullPath!);

        var otherScripts = _additionalScriptContentsByFilePath
            .Select(a => ParseScript(a.Key, a.Value))
            .ToList();

        var analysisContext = new AnalysisContext(
            DatabaseName: DatabaseName,
            DefaultSchemaName: _defaultSchemaName,
            Scripts: [mainScript, .. otherScripts],
            ScriptsByDatabaseName: otherScripts.ToDictionary(a => a.DatabaseName, a => a, StringComparer.OrdinalIgnoreCase),
            DiagnosticSettingsRetriever: diagnosticSettingsProvider,
            IssueReporter: new IssueReporter());

        return new ScriptAnalyzerTester(
            analysisContext: analysisContext,
            analyzer: analyzer,
            mainScript: mainScript,
            expectedIssues: expectedIssues
        );
    }

    private static ScriptModel ParseScript(string scriptContents, string fullScriptFilePath)
    {
        var parseResult = Parser.Parse(scriptContents);
        var errors = parseResult.Errors
            .Select(a => $"{a.Message} at {a.Start.LineNumber},{a.Start.ColumnNumber} - {a.End.LineNumber},{a.End.ColumnNumber}")
            .ToImmutableArray();
        var parsedScript = parseResult.Script;

        return new ScriptModel
        (
            DatabaseName: DatabaseName,
            FullScriptFilePath: fullScriptFilePath,
            Content: scriptContents,
            Script: parsedScript,
            Errors: errors
        );
    }
}
