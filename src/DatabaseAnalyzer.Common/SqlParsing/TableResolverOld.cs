using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public sealed class TableResolverOld
{
    private readonly IAstService _astService;
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly Dictionary<string, CommonTableExpression> _parentCtesByName;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly NamedTableReference _referenceToResolve;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public TableResolverOld(IIssueReporter issueReporter, IAstService astService, TSqlScript script, NamedTableReference referenceToResolve, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
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

    public static TableResolverOld Create(IScriptAnalysisContext context, IAstService astService, NamedTableReference referenceToResolve)
        => new(context.IssueReporter, astService, context.Script.ParsedScript, referenceToResolve, context.Script.RelativeScriptFilePath, context.Script.ParentFragmentProvider, context.DefaultSchemaName);

    public static TableResolverOld Create(IGlobalAnalysisContext context, IScriptModel script, IAstService astService, NamedTableReference referenceToResolve)
        => new(context.IssueReporter, astService, script.ParsedScript, referenceToResolve, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);

    public TableOrViewReference? Resolve()
    {
        var batch = (TSqlBatch?) _referenceToResolve
            .GetParents(_parentFragmentProvider)
            .FirstOrDefault(a => a is TSqlBatch);

        if (batch is null)
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

            var source = fragment switch
            {
                JoinTableReference join                 => Check(join),
                DeleteSpecification deleteSpecification => Check(deleteSpecification),
                FromClause fromClause                   => Check(fromClause),
                QuerySpecification querySpecification   => Check(querySpecification),
                UpdateSpecification updateSpecification => Check(updateSpecification),
                MergeSpecification mergeSpecification   => Check(mergeSpecification),
                SelectStatement selectStatement         => Check(selectStatement),
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

    private TableOrViewReference? Check(MergeSpecification mergeSpecification)
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

    private TableOrViewReference? Check(UpdateSpecification updateSpecification)
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

    private TableOrViewReference? Check(QuerySpecification querySpecification)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause);

    private TableOrViewReference? Check(FromClause fromClause)
    {
        var selectStatement = (SelectStatement?) fromClause.GetParents(_parentFragmentProvider).FirstOrDefault(a => a is SelectStatement);
        if (selectStatement is not null)
        {
            var table = Check(selectStatement);
            if (table is not null)
            {
                return table;
            }
        }

        foreach (var reference in fromClause.TableReferences ?? [])
        {
            if (reference is JoinTableReference joinTableReference)
            {
                var table = Check(joinTableReference);
                if (table is not null)
                {
                    return table;
                }
            }
            else
            {
                var table = CheckTableReference(reference as NamedTableReference);
                if (table is not null)
                {
                    return table;
                }
            }
        }

        return null;
    }

    private TableOrViewReference? Check(DeleteSpecification deleteSpecification)
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

    private TableOrViewReference? Check(JoinTableReference joinTableReference)
    {
        if (joinTableReference.FirstTableReference is JoinTableReference firstJoin)
        {
            var table = Check(firstJoin);
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
            var table = Check(secondJoin);
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
        return CheckTableReference(joinTableReference.FirstTableReference as NamedTableReference)
               ?? CheckTableReference(joinTableReference.SecondTableReference as NamedTableReference);
    }

    private TableOrViewReference? Check(SelectStatement selectStatement)
    {
        if (selectStatement.WithCtesAndXmlNamespaces?.CommonTableExpressions is null)
        {
            return null;
        }

        foreach (var cte in selectStatement.WithCtesAndXmlNamespaces.CommonTableExpressions)
        {
            if (_referenceToResolve.SchemaObject.BaseIdentifier.Value.EqualsOrdinalIgnoreCase(cte.ExpressionName.Value))
            {
                var currentDatabaseName = _script.TryFindCurrentDatabaseNameAtFragment(_referenceToResolve) ?? DatabaseNames.Unknown;
                var tableName = cte.ExpressionName.Value;
                var tableSchemaName = _defaultSchemaName;
                var fullObjectName = _referenceToResolve.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

                return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, TableSourceType.Cte, _referenceToResolve, fullObjectName);
            }
        }

        return null;
    }

    private TableOrViewReference? CheckTableReference(NamedTableReference? namedTableReference)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        if (!IsSearchedTable(namedTableReference))
        {
            return null;
        }

        var tableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var sourceType = _parentCtesByName.ContainsKey(tableName) ? TableSourceType.Cte : TableSourceType.TableOrView;

        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value
                                  ?? _script.TryFindCurrentDatabaseNameAtFragment(_referenceToResolve)
                                  ?? DatabaseNames.Unknown;
        var tableSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var fullObjectName = _referenceToResolve.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;

        return new TableOrViewReference(currentDatabaseName, tableSchemaName, tableName, sourceType, _referenceToResolve, fullObjectName);
    }

    private bool IsSearchedTable(NamedTableReference table)
    {
        if (ReferenceEquals(table, _referenceToResolve))
        {
            return true;
        }

        var table1Alias = table.Alias?.Value?.NullIfEmptyOrWhiteSpace();
        var table2Alias = _referenceToResolve.Alias?.Value?.NullIfEmptyOrWhiteSpace();

        if (table1Alias is not null && table2Alias is not null)
        {
            return table1Alias.EqualsOrdinalIgnoreCase(table2Alias); // both table-references have aliases
        }

        if (table1Alias is null && table2Alias is null)
        {
            return table.SchemaObject.Equals(_referenceToResolve.SchemaObject, _defaultSchemaName);
        }

        return string.Equals(table.Alias?.Value, _referenceToResolve.SchemaObject.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase)
               || string.Equals(_referenceToResolve.Alias?.Value, table.SchemaObject.BaseIdentifier?.Value, StringComparison.OrdinalIgnoreCase);
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
