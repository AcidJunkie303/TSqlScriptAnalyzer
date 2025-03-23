using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public sealed class ForeignKeyConstraintExtractor : Extractor<ForeignKeyConstraintInformation>
{
    public ForeignKeyConstraintExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<ForeignKeyConstraintInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<AlterTableAddTableElementStatement>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects
            .Select(a => GetForeignKeyConstraint(a.Object, a.DatabaseName, script))
            .WhereNotNull()
            .ToList();
    }

    private ForeignKeyConstraintInformation? GetForeignKeyConstraint(AlterTableAddTableElementStatement statement, string? databaseName, IScriptModel script)
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
        var calculatedDatabaseName = statement.SchemaObjectName.DatabaseIdentifier?.Value ?? databaseName ?? throw CreateUnableToDetermineTheDatabaseNameException("table", $"{tableSchemaName}.{tableName}", statement.GetCodeRegion());
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
            fkConstraint.ReferencedTableColumns[0].Value,
            statement,
            script.RelativeScriptFilePath
        );
    }
}
