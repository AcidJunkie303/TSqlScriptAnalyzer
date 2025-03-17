using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.Services;
using Microsoft.SqlServer.TransactSql.ScriptDom;

#pragma warning disable
namespace DatabaseAnalyzer.Common.SqlParsing;

public sealed class FilteringColumnFinder
{
    private readonly IAstService _astService;
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public FilteringColumnFinder(IIssueReporter issueReporter, IAstService astService, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName)
        : this(issueReporter, astService, script, relativeScriptFilePath, defaultSchemaName, script.CreateParentFragmentProvider())
    {
    }

    public FilteringColumnFinder(IIssueReporter issueReporter, IAstService astService, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName, IParentFragmentProvider parentFragmentProvider)
    {
        _issueReporter = issueReporter;
        _astService = astService;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _defaultSchemaName = defaultSchemaName;
        _parentFragmentProvider = parentFragmentProvider;
    }

    public IEnumerable<ColumnReference> Find(TSqlFragment searchRoot)
    {
        foreach (var columnReference in searchRoot.GetChildren<ColumnReferenceExpression>(recursive: true))
        {
            var columnResolver = new TableColumnResolver(_issueReporter, _astService, _script, columnReference, _relativeScriptFilePath, _parentFragmentProvider, _defaultSchemaName);
            if (!IsUsedInComparison(columnReference))
            {
                continue;
            }

            var column = columnResolver.Resolve();
            if (column is null)
            {
                continue;
            }

            var fullObjectName = columnReference.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

            yield return new ColumnReference(column.DatabaseName, column.SchemaName, column.TableName, column.ColumnName, TableSourceType.NotDetermined, columnReference, fullObjectName);
        }
    }

    private bool IsUsedInComparison(ColumnReferenceExpression column)
        => column
            .GetParents(_parentFragmentProvider)
            .Any(a => a is BooleanComparisonExpression or InPredicate);
}
