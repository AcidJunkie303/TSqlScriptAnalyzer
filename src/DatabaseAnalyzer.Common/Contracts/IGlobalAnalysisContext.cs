using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IGlobalAnalysisContext : IAnalysisContext
{
    IGlobalAnalysisContextServices Services { get; }
}
