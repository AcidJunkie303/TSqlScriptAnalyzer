using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IScriptAnalysisContext : IAnalysisContext
{
    IScriptModel Script { get; }
    IScriptAnalysisContextServices Services { get; }
}
