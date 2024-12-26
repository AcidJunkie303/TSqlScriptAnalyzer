using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

public sealed class TableExtractor : Extractor<TableInformation>
{
    public TableExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<TableInformation> ExtractCore(TSqlScript script, string relativeScriptFilePath)
    {
        var visitor = new ObjectExtractorVisitor<CreateTableStatement>(DefaultSchemaName);
        script.AcceptChildren(visitor);

        return visitor.Objects.ConvertAll(a => GetTable(a.Object, a.DatabaseName, relativeScriptFilePath));
    }

    private TableInformation GetTable(CreateTableStatement statement, string? databaseName, string relativeScriptFilePath)
    {
        var tableSchemaName = statement.SchemaObjectName.SchemaIdentifier?.Value ?? DefaultSchemaName;
        var tableName = statement.SchemaObjectName.BaseIdentifier.Value!;

        var calculatedDatabaseName = statement.SchemaObjectName.DatabaseIdentifier?.Value ?? databaseName ?? throw CreateUnableToDetermineTheDatabaseNameException("table", $"{tableSchemaName}.{tableName}", statement.GetCodeRegion());
        var columns = statement.Definition.ColumnDefinitions
            .Select(a => GetColumn(a, calculatedDatabaseName, tableSchemaName, tableName, relativeScriptFilePath))
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
                [a.ObjectName],
                [],
                a.ColumnDefinition,
                relativeScriptFilePath
            ));

        return new TableInformation
        (
            calculatedDatabaseName,
            tableSchemaName,
            tableName,
            statement.Definition.ColumnDefinitions
                .Select(a => GetColumn(a, calculatedDatabaseName, tableSchemaName, tableName, relativeScriptFilePath))
                .ToList(),
            GetIndices(statement, calculatedDatabaseName, tableSchemaName, tableName, relativeScriptFilePath).Concat(uniqueColumnIndices).ToList(),
            GetForeignKeyConstraints(statement, calculatedDatabaseName, tableSchemaName, tableName, relativeScriptFilePath).ToList(),
            statement,
            relativeScriptFilePath
        );
    }

    private static IEnumerable<IndexInformation> GetIndices(CreateTableStatement statement, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
    {
        return statement.Definition.TableConstraints
            .OfType<UniqueConstraintDefinition>()
            .Select(a => GetIndex(a, databaseName, tableSchemaName, tableName, relativeScriptFilePath));
    }

    private IEnumerable<ForeignKeyConstraintInformation> GetForeignKeyConstraints(CreateTableStatement statement, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
        => statement.Definition.TableConstraints
            .OfType<ForeignKeyConstraintDefinition>()
            .Select(a => GetForeignKeyConstraint(a, databaseName, tableSchemaName, tableName, relativeScriptFilePath));

    private ForeignKeyConstraintInformation GetForeignKeyConstraint(ForeignKeyConstraintDefinition constraint, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
    {
        var name = constraint.ConstraintIdentifier?.Value ?? "Unknown";

        return new ForeignKeyConstraintInformation(
            databaseName,
            tableSchemaName,
            tableName,
            constraint.Columns[0].Value,
            name,
            constraint.ReferenceTableName.SchemaIdentifier?.Value ?? DefaultSchemaName,
            constraint.ReferenceTableName.BaseIdentifier.Value!,
            constraint.ReferencedTableColumns[0].Value,
            constraint,
            relativeScriptFilePath
        );
        /*
            string DatabaseName,
            string SchemaName,
            string TableName,
            string ColumnName,
            string ObjectName,
            string ReferencedTableSchemaName,
            string ReferencedTableName,
            string ReferencedTableColumnName,
            TSqlFragment CreationStatement,
        */
    }

    private static IndexInformation GetIndex(UniqueConstraintDefinition constraint, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
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
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            [],
            constraint,
            relativeScriptFilePath
        );
    }

    private static ColumnInformation GetColumn(ColumnDefinition column, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
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
            column,
            column,
            relativeScriptFilePath
        );
    }
}
