using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public sealed class TableColumnResolver
{
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public TableColumnResolver(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName)
        : this(issueReporter, script, relativeScriptFilePath, script.CreateParentFragmentProvider(), defaultSchemaName)
    {
        _relativeScriptFilePath = relativeScriptFilePath;
    }

    public TableColumnResolver(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
    {
        _issueReporter = issueReporter;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _parentFragmentProvider = parentFragmentProvider;
        _defaultSchemaName = defaultSchemaName;
    }

    public ColumnReference? Resolve(ColumnReferenceExpression columnReferenceExpression)
    {
        ArgumentNullException.ThrowIfNull(columnReferenceExpression);

        TSqlFragment? fragment = columnReferenceExpression;
        while (true)
        {
            fragment = fragment.GetParent(_parentFragmentProvider);
            if (fragment is null)
            {
                return null;
            }

            var column = fragment switch
            {
                QualifiedJoin qualifiedJoin => Check(qualifiedJoin, columnReferenceExpression),
                DeleteSpecification deleteSpecification => Check(deleteSpecification, columnReferenceExpression),
                FromClause fromClause => Check(fromClause, columnReferenceExpression),
                QuerySpecification querySpecification => Check(querySpecification, columnReferenceExpression),
                UpdateSpecification updateSpecification => Check(updateSpecification, columnReferenceExpression),
                MergeSpecification mergeSpecification => Check(mergeSpecification, columnReferenceExpression),
                _ => null
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

    private ColumnReference? Check(MergeSpecification mergeSpecification, ColumnReferenceExpression columnReference)
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
            var column = CheckTableReference(mergeSpecification.TableReference as NamedTableReference, columnReference);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(mergeSpecification.Target as NamedTableReference, columnReference);
    }

    private ColumnReference? Check(UpdateSpecification updateSpecification, ColumnReferenceExpression columnReference)
    {
        if (updateSpecification.FromClause is not null)
        {
            var column = Check(updateSpecification.FromClause, columnReference);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(updateSpecification.Target as NamedTableReference, columnReference);
    }

    private ColumnReference? Check(QuerySpecification querySpecification, ColumnReferenceExpression columnReference)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause, columnReference);

    private ColumnReference? Check(FromClause fromClause, ColumnReferenceExpression columnReference)
    {
        foreach (var tableReference in fromClause.TableReferences ?? [])
        {
            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                var column = Check(qualifiedJoin, columnReference);
                if (column is not null)
                {
                    return column;
                }
            }
            else
            {
                var column = CheckTableReference(tableReference as NamedTableReference, columnReference);
                if (column is not null)
                {
                    return column;
                }
            }
        }

        return null;
    }

    private ColumnReference? Check(DeleteSpecification deleteSpecification, ColumnReferenceExpression columnReference)
    {
        if (deleteSpecification.FromClause is not null)
        {
            var column = Check(deleteSpecification.FromClause, columnReference);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(deleteSpecification.Target as NamedTableReference, columnReference);
    }

    private ColumnReference? Check(QualifiedJoin qualifiedJoin, ColumnReferenceExpression columnReference)
    {
        if (columnReference.MultiPartIdentifier.Identifiers.Count < 2)
        {
            ReportMissingAlias(columnReference);
            return null;
        }

        if (qualifiedJoin.FirstTableReference is QualifiedJoin firstJoin)
        {
            var column = Check(firstJoin, columnReference);
            if (column is not null)
            {
                return column;
            }
        }

        if (qualifiedJoin.SecondTableReference is QualifiedJoin secondJoin)
        {
            var column = Check(secondJoin, columnReference);
            if (column is not null)
            {
                return column;
            }
        }

        // qualifiedJoin.FirstTableReference can also be joins -> check previous joins too
        return CheckTableReference(qualifiedJoin.FirstTableReference as NamedTableReference, columnReference)
               ?? CheckTableReference(qualifiedJoin.SecondTableReference as NamedTableReference, columnReference);
    }

    private ColumnReference? CheckTableReference(NamedTableReference? namedTableReference, ColumnReferenceExpression columnReferenceExpression)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        var (tableNameOrAlias, columnName) = columnReferenceExpression.MultiPartIdentifier.GetColumnReferenceParts();
        var tableReferenceTableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var tableReferenceSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value ?? _script.FindCurrentDatabaseNameAtFragment(columnReferenceExpression);

        // if we don't have an alias, we have aborted earlier on in case there are multiple tables involved
        // Therefore, we assume that this is the table we're looking for
        if (tableNameOrAlias is null)
        {
            var fullObjectName = GetFullObjectName();
            return new ColumnReference(currentDatabaseName, tableReferenceSchemaName, tableReferenceTableName, columnName, TableSourceType.NotDetermined, columnReferenceExpression, fullObjectName);
        }

        var tableReferenceAlias = namedTableReference.Alias?.Value;
        if (tableReferenceAlias is null)
        {
            return null;
        }

        return tableReferenceAlias.EqualsOrdinalIgnoreCase(tableNameOrAlias)
            ? new ColumnReference(currentDatabaseName ?? "Unknown", tableReferenceSchemaName, tableReferenceTableName, columnName, TableSourceType.NotDetermined, columnReferenceExpression, GetFullObjectName())
            : null;

        string GetFullObjectName()
            => columnReferenceExpression.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;
    }

    private static bool IsStatementTopmostFragment(TSqlFragment fragment)
        => fragment
            is SelectStatement
            or DeleteStatement
            or UpdateStatement
            or InsertStatement
            or MergeStatement;

    private void ReportMissingAlias(ColumnReferenceExpression columnReference)
    {
        var currentDatabaseName = _script.FindCurrentDatabaseNameAtFragment(columnReference);
        var fullObjectName = columnReference.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider);
        _issueReporter.Report(WellKnownDiagnosticDefinitions.MissingAlias, currentDatabaseName ?? "Unknown", _relativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
    }
}
