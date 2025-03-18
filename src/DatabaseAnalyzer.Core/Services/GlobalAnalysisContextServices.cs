using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;

namespace DatabaseAnalyzer.Core.Services;

public sealed class GlobalAnalysisContextServices : IGlobalAnalysisContextServices
{
    private readonly IAstService _astService;
    private readonly IGlobalAnalysisContext _context;

    public GlobalAnalysisContextServices(IGlobalAnalysisContext context, IAstService astService)
    {
        _context = context;
        _astService = astService;
    }

    public ITableResolver CreateTableResolver(IScriptModel script) => TableResolver.Create(_context, script, _astService);
}
