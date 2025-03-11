using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public sealed class TableResolver
{
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public TableResolver(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName)
        : this(issueReporter, script, relativeScriptFilePath, script.CreateParentFragmentProvider(), defaultSchemaName)
    {
        _relativeScriptFilePath = relativeScriptFilePath;
    }

    public TableResolver(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
    {
        _issueReporter = issueReporter;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _parentFragmentProvider = parentFragmentProvider;
        _defaultSchemaName = defaultSchemaName;
    }

    public TableOrViewReference? Resolve(NamedTableReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var batch = (TSqlBatch?) reference
            .GetParents(_parentFragmentProvider)
            .FirstOrDefault(a => a is TSqlBatch);

        if (batch is null)
        {
            return null;
        }

        TSqlFragment? fragment = reference;
        while (true)
        {
            fragment = fragment.GetParent(_parentFragmentProvider);
            if (fragment is null)
            {
                return null;
            }

            var source = fragment switch
            {
                QualifiedJoin qualifiedJoin             => Check(qualifiedJoin, reference),
                DeleteSpecification deleteSpecification => Check(deleteSpecification, reference),
                FromClause fromClause                   => Check(fromClause, reference),
                QuerySpecification querySpecification   => Check(querySpecification, reference),
                UpdateSpecification updateSpecification => Check(updateSpecification, reference),
                MergeSpecification mergeSpecification   => Check(mergeSpecification, reference),
                SelectStatement selectStatement         => Check(selectStatement, reference),
                _                                       => null
            };

            if (source is not null)
            {
                return source;
            }

            if (IsStatementTopmostFragment(fragment))
            {
                return null;
            }
        }
    }

    private TableOrViewReference? Check(MergeSpecification mergeSpecification, NamedTableReference referenceToCheckFor)
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
            var column = CheckTableReference(mergeSpecification.TableReference as NamedTableReference, referenceToCheckFor);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(mergeSpecification.Target as NamedTableReference, referenceToCheckFor);
    }

    private TableOrViewReference? Check(UpdateSpecification updateSpecification, NamedTableReference referenceToCheckFor)
    {
        if (updateSpecification.FromClause is not null)
        {
            var column = Check(updateSpecification.FromClause, referenceToCheckFor);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(updateSpecification.Target as NamedTableReference, referenceToCheckFor);
    }

    private TableOrViewReference? Check(QuerySpecification querySpecification, NamedTableReference referenceToCheckFor)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause, referenceToCheckFor);

    private TableOrViewReference? Check(FromClause fromClause, NamedTableReference referenceToCheckFor)
    {
        var selectStatement = (SelectStatement?) fromClause.GetParents(_parentFragmentProvider).FirstOrDefault(a => a is SelectStatement);
        if (selectStatement is not null)
        {
            var table = Check(selectStatement, referenceToCheckFor);
            if (table is not null)
            {
                return table;
            }
        }

        foreach (var reference in fromClause.TableReferences ?? [])
        {
            if (reference is QualifiedJoin qualifiedJoin)
            {
                var table = Check(qualifiedJoin, referenceToCheckFor);
                if (table is not null)
                {
                    return table;
                }
            }
            else
            {
                var table = CheckTableReference(reference as NamedTableReference, referenceToCheckFor);
                if (table is not null)
                {
                    return table;
                }
            }
        }

        return null;
    }

    private TableOrViewReference? Check(DeleteSpecification deleteSpecification, NamedTableReference referenceToCheckFor)
    {
        if (deleteSpecification.FromClause is not null)
        {
            var column = Check(deleteSpecification.FromClause, referenceToCheckFor);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(deleteSpecification.Target as NamedTableReference, referenceToCheckFor);
    }

    private TableOrViewReference? Check(QualifiedJoin qualifiedJoin, NamedTableReference referenceToCheckFor)
    {
        if (qualifiedJoin.FirstTableReference is QualifiedJoin firstJoin)
        {
            var table = Check(firstJoin, referenceToCheckFor);
            if (table is not null)
            {
                return table;
            }
        }
        else if (qualifiedJoin.FirstTableReference is not TableReferenceWithAlias)
        {
            ReportMissingAlias(qualifiedJoin.FirstTableReference);
            return null;
        }

        if (qualifiedJoin.SecondTableReference is QualifiedJoin secondJoin)
        {
            var table = Check(secondJoin, referenceToCheckFor);
            if (table is not null)
            {
                return table;
            }
        }
        else if (qualifiedJoin.SecondTableReference is not TableReferenceWithAlias)
        {
            ReportMissingAlias(qualifiedJoin.SecondTableReference);
            return null;
        }

        // qualifiedJoin.FirstTableReference can also be joins -> check previous joins too
        return CheckTableReference(qualifiedJoin.FirstTableReference as NamedTableReference, referenceToCheckFor)
               ?? CheckTableReference(qualifiedJoin.SecondTableReference as NamedTableReference, referenceToCheckFor);
    }

    private TableOrViewReference? Check(SelectStatement selectStatement, NamedTableReference referenceToCheckFor)
    {
        if (selectStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions is null)
        {
            return null;
        }

        foreach (var cte in selectStatement.WithCtesAndXmlNamespaces.CommonTableExpressions)
        {
            if (referenceToCheckFor.SchemaObject.BaseIdentifier.Value.EqualsOrdinalIgnoreCase(cte.ExpressionName.Value))
            {
                var currentDatabaseName = _script.TryFindCurrentDatabaseNameAtFragment(referenceToCheckFor) ?? DatabaseNames.Unknown;
                var tableName = cte.ExpressionName.Value;
                var tableSchemaName = _defaultSchemaName;
                var fullObjectName = referenceToCheckFor.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

                return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, TableSourceType.Cte, referenceToCheckFor, fullObjectName);
            }
        }

        return null;
    }

    private TableOrViewReference? CheckTableReference(NamedTableReference? namedTableReference, NamedTableReference referenceToCheckFor)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        if (!IsSearchedTable(namedTableReference, referenceToCheckFor))
        {
            return null;
        }

        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value
                                  ?? _script.TryFindCurrentDatabaseNameAtFragment(referenceToCheckFor)
                                  ?? DatabaseNames.Unknown;
        var tableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var tableSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var fullObjectName = referenceToCheckFor.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

        return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, TableSourceType.TableOrView, referenceToCheckFor, fullObjectName);
    }

    private bool IsSearchedTable(NamedTableReference table1, NamedTableReference table2)
    {
        if (ReferenceEquals(table1, table2))
        {
            return true;
        }

        var table1Alias = table1.Alias?.Value?.NullIfEmptyOrWhiteSpace();
        var table2Alias = table2.Alias?.Value?.NullIfEmptyOrWhiteSpace();

        if (table1Alias is not null && table2Alias is not null)
        {
            return table1Alias.EqualsOrdinalIgnoreCase(table2Alias); // both table-references have aliases
        }

        if (table1Alias is null && table2Alias is null)
        {
            return table1.SchemaObject.Equals(table2.SchemaObject, _defaultSchemaName);
        }

        return string.Equals(table1.Alias?.Value, table2.SchemaObject.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase)
               || string.Equals(table2.Alias?.Value, table1.SchemaObject.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStatementTopmostFragment(TSqlFragment fragment)
        => fragment
            is SelectStatement
            or DeleteStatement
            or UpdateStatement
            or InsertStatement
            or MergeStatement;

    private void ReportMissingAlias(TableReference tableReference)
    {
        var currentDatabaseName = _script.TryFindCurrentDatabaseNameAtFragment(tableReference);
        var fullObjectName = tableReference.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider);
        _issueReporter.Report(WellKnownDiagnosticDefinitions.MissingAlias, currentDatabaseName ?? DatabaseNames.Unknown, _relativeScriptFilePath, fullObjectName, tableReference.GetCodeRegion(), tableReference.GetSql());
    }
}
