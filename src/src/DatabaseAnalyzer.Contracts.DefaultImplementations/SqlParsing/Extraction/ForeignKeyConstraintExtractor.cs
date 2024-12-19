using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal sealed class ForeignKeyConstraintExtractor : Extractor<ForeignKeyConstraintInformation>
{
    public ForeignKeyConstraintExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<ForeignKeyConstraintInformation> ExtractCore(TSqlScript script)
    {
        var visitor = new ObjectExtractorVisitor<AlterTableAddTableElementStatement>(DefaultSchemaName);
        script.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetForeignKeyConstraint(a.Object, a.DatabaseName))
            .WhereNotNull()
            .ToList();
    }

    private ForeignKeyConstraintInformation? GetForeignKeyConstraint(AlterTableAddTableElementStatement statement, string? databaseName)
    {
        var fkConstraint = statement.Definition.TableConstraints
            ?.OfType<ForeignKeyConstraintDefinition>()
            .FirstOrDefault();

        if (fkConstraint is null)
        {
            return null;
        }

        var tableSchemaName = statement.SchemaObjectName.SchemaIdentifier?.Value ?? DefaultSchemaName;
        var tableName = statement.SchemaObjectName.BaseIdentifier.Value!;
        var calculatedDatabaseName =
            statement.SchemaObjectName.DatabaseIdentifier?.Value
            ?? databaseName;

        if (calculatedDatabaseName is null)
        {
            throw CreateUnableToDetermineTheDatabaseNameException("table", $"{tableSchemaName}.{tableName}", statement.GetCodeRegion());
        }

        if (statement.Definition.TableConstraints.IsNullOrEmpty())
        {
            return null;
        }

        return new ForeignKeyConstraintInformation
        (
            calculatedDatabaseName,
            tableSchemaName,
            tableName,
            fkConstraint.Columns[0].Value,
            fkConstraint.ConstraintIdentifier.Value!,
            fkConstraint.ReferenceTableName.SchemaIdentifier?.Value ?? DefaultSchemaName,
            fkConstraint.ReferenceTableName.BaseIdentifier.Value ?? DefaultSchemaName,
            fkConstraint.ReferencedTableColumns[0].Value
        );
    }
}
