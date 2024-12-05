using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;
using DatabaseAnalyzer.Core.Services;

namespace DatabaseAnalyzer.Core;

public interface IAnalyzer
{
    AnalysisResult Analyze();
}

internal class Analyzer : IAnalyzer
{
    private readonly IProgressCallback _progressCallback;
    private readonly IScriptLoader _scriptLoader;
    private readonly IScriptSourceProvider _scriptSourceProvider;

    public Analyzer
    (
        IProgressCallback progressCallback,
        IScriptSourceProvider scriptSourceProvider, IScriptLoader scriptLoader)
    {
        _progressCallback = progressCallback;
        _scriptSourceProvider = scriptSourceProvider;
        _scriptLoader = scriptLoader;
    }

    public AnalysisResult Analyze()
    {
        var sourceScripts = GetScriptFilePaths();
        var scripts = LoadScriptFiles(sourceScripts);
        Console.WriteLine(scripts);
        return null!;
    }

    private IReadOnlyList<SourceScript> GetScriptFilePaths()
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Searching SQL script files");
        return _scriptSourceProvider.GetScriptFilePaths();
    }

    private List<BasicScriptInformation> LoadScriptFiles(IReadOnlyCollection<SourceScript> scripts)
    {
        using var _ = _progressCallback.OnProgressWithAutoEndActionNotification("Loading SQL script files");

        return scripts
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithDegreeOfParallelism(4)
            .Select(_scriptLoader.LoadScript)
            .ToList();
    }
}
