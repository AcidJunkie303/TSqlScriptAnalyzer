using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

#pragma warning disable
namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public sealed class FilteringColumnFinder
{
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public FilteringColumnFinder(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName)
        : this(issueReporter, script, relativeScriptFilePath, defaultSchemaName, script.CreateParentFragmentProvider())
    {
    }

    public FilteringColumnFinder(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName, IParentFragmentProvider parentFragmentProvider)
    {
        _issueReporter = issueReporter;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _defaultSchemaName = defaultSchemaName;
        _parentFragmentProvider = parentFragmentProvider;
    }

    public IEnumerable<ColumnReference> Find(TSqlFragment searchRoot)
    {
        var columnResolver = new TableColumnResolver(_issueReporter, _script, _relativeScriptFilePath, _parentFragmentProvider, _defaultSchemaName);
        var columnReferences = searchRoot.GetChildren<ColumnReferenceExpression>(recursive: true);
        foreach (var columnReference in columnReferences)
        {
            if (!IsUsedInComparison(columnReference))
            {
                continue;
            }

            var column = columnResolver.Resolve(columnReference);
            yield return new ColumnReference(column.DatabaseName, column.SchemaName, column.TableName, column.ColumnName, TableSourceType.NotDetermined, columnReference);
        }
    }

    private bool IsUsedInComparison(ColumnReferenceExpression column)
        => column
            .GetParents(_parentFragmentProvider)
            .Any(a => a is BooleanComparisonExpression or InPredicate);
}
