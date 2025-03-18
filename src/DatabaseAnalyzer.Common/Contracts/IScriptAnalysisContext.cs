using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IScriptAnalysisContext : IAnalysisContext
{
    IScriptModel Script { get; }
    IScriptAnalysisContextServices Services { get; }
}
