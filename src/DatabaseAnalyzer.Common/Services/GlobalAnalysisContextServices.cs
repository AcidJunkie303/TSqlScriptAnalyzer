using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Common.Services;

public sealed class GlobalAnalysisContextServices : IScriptAnalysisContextServices
{
    private readonly IAstService _astService;
    private readonly IGlobalAnalysisContext _context;
    private readonly IScriptModel _script;

    public GlobalAnalysisContextServices(IGlobalAnalysisContext context, IScriptModel script, IAstService astService)
    {
        _context = context;
        _script = script;
        _astService = astService;
    }

    public ITableResolver CreateTableResolver() => TableResolver.Create(_context, _script, _astService);
}
