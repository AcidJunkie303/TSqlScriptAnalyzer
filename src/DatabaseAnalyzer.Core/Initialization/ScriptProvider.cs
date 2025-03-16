using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Core.Initialization;

public sealed class ScriptProvider
{
    private readonly DiagnosticSuppressionExtractor _diagnosticSuppressionExtractor = new();
    private readonly IProgressCallback _progressCallback;
    private readonly ScriptLoader _scriptLoader = new();
    private readonly ApplicationSettings _settings;

    public ScriptProvider(ApplicationSettings settings, IProgressCallback progressCallback)
    {
        _settings = settings;
        _progressCallback = progressCallback;
    }

    [SuppressMessage("Minor Code Smell", "S4049:Properties should be preferred")]
    public IReadOnlyList<IScriptModel> GetScripts()
    {
        var scriptPaths = GetScriptFilePaths();
        var scriptMetadatas = LoadScriptFiles(scriptPaths);
        return ParseScripts(scriptMetadatas);
    }

    private IReadOnlyList<SourceScript> GetScriptFilePaths()
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Searching SQL script files");
        var scriptSourceProvider = new ScriptSourceProvider(_settings.ScriptSource);
        return scriptSourceProvider.GetScriptFilePaths();
    }

    private List<BasicScriptInformation> LoadScriptFiles(IReadOnlyCollection<SourceScript> scripts)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Loading SQL script files");

        return scripts
#if !DEBUG
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithDegreeOfParallelism(4)
#endif
            .Select(_scriptLoader.LoadScript)
            .ToList();
    }

    private List<IScriptModel> ParseScripts(IReadOnlyList<BasicScriptInformation> scripts)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Parsing SQL script files");

        return scripts
#if !DEBUG
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
#else
            .Select(ParseScript)
            .ToList();
#endif
    }

    private IScriptModel ParseScript(BasicScriptInformation script)
    {
        var parser = TSqlParser.CreateParser(SqlVersion.Sql170, initialQuotedIdentifiers: true);
        using var reader = new StringReader(script.Contents);
        var parsedScript = parser.Parse(reader, out var parserErrors) as TSqlScript ?? new TSqlScript();
        var errorMessages = parserErrors
            .Select(a => new ScriptError(a.Message, CodeRegion.Create(a.Line, a.Column, a.Line, a.Column)))
            .ToImmutableArray();
        var suppressions = _diagnosticSuppressionExtractor.ExtractSuppressions(parsedScript).ToList();
        var parentFragmentProvider = parsedScript.CreateParentFragmentProvider();

        return new ScriptModel
        (
            script.DatabaseName,
            script.FullScriptPath,
            script.Contents,
            parsedScript,
            parentFragmentProvider,
            errorMessages,
            suppressions
        );
    }
}
