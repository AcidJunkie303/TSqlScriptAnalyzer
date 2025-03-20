using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Services;

public sealed class TableResolverFactory : ITableResolverFactory
{
    private readonly IAstService _astService;

    public TableResolverFactory(IAstService astService)
    {
        _astService = astService;
    }

    public ITableResolver CreateTableResolver(IScriptAnalysisContext context)
        => TableResolver.Create(context, _astService);

    public ITableResolver CreateTableResolver(IGlobalAnalysisContext context, IScriptModel scriptModel)
        => TableResolver.Create(context, _astService, scriptModel);
}
