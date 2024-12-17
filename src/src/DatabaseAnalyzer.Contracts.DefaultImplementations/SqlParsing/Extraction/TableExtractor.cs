using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class TableExtractor : Extractor<TableInformationRaw>
{
    private readonly string _defaultSchemaName;

    public TableExtractor(string defaultSchemaName)
    {
        _defaultSchemaName = defaultSchemaName;
    }

    protected override List<TableInformationRaw> ExtractCore(TSqlScript script, string defaultSchemaName)
    {
        var visitor2 = new ObjectExtractorVisitor<CreateTableStatement>();
        script.AcceptChildren(visitor2);

        return visitor2.Objects.ConvertAll(a => GetTable(a.Object, a.DatabaseName));
    }

    private TableInformationRaw GetTable(CreateTableStatement statement, string? databaseName)
    {
        var tableSchemaName = statement.SchemaObjectName.SchemaIdentifier?.Value ?? _defaultSchemaName;
        var tableName = statement.SchemaObjectName.BaseIdentifier.Value!;

        var calculatedDatabaseName =
            statement.SchemaObjectName.DatabaseIdentifier?.Value
            ?? databaseName;
        if (calculatedDatabaseName is null)
        {
            throw CreateUnableToDetermineTheDatabaseNameException("table", $"{tableSchemaName}.{tableName}", statement.GetCodeRegion());
        }

        var columns = statement.Definition.ColumnDefinitions
            .Select(a => GetColumn(a, calculatedDatabaseName, tableSchemaName, tableName))
            .ToList();

        var uniqueColumnIndices = columns
            .Where(a => a.IsUnique)
            .Select(a => new IndexInformation
            (
                calculatedDatabaseName,
                tableSchemaName,
                tableName,
                null,
                TableColumnIndexType.Unique,
                [a.ColumnName],
                []
            ));

        return new TableInformationRaw
        {
            SchemaName = tableSchemaName,
            TableName = tableName,
            DatabaseName = calculatedDatabaseName,
            Columns = statement.Definition.ColumnDefinitions
                .Select(a => GetColumn(a, calculatedDatabaseName, tableSchemaName, tableName))
                .ToList(),
            Indices = GetIndices(statement, calculatedDatabaseName, tableSchemaName, tableName).Concat(uniqueColumnIndices).ToList(),
            ForeignKeys = GetForeignKeyConstraints(statement, calculatedDatabaseName, tableSchemaName, tableName).ToList()
        };
    }

    private static IEnumerable<IndexInformation> GetIndices(CreateTableStatement statement, string databaseName, string tableSchemaName, string tableName)
    {
        return statement.Definition.TableConstraints
            .OfType<UniqueConstraintDefinition>()
            .Select(a => GetIndex(a, databaseName, tableSchemaName, tableName));
    }

    private IEnumerable<ForeignKeyConstraintInformation> GetForeignKeyConstraints(CreateTableStatement statement, string databaseName, string tableSchemaName, string tableName)
        => statement.Definition.TableConstraints
            .OfType<ForeignKeyConstraintDefinition>()
            .Select(a => GetForeignKeyConstraint(a, databaseName, tableSchemaName, tableName));

    private ForeignKeyConstraintInformation GetForeignKeyConstraint(ForeignKeyConstraintDefinition constraint, string databaseName, string tableSchemaName, string tableName)
    {
        var name = constraint.ConstraintIdentifier?.Value ?? "Unknown";

        return new ForeignKeyConstraintInformation(
            databaseName,
            tableSchemaName,
            tableName,
            name,
            constraint.Columns[0].Value,
            constraint.ReferenceTableName.SchemaIdentifier?.Value ?? _defaultSchemaName,
            constraint.ReferenceTableName.BaseIdentifier.Value!,
            constraint.ReferencedTableColumns[0].Value
        );
    }

    private static IndexInformation GetIndex(UniqueConstraintDefinition constraint, string databaseName, string tableSchemaName, string tableName)
    {
        var extractedType = constraint.IndexType.IndexTypeKind ?? IndexTypeKind.NonClustered;

        var indexType = TableColumnIndexType.None;

        if (extractedType.HasFlag(IndexTypeKind.Clustered))
        {
            indexType |= TableColumnIndexType.Clustered;
        }

        if (constraint.IsPrimaryKey)
        {
            indexType |= TableColumnIndexType.PrimaryKey;
        }

        return new IndexInformation(
            databaseName,
            tableSchemaName,
            tableName,
            constraint.ConstraintIdentifier?.Value,
            indexType,
            constraint.Columns
                .Select(a => a.Column.MultiPartIdentifier.Identifiers[0].Value)
                .ToList(),
            []
        );
    }

    private static ColumnInformation GetColumn(ColumnDefinition column, string databaseName, string tableSchemaName, string tableName)
    {
        var isNullable = column.Constraints
            .OfType<NullableConstraintDefinition>()
            .FirstOrDefault(a => a.Nullable)
            ?.Nullable ?? false;
        var isUnique = column.Constraints
            .OfType<UniqueConstraintDefinition>()
            .Any();

        return new ColumnInformation
        (
            databaseName,
            tableSchemaName,
            tableName,
            column.ColumnIdentifier.Value,
            isNullable,
            isUnique,
            column.DataType
        );
    }
}
