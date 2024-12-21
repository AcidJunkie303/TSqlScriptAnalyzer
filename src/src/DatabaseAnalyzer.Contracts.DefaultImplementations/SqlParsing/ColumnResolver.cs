// TODO: remove

using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

#pragma warning disable

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

/*
For Delete:
+-----------------------------------------+-----------------+-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Type                                    | Region          | Contents                                                                                                                                                                                                                    |
+-----------------------------------------+-----------------+-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
|     DeleteStatement                     | (4,1)-(5,17)    | DELETE  Table1\r\nWHERE   ID = 303                                                                                                                                                                                          |
|       DeleteSpecification               | (4,1)-(5,17)    | DELETE  Table1\r\nWHERE   ID = 303                                                                                                                                                                                          |
|         NamedTableReference             | (4,9)-(4,15)    | Table1                                                                                                                                                                                                                      |
|           SchemaObjectName              | (4,9)-(4,15)    | Table1                                                                                                                                                                                                                      |
|             Identifier                  | (4,9)-(4,15)    | Table1                                                                                                                                                                                                                      |
|         WhereClause                     | (5,1)-(5,17)    | WHERE   ID = 303                                                                                                                                                                                                            |
|           BooleanComparisonExpression   | (5,9)-(5,17)    | ID = 303                                                                                                                                                                                                                    |
|             ColumnReferenceExpression   | (5,9)-(5,11)    | ID                                                                                                                                                                                                                          |
|               MultiPartIdentifier       | (5,9)-(5,11)    | ID                                                                                                                                                                                                                          |
|                 Identifier              | (5,9)-(5,11)    | ID                                                                                                                                                                                                                          |
|             IntegerLiteral              | (5,14)-(5,17)   | 303                                                                                                                                                                                                                         |
|     DeleteStatement                     | (7,1)-(9,15)    | DELETE\r\nFROM    Table1\r\nWHERE   ID = 1                                                                                                                                                                                  |
|       DeleteSpecification               | (7,1)-(9,15)    | DELETE\r\nFROM    Table1\r\nWHERE   ID = 1                                                                                                                                                                                  |
|         NamedTableReference             | (8,9)-(8,15)    | Table1                                                                                                                                                                                                                      |
|           SchemaObjectName              | (8,9)-(8,15)    | Table1                                                                                                                                                                                                                      |
|             Identifier                  | (8,9)-(8,15)    | Table1                                                                                                                                                                                                                      |
|         WhereClause                     | (9,1)-(9,15)    | WHERE   ID = 1                                                                                                                                                                                                              |
|           BooleanComparisonExpression   | (9,9)-(9,15)    | ID = 1                                                                                                                                                                                                                      |
|             ColumnReferenceExpression   | (9,9)-(9,11)    | ID                                                                                                                                                                                                                          |
|               MultiPartIdentifier       | (9,9)-(9,11)    | ID                                                                                                                                                                                                                          |
|                 Identifier              | (9,9)-(9,11)    | ID                                                                                                                                                                                                                          |
|             IntegerLiteral              | (9,14)-(9,15)   | 1                                                                                                                                                                                                                           |
|     DeleteStatement                     | (11,1)-(14,24)  | DELETE  t1\r\nFROM    Table1 t1\r\ninner   join Table2 t2 on t2.Id = t1.id\r\nwhere   t2.Value2 = 303                                                                                                                       |
|       DeleteSpecification               | (11,1)-(14,24)  | DELETE  t1\r\nFROM    Table1 t1\r\ninner   join Table2 t2 on t2.Id = t1.id\r\nwhere   t2.Value2 = 303                                                                                                                       |
|         NamedTableReference             | (11,9)-(11,11)  | t1                                                                                                                                                                                                                          |
|           SchemaObjectName              | (11,9)-(11,11)  | t1                                                                                                                                                                                                                          |
|             Identifier                  | (11,9)-(11,11)  | t1                                                                                                                                                                                                                          |
|         FromClause                      | (12,1)-(13,40)  | FROM    Table1 t1\r\ninner   join Table2 t2 on t2.Id = t1.id                                                                                                                                                                |
|           QualifiedJoin                 | (12,9)-(13,40)  | Table1 t1\r\ninner   join Table2 t2 on t2.Id = t1.id                                                                                                                                                                        |
|             NamedTableReference         | (12,9)-(12,18)  | Table1 t1                                                                                                                                                                                                                   |
|               SchemaObjectName          | (12,9)-(12,15)  | Table1                                                                                                                                                                                                                      |
|                 Identifier              | (12,9)-(12,15)  | Table1                                                                                                                                                                                                                      |
|               Identifier                | (12,16)-(12,18) | t1                                                                                                                                                                                                                          |
|             NamedTableReference         | (13,14)-(13,23) | Table2 t2                                                                                                                                                                                                                   |
|               SchemaObjectName          | (13,14)-(13,20) | Table2                                                                                                                                                                                                                      |
|                 Identifier              | (13,14)-(13,20) | Table2                                                                                                                                                                                                                      |
|               Identifier                | (13,21)-(13,23) | t2                                                                                                                                                                                                                          |
|             BooleanComparisonExpression | (13,27)-(13,40) | t2.Id = t1.id                                                                                                                                                                                                               |
|               ColumnReferenceExpression | (13,27)-(13,32) | t2.Id                                                                                                                                                                                                                       |
|                 MultiPartIdentifier     | (13,27)-(13,32) | t2.Id                                                                                                                                                                                                                       |
|                   Identifier            | (13,27)-(13,29) | t2                                                                                                                                                                                                                          |
|                   Identifier            | (13,30)-(13,32) | Id                                                                                                                                                                                                                          |
|               ColumnReferenceExpression | (13,35)-(13,40) | t1.id                                                                                                                                                                                                                       |
|                 MultiPartIdentifier     | (13,35)-(13,40) | t1.id                                                                                                                                                                                                                       |
|                   Identifier            | (13,35)-(13,37) | t1                                                                                                                                                                                                                          |
|                   Identifier            | (13,38)-(13,40) | id                                                                                                                                                                                                                          |
|         WhereClause                     | (14,1)-(14,24)  | where   t2.Value2 = 303                                                                                                                                                                                                     |
|           BooleanComparisonExpression   | (14,9)-(14,24)  | t2.Value2 = 303                                                                                                                                                                                                             |
|             ColumnReferenceExpression   | (14,9)-(14,18)  | t2.Value2                                                                                                                                                                                                                   |
|               MultiPartIdentifier       | (14,9)-(14,18)  | t2.Value2                                                                                                                                                                                                                   |
|                 Identifier              | (14,9)-(14,11)  | t2                                                                                                                                                                                                                          |
|                 Identifier              | (14,12)-(14,18) | Value2                                                                                                                                                                                                                      |
|             IntegerLiteral              | (14,21)-(14,24) | 303                                                                                                                                                                                                                         |
+-----------------------------------------+-----------------+-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+



 */

public class ColumnResolver
{
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public ColumnResolver(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, string defaultSchemaName) : this(issueReporter, script, relativeScriptFilePath, script.CreateParentFragmentProvider(), defaultSchemaName)
    {
        _relativeScriptFilePath = relativeScriptFilePath;
    }

    public ColumnResolver(IIssueReporter issueReporter, TSqlScript script, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
    {
        _issueReporter = issueReporter;
        _script = script;
        _parentFragmentProvider = parentFragmentProvider;
        _defaultSchemaName = defaultSchemaName;
    }

    public Column? Resolve(ColumnReferenceExpression columnReferenceExpression)
    {
        TSqlFragment fragment = columnReferenceExpression;
        while (true)
        {
            fragment = fragment.GetParent(_parentFragmentProvider);
            if (fragment is null || IsStatementTopmostFragment(fragment))
            {
                break;
            }

            var column = fragment switch
            {
                QualifiedJoin qualifiedJoin => Check(qualifiedJoin, columnReferenceExpression),
                DeleteSpecification deleteSpecification => Check(deleteSpecification, columnReferenceExpression),
                FromClause fromClause => Check(fromClause, columnReferenceExpression),
                QuerySpecification querySpecification => Check(querySpecification, columnReferenceExpression),
                _ => null
            };

            if (column is not null)
            {
                return column;
            }
        }

        return null;
    }

    private Column? Check(QuerySpecification querySpecification, ColumnReferenceExpression columnReference)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause, columnReference);

    private Column? Check(FromClause fromClause, ColumnReferenceExpression columnReference)
    {
        foreach (var tableReference in fromClause.TableReferences ?? [])
        {
            var column = tableReference switch
            {
                QualifiedJoin qualifiedJoin => Check(qualifiedJoin, columnReference),
                _ => CheckTableReference(tableReference as NamedTableReference, columnReference)
            };

            if (column is not null)
            {
                return column;
            }
        }

        return null;
    }

    private Column? Check(DeleteSpecification deleteSpecification, ColumnReferenceExpression columnReference)
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

    private Column? Check(QualifiedJoin qualifiedJoin, ColumnReferenceExpression columnReference)
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

    private Column? CheckTableReference(NamedTableReference? namedTableReference, ColumnReferenceExpression columnReferenceExpression)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        var (tableNameOrAlias, columnName) = columnReferenceExpression.MultiPartIdentifier.GetColumnReferenceParts();
        var tableReferenceTableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var tableReferenceSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var currentDatabaseName = columnReferenceExpression.FindCurrentDatabaseNameAtFragment(_script);

        // if we don't have an alias, we have aborted earlier on in case there are multiple tables involved
        // Therefore, we assume that this is the table we're looking for
        if (tableNameOrAlias is null)
        {
            return new Column(currentDatabaseName ?? "Unknown", tableReferenceSchemaName, tableReferenceTableName, columnName);
        }

        var tableReferenceAlias = namedTableReference.Alias?.Value;
        if (tableReferenceAlias is null)
        {
            return null;
        }

        return tableReferenceAlias.EqualsOrdinalIgnoreCase(tableNameOrAlias)
            ? new Column(currentDatabaseName ?? "Unknown", tableReferenceSchemaName, tableReferenceTableName, columnName)
            : null;
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
        var currentDatabaseName = columnReference.FindCurrentDatabaseNameAtFragment(_script);
        var fullObjectName = columnReference.TryGetFirstClassObjectName(_defaultSchemaName, _parentFragmentProvider);
        _issueReporter.Report(WellKnownDiagnosticDefinitions.MissingAlias, currentDatabaseName ?? "Unknown", _relativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
    }

    public sealed record Column(string DatabaseName, string SchemaName, string ObjectName, string ColumnName);
}

/*
public class ColumnResolver
{
    private readonly TSqlScript _script;

    public ColumnResolver(TSqlScript script)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
    }

    public (string? DatabaseName, string? SchemaName, string ObjectName) ResolveColumnSource(ColumnReferenceExpression columnReference)
    {
        if (columnReference == null)
            throw new ArgumentNullException(nameof(columnReference));

        var visitor = new ColumnSourceVisitor(columnReference);
        visitor.ExplicitVisit(_script); // Explicitly visit the TSqlScript

        return visitor.ResolvedSource;
    }

    private class ColumnSourceVisitor : TSqlFragmentVisitor
    {
        private readonly ColumnReferenceExpression _targetColumn;
        public (string? DatabaseName, string? SchemaName, string ObjectName) ResolvedSource { get; private set; }

        public ColumnSourceVisitor(ColumnReferenceExpression targetColumn)
        {
            _targetColumn = targetColumn;
            ResolvedSource = (null, null, null); // Default to null values
        }

        public override void ExplicitVisit(SelectStatement node)
        {
            if (node.QueryExpression is QuerySpecification querySpecification)
            {
                ResolveFromClause(querySpecification.FromClause);
            }

            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(InsertStatement node)
        {
            ResolveTarget(node.InsertSpecification.Target);
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(UpdateStatement node)
        {
            ResolveTarget(node.UpdateSpecification.Target);
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(DeleteStatement node)
        {
            ResolveTarget(node.DeleteSpecification.Target);
            base.ExplicitVisit(node);
        }

        private void ResolveFromClause(FromClause fromClause)
        {
            if (fromClause == null) return;

            foreach (var tableReference in fromClause.TableReferences)
            {
                if (tableReference is NamedTableReference namedTable)
                {
                    // Handle table alias
                    var alias = namedTable.Alias?.Value;
                    if (MatchesAliasOrTable(namedTable.SchemaObject.BaseIdentifier?.Value, alias))
                    {
                        ResolvedSource = (
                            namedTable.SchemaObject.DatabaseIdentifier?.Value, // Database name
                            namedTable.SchemaObject.SchemaIdentifier?.Value, // Schema name
                            alias ?? namedTable.SchemaObject.BaseIdentifier?.Value // Alias or table name
                        );
                        break;
                    }
                }
                else if (tableReference is QueryDerivedTable derivedTable)
                {
                    // Handle derived table alias
                    if (MatchesAliasOrTable(derivedTable.Alias?.Value, derivedTable.Alias?.Value))
                    {
                        ResolvedSource = (null, null, derivedTable.Alias.Value); // Derived tables don't have schemas or databases
                        break;
                    }
                }
                else if (tableReference is JoinTableReference joinTable)
                {
                    // Handle joins recursively
                    ResolveJoin(joinTable);
                }
            }
        }

        private void ResolveJoin(JoinTableReference joinTable)
        {
            // Recursively resolve both sides of the join
            if (joinTable.FirstTableReference is NamedTableReference firstNamedTable)
            {
                var alias = firstNamedTable.Alias?.Value;
                if (MatchesAliasOrTable(firstNamedTable.SchemaObject.BaseIdentifier?.Value, alias))
                {
                    ResolvedSource = (
                        firstNamedTable.SchemaObject.DatabaseIdentifier?.Value,
                        firstNamedTable.SchemaObject.SchemaIdentifier?.Value,
                        alias ?? firstNamedTable.SchemaObject.BaseIdentifier?.Value
                    );
                }
            }

            if (joinTable.SecondTableReference is NamedTableReference secondNamedTable)
            {
                var alias = secondNamedTable.Alias?.Value;
                if (MatchesAliasOrTable(secondNamedTable.SchemaObject.BaseIdentifier?.Value, alias))
                {
                    ResolvedSource = (
                        secondNamedTable.SchemaObject.DatabaseIdentifier?.Value,
                        secondNamedTable.SchemaObject.SchemaIdentifier?.Value,
                        alias ?? secondNamedTable.SchemaObject.BaseIdentifier?.Value
                    );
                }
            }
        }

        private void ResolveTarget(TableReference target)
        {
            if (target is NamedTableReference namedTarget)
            {
                // Check table alias or name
                var alias = namedTarget.Alias?.Value;
                if (MatchesAliasOrTable(namedTarget.SchemaObject.BaseIdentifier?.Value, alias))
                {
                    ResolvedSource = (
                        namedTarget.SchemaObject.DatabaseIdentifier?.Value,
                        namedTarget.SchemaObject.SchemaIdentifier?.Value,
                        alias ?? namedTarget.SchemaObject.BaseIdentifier?.Value
                    );
                }
            }
        }

        private bool MatchesAliasOrTable(string? tableName, string? alias)
        {
            var columnName = _targetColumn.MultiPartIdentifier.Identifiers[0].Value;
            var columnReferenceAlias = _targetColumn.MultiPartIdentifier.Identifiers.Count > 0
                ? _targetColumn.MultiPartIdentifier.Identifiers[1]
                : null;

            if (alias is null)
            {
                return
            }

            return (!string.IsNullOrEmpty(tableName) &&
                    string.Equals(tableOrAlias, tableName, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrEmpty(alias) &&
                    string.Equals(columnName, alias, StringComparison.OrdinalIgnoreCase));


        //   // Extract identifiers from the column reference expression
        //   var identifiers = _targetColumn.MultiPartIdentifier?.Identifiers;

        //   if ((identifiers == null) || (identifiers.Count == 0)) return false;

        //   // If there are multiple parts in the identifier, treat the first part as the table/alias
        //   var tableOrAlias = identifiers.Count > 1 ? identifiers[0].Value : null;

        //   // If there's only one part in the identifier, we can't match it to a table/alias directly
        //   if (tableOrAlias == null) return false;

        //   // Match against either the table name or its alias
        //   return (!string.IsNullOrEmpty(tableName) &&
        //           string.Equals(tableOrAlias, tableName, StringComparison.OrdinalIgnoreCase)) ||
        //          (!string.IsNullOrEmpty(alias) &&
        //           string.Equals(tableOrAlias, alias, StringComparison.OrdinalIgnoreCase));
        //
        }
    }
}
*/
/*
public sealed class ColumnResolver
{
    private readonly string _defaultSchemaName;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly TSqlScript _script;
    private IIssueReporter _issueReporter;

    public ColumnResolver(string defaultSchemaName, TSqlScript script, IIssueReporter issueReporter) : this(defaultSchemaName, script, script.CreateParentFragmentProvider(), issueReporter)
    {
    }

    public ColumnResolver(string defaultSchemaName, TSqlScript script, IParentFragmentProvider parentFragmentProvider, IIssueReporter issueReporter)
    {
        _defaultSchemaName = defaultSchemaName;
        _script = script;
        _parentFragmentProvider = parentFragmentProvider;
        _issueReporter = issueReporter;
    }

    public sealed record Column(string DatabaseName, string SchemaName, string ObjectName, string ColumnName);
}
*/
