using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Core.Configuration;
using DatabaseAnalyzer.Core.Extensions;
using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

internal sealed class ScriptSourceProvider : IScriptSourceProvider
{
    private readonly ScriptSourceSettings _settings;

    public ScriptSourceProvider(ScriptSourceSettings settings)
    {
        _settings = settings;
    }

    public IReadOnlyList<SourceScript> GetScriptFilePaths(CancellationToken cancellationToken = default)
    {
        AssertSourceDirectoriesExist();

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        return _settings.DatabaseScriptsRootPathByDatabaseName
#if !DEBUG
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
#endif
            .Select(a => (DatabaseName: a.Key, ScriptRootPath: a.Value))
            .Select(a => (FilePaths: Directory.GetFiles(a.ScriptRootPath, "*.sql", options), a.DatabaseName, a.ScriptRootPath))
            .SelectMany(a => a.FilePaths.Select(scriptPath => new SourceScript(scriptPath, a.DatabaseName)))
            .Where(script => _settings.ExclusionFilters.IsEmpty() || _settings.ExclusionFilters.None(exclusionFilter => exclusionFilter.IsMatch(script.FullScriptPath)))
            .ToList();
    }

    private void AssertSourceDirectoriesExist()
    {
        foreach (var (_, databaseScriptRootPath) in _settings.DatabaseScriptsRootPathByDatabaseName)
        {
            if (!Directory.Exists(databaseScriptRootPath))
            {
                throw new DirectoryNotFoundException($"Database script root path does not exist: {databaseScriptRootPath}");
            }
        }
    }
}
