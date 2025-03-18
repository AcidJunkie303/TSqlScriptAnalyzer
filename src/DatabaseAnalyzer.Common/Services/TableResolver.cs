using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Services;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
public sealed class TableResolver : ITableResolver
{
    private readonly IAstService _astService;
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public TableResolver(IIssueReporter issueReporter, IAstService astService, TSqlScript script, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
    {
        _issueReporter = issueReporter;
        _astService = astService;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _parentFragmentProvider = parentFragmentProvider;
        _defaultSchemaName = defaultSchemaName;
    }

    public TableOrViewReference? Resolve(NamedTableReference tableReference)
    {
        var batch = (TSqlBatch?) tableReference
            .GetParents(_parentFragmentProvider)
            .FirstOrDefault(a => a is TSqlBatch);

        if (batch is null)
        {
            return null;
        }

        if (_astService.IsChildOfFunctionEnumParameter(tableReference, _parentFragmentProvider))
        {
            return null;
        }

        var parentCtesByName = GetParentCtesByName(tableReference, _parentFragmentProvider);

        TSqlFragment? fragment = tableReference;
        while (true)
        {
            fragment = fragment.GetParent(_parentFragmentProvider);
            if (fragment is null)
            {
                return null;
            }

            var source = fragment switch
            {
                JoinTableReference join                 => Check(join, parentCtesByName, tableReference),
                DeleteSpecification deleteSpecification => Check(deleteSpecification, parentCtesByName, tableReference),
                FromClause fromClause                   => Check(fromClause, parentCtesByName, tableReference),
                QuerySpecification querySpecification   => Check(querySpecification, parentCtesByName, tableReference),
                UpdateSpecification updateSpecification => Check(updateSpecification, parentCtesByName, tableReference),
                MergeSpecification mergeSpecification   => Check(mergeSpecification, parentCtesByName, tableReference),
                SelectStatement selectStatement         => Check(selectStatement, tableReference),
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

    public static TableResolver Create(IScriptAnalysisContext context, IAstService astService)
        => new(context.IssueReporter, astService, context.Script.ParsedScript, context.Script.RelativeScriptFilePath, context.Script.ParentFragmentProvider, context.DefaultSchemaName);

    public static TableResolver Create(IGlobalAnalysisContext context, IScriptModel script, IAstService astService)
        => new(context.IssueReporter, astService, script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);

    private static Dictionary<string, CommonTableExpression> GetParentCtesByName(TSqlFragment fragement, IParentFragmentProvider parentFragmentProvider)
    {
        foreach (var parent in fragement.GetParents(parentFragmentProvider))
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

    private TableOrViewReference? Check(MergeSpecification mergeSpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
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
            var column = CheckTableReference(mergeSpecification.TableReference as NamedTableReference, parentCtesByName, namedTableToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(mergeSpecification.Target as NamedTableReference, parentCtesByName, namedTableToResolve);
    }

    private TableOrViewReference? Check(UpdateSpecification updateSpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
    {
        if (updateSpecification.FromClause is not null)
        {
            var column = Check(updateSpecification.FromClause, parentCtesByName, namedTableToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(updateSpecification.Target as NamedTableReference, parentCtesByName, namedTableToResolve);
    }

    private TableOrViewReference? Check(QuerySpecification querySpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause, parentCtesByName, namedTableToResolve);

    private TableOrViewReference? Check(FromClause fromClause, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
    {
        var selectStatement = (SelectStatement?) fromClause.GetParents(_parentFragmentProvider).FirstOrDefault(a => a is SelectStatement);
        if (selectStatement is not null)
        {
            var table = Check(selectStatement, namedTableToResolve);
            if (table is not null)
            {
                return table;
            }
        }

        foreach (var reference in fromClause.TableReferences ?? [])
        {
            if (reference is JoinTableReference joinTableReference)
            {
                var table = Check(joinTableReference, parentCtesByName, namedTableToResolve);
                if (table is not null)
                {
                    return table;
                }
            }
            else
            {
                var table = CheckTableReference(reference as NamedTableReference, parentCtesByName, namedTableToResolve);
                if (table is not null)
                {
                    return table;
                }
            }
        }

        return null;
    }

    private TableOrViewReference? Check(DeleteSpecification deleteSpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
    {
        if (deleteSpecification.FromClause is not null)
        {
            var column = Check(deleteSpecification.FromClause, parentCtesByName, namedTableToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(deleteSpecification.Target as NamedTableReference, parentCtesByName, namedTableToResolve);
    }

    private TableOrViewReference? Check(JoinTableReference joinTableReference, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
    {
        if (joinTableReference.FirstTableReference is JoinTableReference firstJoin)
        {
            var table = Check(firstJoin, parentCtesByName, namedTableToResolve);
            if (table is not null)
            {
                return table;
            }
        }
        else if (joinTableReference.FirstTableReference is not TableReferenceWithAlias)
        {
            ReportMissingAlias(joinTableReference.FirstTableReference);
            return null;
        }

        if (joinTableReference.SecondTableReference is JoinTableReference secondJoin)
        {
            var table = Check(secondJoin, parentCtesByName, namedTableToResolve);
            if (table is not null)
            {
                return table;
            }
        }
        else if (joinTableReference.SecondTableReference is not TableReferenceWithAlias)
        {
            ReportMissingAlias(joinTableReference.SecondTableReference);
            return null;
        }

        // joinTableReference.FirstTableReference can also be joins -> check previous joins too
        return CheckTableReference(joinTableReference.FirstTableReference as NamedTableReference, parentCtesByName, namedTableToResolve)
               ?? CheckTableReference(joinTableReference.SecondTableReference as NamedTableReference, parentCtesByName, namedTableToResolve);
    }

    private TableOrViewReference? Check(SelectStatement selectStatement, NamedTableReference namedTableToResolve)
    {
        if (selectStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions is null)
        {
            return null;
        }

        foreach (var cte in selectStatement.WithCtesAndXmlNamespaces.CommonTableExpressions)
        {
            if (namedTableToResolve.SchemaObject.BaseIdentifier.Value.EqualsOrdinalIgnoreCase(cte.ExpressionName.Value))
            {
                var currentDatabaseName = _script.TryFindCurrentDatabaseNameAtFragment(namedTableToResolve) ?? DatabaseNames.Unknown;
                var tableName = cte.ExpressionName.Value;
                var tableSchemaName = _defaultSchemaName;
                var fullObjectName = namedTableToResolve.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

                return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, TableSourceType.Cte, namedTableToResolve, fullObjectName);
            }
        }

        return null;
    }

    private TableOrViewReference? CheckTableReference(NamedTableReference? namedTableReference, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, NamedTableReference namedTableToResolve)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        if (!IsSearchedTable(namedTableReference, namedTableToResolve))
        {
            return null;
        }

        var tableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var sourceType = parentCtesByName.ContainsKey(tableName) ? TableSourceType.Cte : TableSourceType.TableOrView;

        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value
                                  ?? _script.TryFindCurrentDatabaseNameAtFragment(namedTableToResolve)
                                  ?? DatabaseNames.Unknown;
        var tableSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var fullObjectName = namedTableToResolve.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

        return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, sourceType, namedTableToResolve, fullObjectName);
    }

    private bool IsSearchedTable(NamedTableReference table, NamedTableReference namedTableToResolve)
    {
        if (ReferenceEquals(table, namedTableToResolve))
        {
            return true;
        }

        var table1Alias = table.Alias?.Value?.NullIfEmptyOrWhiteSpace();
        var table2Alias = namedTableToResolve.Alias?.Value?.NullIfEmptyOrWhiteSpace();

        if (table1Alias is not null && table2Alias is not null)
        {
            return table1Alias.EqualsOrdinalIgnoreCase(table2Alias); // both table-references have aliases
        }

        if (table1Alias is null && table2Alias is null)
        {
            return table.SchemaObject.Equals(namedTableToResolve.SchemaObject, _defaultSchemaName);
        }

        return string.Equals(table.Alias?.Value, namedTableToResolve.SchemaObject.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase)
               || string.Equals(namedTableToResolve.Alias?.Value, table.SchemaObject.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase);
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
