using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Services;

public sealed class TableResolverFactory : ITableResolverFactory
{
    private readonly IAstService _astService;
    private readonly IIssueReporter _issueReporter;

    public TableResolverFactory(IAstService astService, IIssueReporter issueReporter)
    {
        _astService = astService;
        _issueReporter = issueReporter;
    }

    public ITableResolver CreateTableResolver(IScriptAnalysisContext context)
        => new TableResolver(_issueReporter, _astService, context.Script.ParsedScript, context.Script.RelativeScriptFilePath, context.Script.ParentFragmentProvider, context.DefaultSchemaName);

    public ITableResolver CreateTableResolver(IGlobalAnalysisContext context, IScriptModel scriptModel)
        => new TableResolver(_issueReporter, _astService, scriptModel.ParsedScript, scriptModel.RelativeScriptFilePath, scriptModel.ParentFragmentProvider, context.DefaultSchemaName);
}
