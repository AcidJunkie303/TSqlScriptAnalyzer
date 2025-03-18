namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface IScriptAnalysisContextServices
{
    ITableResolver CreateTableResolver();
    IColumnResolver CreateColumnResolver();
}
