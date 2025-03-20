using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public sealed class TableExtractor : Extractor<TableInformation>
{
    public TableExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<TableInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<CreateTableStatement>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetTable(a.Object, a.DatabaseName, script))
            .Where(a => !a.ObjectName.IsTempTableName())
            .ToList();
    }

    private TableInformation GetTable(CreateTableStatement statement, string? databaseName, IScriptModel script)
    {
        var tableSchemaName = statement.SchemaObjectName.SchemaIdentifier?.Value ?? DefaultSchemaName;
        var tableName = statement.SchemaObjectName.BaseIdentifier.Value!;
        var calculatedDatabaseName = statement.SchemaObjectName.DatabaseIdentifier?.Value ?? databaseName ?? throw CreateUnableToDetermineTheDatabaseNameException("table", $"{tableSchemaName}.{tableName}", statement.GetCodeRegion());
        var columns = statement.Definition.ColumnDefinitions
            .Select(a => GetColumn(a, calculatedDatabaseName, tableSchemaName, tableName, script.RelativeScriptFilePath))
            .ToList();

        var directColumnIndices = columns
            .Where(a => a.IsUnique)
            .Select(a =>
            {
                var isPrimaryKey = a.ColumnDefinition.Constraints.Any(b => b is UniqueConstraintDefinition { IsPrimaryKey: true });
                var isClustered = a.ColumnDefinition.Constraints.Any(b => b is UniqueConstraintDefinition { Clustered    : true });
                var indexType = TableColumnIndexTypes.None;

                if (isPrimaryKey)
                {
                    indexType |= TableColumnIndexTypes.PrimaryKey | TableColumnIndexTypes.Unique;
                }

                if (isClustered)
                {
                    indexType |= TableColumnIndexTypes.Clustered;
                }

                if (a.IsUnique)
                {
                    indexType |= TableColumnIndexTypes.Unique;
                }

                return new IndexInformation
                (
                    calculatedDatabaseName,
                    tableSchemaName,
                    tableName,
                    IndexName: null,
                    indexType,
                    a.ObjectName.ToSingleItemFrozenSet(StringComparer.Ordinal),
                    FrozenSet<string>.Empty,
                    a.ColumnDefinition,
                    script.RelativeScriptFilePath
                );
            });

        var allForeignKeyConstraints = GetForeignKeyConstraints(statement, calculatedDatabaseName, tableSchemaName, tableName, script.RelativeScriptFilePath).ToList();
        var allIndices = GetIndices(statement, calculatedDatabaseName, tableSchemaName, tableName, script.RelativeScriptFilePath)
            .Concat(directColumnIndices)
            .ToList();
        var allColumns = statement.Definition.ColumnDefinitions
            .Select(a => GetColumn(a, calculatedDatabaseName, tableSchemaName, tableName, script.RelativeScriptFilePath))
            .ToList();
        var columnsByName = allColumns
            .ToFrozenDictionary(
                a => a.ObjectName,
                a => a,
                StringComparer.OrdinalIgnoreCase);

        return new TableInformation
        (
            calculatedDatabaseName,
            tableSchemaName,
            tableName,
            allColumns,
            columnsByName,
            allIndices,
            allForeignKeyConstraints,
            statement,
            script.RelativeScriptFilePath
        )
        {
            ScriptModel = script
        };
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
        var name = constraint.ConstraintIdentifier?.Value ?? "Unknown"; // Unique key constraints don't have a name

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
    }

    private static IndexInformation GetIndex(UniqueConstraintDefinition constraint, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
    {
        var indexType = TableColumnIndexTypes.Unique;

        if (constraint.Clustered.GetValueOrDefault())
        {
            indexType |= TableColumnIndexTypes.Clustered;
        }

        if (constraint.IsPrimaryKey)
        {
            indexType |= TableColumnIndexTypes.PrimaryKey;
        }

        return new IndexInformation(
            databaseName,
            tableSchemaName,
            tableName,
            constraint.ConstraintIdentifier?.Value,
            indexType,
            constraint.Columns
                .Select(static a => a.Column.MultiPartIdentifier.Identifiers[0].Value)
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            FrozenSet<string>.Empty,
            constraint,
            relativeScriptFilePath
        );
    }

    private static ColumnInformation GetColumn(ColumnDefinition column, string databaseName, string tableSchemaName, string tableName, string relativeScriptFilePath)
    {
        var isNullable = column.Constraints
            .OfType<NullableConstraintDefinition>()
            .FirstOrDefault(static a => a.Nullable)
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
