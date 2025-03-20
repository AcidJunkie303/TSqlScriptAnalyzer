namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface IColumnResolverFactory
{
    IColumnResolver CreateColumnResolver(IScriptAnalysisContext context);
    IColumnResolver CreateColumnResolver(IGlobalAnalysisContext context, IScriptModel scriptModel);
}
