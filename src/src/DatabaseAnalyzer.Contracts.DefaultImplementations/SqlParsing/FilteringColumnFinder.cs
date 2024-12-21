// TODO: remove

#pragma warning disable
namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

/*
public enum TableSourceType
{
    Unknown = 0,
    TableOrView = 1,
    Cte = 2,
    TempTable = 3
}

internal sealed class FilteringColumnFinder
{
    private readonly string _defaultSchemaName;
    private readonly List<FilteringColumn> _filteringColumns = [];
    private readonly IParentFragmentProvider _parentFragmentProvider;
    private readonly TSqlScript _script;

    public FilteringColumnFinder(TSqlScript script, string defaultSchemaName)
        : this(script, defaultSchemaName, script.CreateParentFragmentProvider())
    {
    }

    public FilteringColumnFinder(TSqlScript script, string defaultSchemaName, IParentFragmentProvider parentFragmentProvider)
    {
        _script = script;
        _defaultSchemaName = defaultSchemaName;
        _parentFragmentProvider = parentFragmentProvider;
    }

    public IReadOnlyList<FilteringColumn> Find(TSqlScript script)
    {
        foreach (var columnReference in script.GetChildren<ColumnReferenceExpression>(recursive: true))
        {
            if (!IsUsedInComparison(columnReference))
            {
                continue;
            }

            var filteringColumn = GetFilteringColumnFromSelectStatement(columnReference);
            if (filteringColumn is not null)
            {
                _filteringColumns.Add(filteringColumn);
            }
        }

        return _filteringColumns;

        bool IsUsedInComparison(ColumnReferenceExpression column)
        {
            return column
                .GetParents(_parentFragmentProvider)
                .Any(a => a is BooleanComparisonExpression or InPredicate);
        }
    }

    private FilteringColumn? GetFilteringColumnFromSelectStatement(ColumnReferenceExpression columnReference)
    {
        var querySpecifications = columnReference
            .GetParents(_parentFragmentProvider)
            .OfType<QuerySpecification>();


        foreach (var querySpecification in querySpecifications)
        {
            var filteringColumn = GetFilteringColumn(columnReference, querySpecification);
            if (filteringColumn is not null)
            {
                return filteringColumn;
            }
        }

        return null;
    }

    private FilteringColumn? GetFilteringColumn(ColumnReferenceExpression columnReference, QuerySpecification querySpecification)
    {
        var tableReferences = querySpecification.FromClause?.TableReferences;
        if (tableReferences.IsNullOrEmpty())
        {
            return null;
        }

        var hasMultipleTableReferencesOrJoins = (tableReferences.Count > 1) || tableReferences.OfType<JoinTableReference>().Any();
        if (hasMultipleTableReferencesOrJoins && (columnReference.MultiPartIdentifier.Count == 1))
        {
            // Since we have only a one-part column identifier (column name only),
            // we cannot resolve to which table this column belongs to without the db schema information.
            // Therefore, we abort.
            // This situation is handled by an analyzer though. So the user is made aware of it
            return null;
        }

        var databaseName = columnReference.FindCurrentDatabaseNameAtFragment(_script);

        // we search for the parent FromClause
        if (columnReference.MultiPartIdentifier.Count == 1)
        {
            if (tableReferences[0] is not NamedTableReference namedTableReference)
            {
                return null;
            }

            var tableName = namedTableReference.SchemaObject.BaseIdentifier.Value;
            var columnName = columnReference.MultiPartIdentifier.Identifiers[0].Value;
            var schemaName = columnReference.MultiPartIdentifier.GetSchemaName(_defaultSchemaName);
            return new FilteringColumn(databaseName, tableName, schemaName, columnName, TableSourceType.TableOrView, columnReference);
        }

        var aliasOrTableName = columnReference.MultiPartIdentifier.Identifiers[1].Value;

        var querySpecifications = columnReference
            .GetParents(_parentFragmentProvider)
            .OfType<QuerySpecification>();

        var tableReferences = columnReference
            .GetParents(_parentFragmentProvider)
            .OfType<TableReference>();

        foreach (var querySpecification in querySpecifications)
        {
            if (querySpecification?.FromClause?.TableReferences is null)
            {
            }
        }


        return null;


//       if (hasMultipleTableReferencesOrJoins >)


//           if (hasMultipleTableReferencesOrJoins)
//           {
//               var databaseName = columnReference.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
//               var fullObjectName = columnReference.TryGetFirstClassObjectName(context, script);
//               context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
//           }
    }

    private FilteringColumn? TryGetFilteringColumnFrom(ColumnReferenceExpression columnReference)internal sealed record FilteringColumn(string DatabaseName, string SchemaName, string? TableName, string ColumnName, TableSourceType SourceType, TSqlFragment Fragment)
    {
        public override string ToString() => $"{SourceType}: {DatabaseName}.{SchemaName}.{TableName}.{ColumnName} at {(Fragment is null ? "NULL" : Fragment.GetCodeRegion().ToString())}";
    }

    // TODO: give the method a better name

    //
    //           foreach (var columnReference in script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
    //       {
    //           Analyze(context, script, columnReference);
    //       }
//
//
//
    //   private static void Analyze(IAnalysisContext context, IScriptModel script, ColumnReferenceExpression columnReference)
    //   {
    //       var querySpecification = columnReference
    //           .GetParents(script.ParentFragmentProvider)
    //           .OfType<QuerySpecification>()
    //           .FirstOrDefault();
//
    //       if (querySpecification?.FromClause is null)
    //       {
    //           return;
    //       }
//
    //       if (columnReference.MultiPartIdentifier.Count > 1)
    //       {
    //           return;
    //       }
//
    //       var tableReferences = querySpecification.FromClause.TableReferences;
//
    //       if ((tableReferences.Count > 1) || tableReferences.OfType<JoinTableReference>().Any())
    //       {
    //           var databaseName = columnReference.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
    //           var fullObjectName = columnReference.TryGetFirstClassObjectName(context, script);
    //           context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), columnReference.GetSql());
    //       }
    //   }
}
*/
