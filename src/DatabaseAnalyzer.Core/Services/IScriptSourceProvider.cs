using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core.Services;

internal interface IScriptSourceProvider
{
    IReadOnlyList<SourceScript> GetScriptFilePaths(CancellationToken cancellationToken = default);
}
