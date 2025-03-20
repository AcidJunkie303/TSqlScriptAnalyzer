namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface ITableResolverFactory
{
    ITableResolver CreateTableResolver(IScriptAnalysisContext context);
    ITableResolver CreateTableResolver(IGlobalAnalysisContext context, IScriptModel scriptModel);
}
