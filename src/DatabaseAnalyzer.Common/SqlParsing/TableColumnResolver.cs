using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.Services;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public sealed class TableColumnResolver
{
    private readonly IAstService _astService;
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly Dictionary<string, CommonTableExpression> _parentCtesByName;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly ColumnReferenceExpression _referenceToResolve;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public TableColumnResolver(IIssueReporter issueReporter, IAstService astService, TSqlScript script, ColumnReferenceExpression referenceToResolve, string relativeScriptFilePath, string defaultSchemaName)
        : this(issueReporter, astService, script, referenceToResolve, relativeScriptFilePath, script.CreateParentFragmentProvider(), defaultSchemaName)
    {
        _relativeScriptFilePath = relativeScriptFilePath;
    }

    public TableColumnResolver(IIssueReporter issueReporter, IAstService astService, TSqlScript script, ColumnReferenceExpression referenceToResolve, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
    {
        _issueReporter = issueReporter;
        _astService = astService;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _parentFragmentProvider = parentFragmentProvider;
        _defaultSchemaName = defaultSchemaName;
        _referenceToResolve = referenceToResolve;
        _parentCtesByName = GetParentCtesByName(referenceToResolve, _parentFragmentProvider);
    }

    public ColumnReference? Resolve()
    {
        if (_referenceToResolve.MultiPartIdentifier?.Identifiers is null)
        {
            return null;
        }

        if (_astService.IsChildOfFunctionEnumParameter(_referenceToResolve, _parentFragmentProvider))
        {
            return null;
        }

        TSqlFragment? fragment = _referenceToResolve;

        while (true)
        {
            fragment = fragment.GetParent(_parentFragmentProvider);
            if (fragment is null)
            {
                return null;
            }

            var column = fragment switch
            {
                QualifiedJoin qualifiedJoin             => Check(qualifiedJoin),
                DeleteSpecification deleteSpecification => Check(deleteSpecification),
                FromClause fromClause                   => Check(fromClause),
                QuerySpecification querySpecification   => Check(querySpecification),
                UpdateSpecification updateSpecification => Check(updateSpecification),
                MergeSpecification mergeSpecification   => Check(mergeSpecification),
                _                                       => null
            };

            if (column is not null)
            {
                return column;
            }

            if (IsStatementTopmostFragment(fragment))
            {
                return null;
            }
        }
    }

    private ColumnReference? Check(MergeSpecification mergeSpecification)
    {
        // The Alias is stored separately from the target table
        // to make our logic work, we do assign the alias to targetNamedTableReference
        if (mergeSpecification.Target is NamedTableReference targetNamedTableReference)
        {
            if (targetNamedTableReference.Alias is null && mergeSpecification.TableAlias is not null)
            {
                targetNamedTableReference.Alias = mergeSpecification.TableAlias;
            }
        }

        if (mergeSpecification.TableReference is not null)
        {
            var column = CheckTableReference(mergeSpecification.TableReference as NamedTableReference);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(mergeSpecification.Target as NamedTableReference);
    }

    private ColumnReference? Check(UpdateSpecification updateSpecification)
    {
        if (updateSpecification.FromClause is not null)
        {
            var column = Check(updateSpecification.FromClause);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(updateSpecification.Target as NamedTableReference);
    }

    private ColumnReference? Check(QuerySpecification querySpecification)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause);

    private ColumnReference? Check(FromClause fromClause)
    {
        foreach (var tableReference in fromClause.TableReferences ?? [])
        {
            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                var column = Check(qualifiedJoin);
                if (column is not null)
                {
                    return column;
                }
            }
            else
            {
                var column = CheckTableReference(tableReference as NamedTableReference);
                if (column is not null)
                {
                    return column;
                }
            }
        }

        return null;
    }

    private ColumnReference? Check(DeleteSpecification deleteSpecification)
    {
        if (deleteSpecification.FromClause is not null)
        {
            var column = Check(deleteSpecification.FromClause);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(deleteSpecification.Target as NamedTableReference);
    }

    private ColumnReference? Check(QualifiedJoin qualifiedJoin)
    {
        if (_referenceToResolve.GetParent(_parentFragmentProvider) is AssignmentSetClause assignmentExpression)
        {
            if (assignmentExpression.Column == _referenceToResolve)
            {
                return null; // we don't check the left part of update statement assignments
            }
        }

        if (_referenceToResolve.MultiPartIdentifier.Identifiers.Count < 2)
        {
            ReportMissingAlias(_referenceToResolve);
            return null;
        }

        if (qualifiedJoin.FirstTableReference is QualifiedJoin firstJoin)
        {
            var column = Check(firstJoin);
            if (column is not null)
            {
                return column;
            }
        }

        if (qualifiedJoin.SecondTableReference is QualifiedJoin secondJoin)
        {
            var column = Check(secondJoin);
            if (column is not null)
            {
                return column;
            }
        }

        // qualifiedJoin.FirstTableReference can also be joins -> check previous joins too
        return CheckTableReference(qualifiedJoin.FirstTableReference as NamedTableReference)
               ?? CheckTableReference(qualifiedJoin.SecondTableReference as NamedTableReference);
    }

    private ColumnReference? CheckTableReference(NamedTableReference? namedTableReference)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        var (tableNameOrAlias, columnName) = _referenceToResolve.MultiPartIdentifier.GetColumnReferenceParts();
        var tableReferenceTableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var tableReferenceSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value
                                  ?? _script.TryFindCurrentDatabaseNameAtFragment(_referenceToResolve)
                                  ?? DatabaseNames.Unknown;

        // if we don't have an alias, we have aborted earlier on in case there are multiple tables involved
        // Therefore, we assume that this is the table we're looking for
        if (tableNameOrAlias is null)
        {
            var fullObjectName = GetFullObjectName();
            return new ColumnReference(currentDatabaseName, tableReferenceSchemaName, tableReferenceTableName, columnName, TableSourceType.NotDetermined, _referenceToResolve, fullObjectName);
        }

        var tableReferenceAlias = namedTableReference.Alias?.Value;
        if (tableReferenceAlias is null)
        {
            return null;
        }

        var sourceType = _parentCtesByName.ContainsKey(tableNameOrAlias) || _parentCtesByName.ContainsKey(tableReferenceAlias)
            ? TableSourceType.Cte
            : TableSourceType.TableOrView;

        return tableReferenceAlias.EqualsOrdinalIgnoreCase(tableNameOrAlias)
            ? new ColumnReference(currentDatabaseName, tableReferenceSchemaName, tableReferenceTableName, columnName, sourceType, _referenceToResolve, GetFullObjectName())
            : null;

        string GetFullObjectName()
            => _referenceToResolve.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;
    }

    private static bool IsStatementTopmostFragment(TSqlFragment fragment)
        => fragment
            is SelectStatement
            or DeleteStatement
            or UpdateStatement
            or InsertStatement
            or MergeStatement
            or TSqlBatch
            or TSqlScript;

    private void ReportMissingAlias(ColumnReferenceExpression columnReference)
    {
        var currentDatabaseName = _script.TryFindCurrentDatabaseNameAtFragment(columnReference);
        var fullObjectName = columnReference.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider);

        _issueReporter.Report(WellKnownDiagnosticDefinitions.MissingAlias, currentDatabaseName ?? DatabaseNames.Unknown, _relativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
    }

    private static Dictionary<string, CommonTableExpression> GetParentCtesByName(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
    {
        foreach (var parent in fragment.GetParents(parentFragmentProvider))
        {
            if (parent is not SelectStatement selectStatement)
            {
                continue;
            }

            if ((selectStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions).IsNullOrEmpty())
            {
                return [];
            }

            return selectStatement.WithCtesAndXmlNamespaces.CommonTableExpressions
                .ToDictionary(
                    a => a.ExpressionName.Value,
                    a => a,
                    StringComparer.OrdinalIgnoreCase);
        }

        return [];
    }
}
