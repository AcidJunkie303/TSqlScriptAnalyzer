using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Services;

public sealed class ScriptAnalysisContextServices : IScriptAnalysisContextServices
{
    private readonly IAstService _astService;
    private readonly IScriptAnalysisContext _context;

    public ScriptAnalysisContextServices(IScriptAnalysisContext context, IAstService astService)
    {
        _context = context;
        _astService = astService;
    }

    public ITableResolver CreateTableResolver() => TableResolver.Create(_context, _astService);
}
