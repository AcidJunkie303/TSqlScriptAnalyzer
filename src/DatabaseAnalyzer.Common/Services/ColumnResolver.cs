using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Services;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
public sealed class ColumnResolver : IColumnResolver
{
    private readonly IAstService _astService;
    private readonly string _defaultSchemaName;
    private readonly IIssueReporter _issueReporter;
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly string _relativeScriptFilePath;
    private readonly TSqlScript _script;

    public ColumnResolver(IIssueReporter issueReporter, IAstService astService, TSqlScript script, string relativeScriptFilePath, IParentFragmentProvider parentFragmentProvider, string defaultSchemaName)
    {
        _issueReporter = issueReporter;
        _astService = astService;
        _script = script;
        _relativeScriptFilePath = relativeScriptFilePath;
        _parentFragmentProvider = parentFragmentProvider;
        _defaultSchemaName = defaultSchemaName;
    }

    public static ColumnResolver Create(IScriptAnalysisContext context, IAstService astService)
        => new(context.IssueReporter, astService, context.Script.ParsedScript, context.Script.RelativeScriptFilePath, context.Script.ParentFragmentProvider, context.DefaultSchemaName);

    public static ColumnResolver Create(IGlobalAnalysisContext context, IAstService astService, IScriptModel script)
        => new(context.IssueReporter, astService, script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);

    public ColumnReference? Resolve(ColumnReferenceExpression columnReference)
    {
        if (columnReference.MultiPartIdentifier?.Identifiers is null)
        {
            return null;
        }

        if (_astService.IsChildOfFunctionEnumParameter(columnReference, _parentFragmentProvider))
        {
            return null;
        }

        var parentCtesByName = GetParentCtesByName(columnReference, _parentFragmentProvider);

        TSqlFragment? fragment = columnReference;

        while (true)
        {
            fragment = fragment.GetParent(_parentFragmentProvider);
            if (fragment is null)
            {
                return null;
            }

            var column = fragment switch
            {
                QualifiedJoin qualifiedJoin             => Check(qualifiedJoin, parentCtesByName, columnReference),
                DeleteSpecification deleteSpecification => Check(deleteSpecification, parentCtesByName, columnReference),
                FromClause fromClause                   => Check(fromClause, parentCtesByName, columnReference),
                QuerySpecification querySpecification   => Check(querySpecification, parentCtesByName, columnReference),
                UpdateSpecification updateSpecification => Check(updateSpecification, parentCtesByName, columnReference),
                MergeSpecification mergeSpecification   => Check(mergeSpecification, parentCtesByName, columnReference),
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

    private ColumnReference? Check(MergeSpecification mergeSpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
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
            var column = CheckTableReference(mergeSpecification.TableReference as NamedTableReference, parentCtesByName, referenceToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(mergeSpecification.Target as NamedTableReference, parentCtesByName, referenceToResolve);
    }

    private ColumnReference? Check(UpdateSpecification updateSpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
    {
        if (updateSpecification.FromClause is not null)
        {
            var column = Check(updateSpecification.FromClause, parentCtesByName, referenceToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(updateSpecification.Target as NamedTableReference, parentCtesByName, referenceToResolve);
    }

    private ColumnReference? Check(QuerySpecification querySpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
        => querySpecification.FromClause is null ? null : Check(querySpecification.FromClause, parentCtesByName, referenceToResolve);

    private ColumnReference? Check(FromClause fromClause, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
    {
        foreach (var tableReference in fromClause.TableReferences ?? [])
        {
            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                var column = Check(qualifiedJoin, parentCtesByName, referenceToResolve);
                if (column is not null)
                {
                    return column;
                }
            }
            else
            {
                var column = CheckTableReference(tableReference as NamedTableReference, parentCtesByName, referenceToResolve);
                if (column is not null)
                {
                    return column;
                }
            }
        }

        return null;
    }

    private ColumnReference? Check(DeleteSpecification deleteSpecification, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
    {
        if (deleteSpecification.FromClause is not null)
        {
            var column = Check(deleteSpecification.FromClause, parentCtesByName, referenceToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        return CheckTableReference(deleteSpecification.Target as NamedTableReference, parentCtesByName, referenceToResolve);
    }

    private ColumnReference? Check(QualifiedJoin qualifiedJoin, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
    {
        if (referenceToResolve.GetParent(_parentFragmentProvider) is AssignmentSetClause assignmentExpression)
        {
            if (assignmentExpression.Column == referenceToResolve)
            {
                return null; // we don't check the left part of update statement assignments
            }
        }

        if (referenceToResolve.MultiPartIdentifier.Identifiers.Count < 2)
        {
            ReportMissingAlias(referenceToResolve);
            return null;
        }

        if (qualifiedJoin.FirstTableReference is QualifiedJoin firstJoin)
        {
            var column = Check(firstJoin, parentCtesByName, referenceToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        if (qualifiedJoin.SecondTableReference is QualifiedJoin secondJoin)
        {
            var column = Check(secondJoin, parentCtesByName, referenceToResolve);
            if (column is not null)
            {
                return column;
            }
        }

        // qualifiedJoin.FirstTableReference can also be joins -> check previous joins too
        return CheckTableReference(qualifiedJoin.FirstTableReference as NamedTableReference, parentCtesByName, referenceToResolve)
               ?? CheckTableReference(qualifiedJoin.SecondTableReference as NamedTableReference, parentCtesByName, referenceToResolve);
    }

    private ColumnReference? CheckTableReference(NamedTableReference? namedTableReference, IReadOnlyDictionary<string, CommonTableExpression> parentCtesByName, ColumnReferenceExpression referenceToResolve)
    {
        if (namedTableReference is null)
        {
            return null;
        }

        var (tableNameOrAlias, columnName) = referenceToResolve.MultiPartIdentifier.GetColumnReferenceParts();
        var tableReferenceTableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
        var tableReferenceSchemaName = namedTableReference.SchemaObject.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var currentDatabaseName = namedTableReference.SchemaObject.DatabaseIdentifier?.Value
                                  ?? _script.TryFindCurrentDatabaseNameAtFragment(referenceToResolve)
                                  ?? DatabaseNames.Unknown;

        var tableReferenceIsCteReference = parentCtesByName.ContainsKey(namedTableReference.SchemaObject.BaseIdentifier.Value);
        if (tableReferenceIsCteReference)
        {
            return new ColumnReference(currentDatabaseName, tableReferenceSchemaName, tableReferenceTableName, columnName, TableSourceType.Cte, referenceToResolve, GetFullObjectName(), namedTableReference.Alias?.Value);
        }

        // if we don't have an alias, we have aborted earlier on in case there are multiple tables involved
        // Therefore, we assume that this is the table we're looking for
        if (tableNameOrAlias is null)
        {
            var fullObjectName = GetFullObjectName();
            return new ColumnReference(currentDatabaseName, tableReferenceSchemaName, tableReferenceTableName, columnName, TableSourceType.NotDetermined, referenceToResolve, fullObjectName, namedTableReference.Alias?.Value);
        }

        var tableReferenceAlias = namedTableReference.Alias?.Value;
        if (tableReferenceAlias is null)
        {
            return null;
        }

        var sourceType = parentCtesByName.ContainsKey(tableNameOrAlias) || parentCtesByName.ContainsKey(tableReferenceAlias)
            ? TableSourceType.Cte
            : TableSourceType.TableOrView;

        return tableReferenceAlias.EqualsOrdinalIgnoreCase(tableNameOrAlias)
            ? new ColumnReference(currentDatabaseName, tableReferenceSchemaName, tableReferenceTableName, columnName, sourceType, referenceToResolve, GetFullObjectName(), namedTableReference.Alias?.Value)
            : null;

        string GetFullObjectName()
            => referenceToResolve.TryGetFirstClassObjectName(_defaultSchemaName, _script, _parentFragmentProvider) ?? _relativeScriptFilePath;
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
            if (parent is not StatementWithCtesAndXmlNamespaces statementWithCtes)
            {
                continue;
            }

            if ((statementWithCtes.WithCtesAndXmlNamespaces?.CommonTableExpressions).IsNullOrEmpty())
            {
                return [];
            }

            return statementWithCtes.WithCtesAndXmlNamespaces.CommonTableExpressions
                .ToDictionary(
                    a => a.ExpressionName.Value,
                    a => a,
                    StringComparer.OrdinalIgnoreCase);
        }

        return [];
    }
}
