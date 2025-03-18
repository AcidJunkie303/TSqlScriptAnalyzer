using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface IGlobalAnalysisContextServices
{
    ITableResolver CreateTableResolver(IScriptModel script);
}
