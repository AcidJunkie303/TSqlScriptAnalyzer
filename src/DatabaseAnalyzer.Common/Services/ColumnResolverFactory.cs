using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Services;

public sealed class ColumnResolverFactory : IColumnResolverFactory
{
    private readonly IAstService _astService;

    public ColumnResolverFactory(IAstService astService)
    {
        _astService = astService;
    }

    public IColumnResolver CreateColumnResolver(IScriptAnalysisContext context)
        => ColumnResolver.Create(context, _astService);

    public IColumnResolver CreateColumnResolver(IGlobalAnalysisContext context, IScriptModel scriptModel)
        => ColumnResolver.Create(context, _astService, scriptModel);
}
