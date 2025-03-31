using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;

namespace DatabaseAnalyzer.Common.Services;

public sealed class ColumnResolverFactory : IColumnResolverFactory
{
    private readonly IAstService _astService;
    private readonly IIssueReporter _issueReporter;

    public ColumnResolverFactory(IAstService astService, IIssueReporter issueReporter)
    {
        _astService = astService;
        _issueReporter = issueReporter;
    }

    public IColumnResolver CreateColumnResolver(IScriptAnalysisContext context)
        => new ColumnResolver(_issueReporter, _astService, context.Script.ParsedScript, context.Script.RelativeScriptFilePath, context.Script.ParentFragmentProvider, context.DefaultSchemaName);

    public IColumnResolver CreateColumnResolver(IGlobalAnalysisContext context, IScriptModel scriptModel)
        => new ColumnResolver(_issueReporter, _astService, scriptModel.ParsedScript, scriptModel.RelativeScriptFilePath, scriptModel.ParentFragmentProvider, context.DefaultSchemaName);
}
