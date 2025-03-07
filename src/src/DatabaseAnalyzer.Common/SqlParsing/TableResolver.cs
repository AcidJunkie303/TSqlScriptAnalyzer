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

    private TableOrViewReference? Check(MergeSpecification mergeSpecification, NamedTableReference referenceToCheck)
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
            var column = CheckTableReference(mergeSpecification.TableReference as NamedTableReference, referenceToCheck);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(mergeSpecification.Target as NamedTableReference, referenceToCheck);
    }

    private TableOrViewReference? Check(UpdateSpecification updateSpecification, NamedTableReference referenceToCheck)
    {
        if (updateSpecification.FromClause is not null)
        {
            var column = Check(updateSpecification.FromClause, referenceToCheck);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(updateSpecification.Target as NamedTableReference, referenceToCheck);
    }

    private TableOrViewReference? Check(QuerySpecification querySpecification, NamedTableReference referenceToCheck)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause, referenceToCheck);

    private TableOrViewReference? Check(FromClause fromClause, NamedTableReference referenceToCheck)
    {
        foreach (var reference in fromClause.TableReferences ?? [])
        {
            if (reference is QualifiedJoin qualifiedJoin)
            {
                var table = Check(qualifiedJoin, referenceToCheck);
                if (table is not null)
                {
                    return table;
                }
            }
            else
            {
                var table = CheckTableReference(reference as NamedTableReference, referenceToCheck);
                if (table is not null)
                {
                    return table;
                }
            }
        }

        return null;
    }

    private TableOrViewReference? Check(DeleteSpecification deleteSpecification, NamedTableReference referenceToCheck)
    {
        if (deleteSpecification.FromClause is not null)
        {
            var column = Check(deleteSpecification.FromClause, referenceToCheck);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(deleteSpecification.Target as NamedTableReference, referenceToCheck);
    }

    private TableOrViewReference? Check(QualifiedJoin qualifiedJoin, NamedTableReference referenceToCheck)
    {
        if (qualifiedJoin.FirstTableReference is not TableReferenceWithAlias)
        {
            ReportMissingAlias(qualifiedJoin.FirstTableReference);
            return null;
        }

        if (qualifiedJoin.SecondTableReference is not TableReferenceWithAlias)
        {
            ReportMissingAlias(qualifiedJoin.SecondTableReference);
            return null;
        }

        if (qualifiedJoin.FirstTableReference is QualifiedJoin firstJoin)
        {
            var column = Check(firstJoin, referenceToCheck);
            if (column is not null)
            {
                return column;
            }
        }

        if (qualifiedJoin.SecondTableReference is QualifiedJoin secondJoin)
        {
            var column = Check(secondJoin, referenceToCheck);
            if (column is not null)
            {
                return column;
            }
        }

        // qualifiedJoin.FirstTableReference can also be joins -> check previous joins too
        return CheckTableReference(qualifiedJoin.FirstTableReference as NamedTableReference, referenceToCheck)
               ?? CheckTableReference(qualifiedJoin.SecondTableReference as NamedTableReference, referenceToCheck);
    }

    private TableOrViewReference? CheckTableReference(NamedTableReference? namedTableReference, NamedTableReference referenceToCheck)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        if (!IsSearchedTable(namedTableReference, referenceToCheck))
        {
            return null;
        }

        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value
                                  ?? _script.TryFindCurrentDatabaseNameAtFragment(referenceToCheck)
                                  ?? DatabaseNames.Unknown;
        var tableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var tableSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var fullObjectName = referenceToCheck.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

        return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, TableSourceType.TableOrView, referenceToCheck, fullObjectName);
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
