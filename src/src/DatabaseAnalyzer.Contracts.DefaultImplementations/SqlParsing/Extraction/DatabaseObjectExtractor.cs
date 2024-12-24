using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

public interface IDatabaseObjectExtractor
{
    IReadOnlyDictionary<string, DatabaseInformation> Extract(IReadOnlyCollection<IScriptModel> scripts, string defaultSchemaName);
}

public sealed class DatabaseObjectExtractor : IDatabaseObjectExtractor
{
    [SuppressMessage("Design", "MA0051:Method is too long")]
    public IReadOnlyDictionary<string, DatabaseInformation> Extract(IReadOnlyCollection<IScriptModel> scripts, string defaultSchemaName)
    {
        var schemas = new SchemaExtractor(defaultSchemaName).Extract(scripts).ToList();
        var functions = new FunctionExtractor(defaultSchemaName).Extract(scripts).ToList();
        var procedures = new ProcedureExtractor(defaultSchemaName).Extract(scripts).ToList();
        var tables = new TableExtractor(defaultSchemaName).Extract(scripts).ToList();
        var indices = new IndexExtractor(defaultSchemaName).Extract(scripts).ToList();
        var foreignKeyConstraints = new ForeignKeyConstraintExtractor(defaultSchemaName).Extract(scripts).ToList();
        var aggregatedTables = AggregateTables(tables, foreignKeyConstraints, indices);

        var functionsByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(functions, a => a.SchemaName);
        var proceduresByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(procedures, a => a.SchemaName);
        var tablesByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(aggregatedTables, a => a.SchemaName);

        ISchemaBoundObject[] allObjects = [.. schemas, .. aggregatedTables, .. functions, .. procedures];

        return allObjects
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                comparer: StringComparer.OrdinalIgnoreCase,
                keySelector: db => db.Key,
                elementSelector: db => new DatabaseInformation
                (
                    db.Key,
                    db
                        .GroupBy(schema => schema.SchemaName, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            comparer: StringComparer.OrdinalIgnoreCase,
                            keySelector: schema => schema.Key,
                            elementSelector: schema => new SchemaInformationWithObjects
                            (
                                db.Key,
                                schema.Key,
                                tablesByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToDictionary(table => table.TableName, table => table, StringComparer.OrdinalIgnoreCase)
                                    ?.AsIReadOnlyDictionary() ?? FrozenDictionary<string, TableInformation>.Empty.AsIReadOnlyDictionary(),
                                proceduresByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToDictionary(procedure => procedure.ProcedureName, procedure => procedure, StringComparer.OrdinalIgnoreCase)
                                    ?.AsIReadOnlyDictionary() ?? FrozenDictionary<string, ProcedureInformation>.Empty.AsIReadOnlyDictionary(),
                                functionsByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToDictionary(function => function.FunctionName, function => function, StringComparer.OrdinalIgnoreCase)
                                    ?.AsIReadOnlyDictionary() ?? FrozenDictionary<string, FunctionInformation>.Empty.AsIReadOnlyDictionary()
                            )
                        )
                )
            );
    }

    private static List<TableInformation> AggregateTables(IReadOnlyList<TableInformation> tables, IReadOnlyList<ForeignKeyConstraintInformation> foreignKeyConstraints, IReadOnlyList<IndexInformation> indices)
    {
        var foreignKeyConstraintsByTableFullName = foreignKeyConstraints
            .GroupBy(
                a => $"{a.DatabaseName}.{a.SchemaName}.{a.TableName}",
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                a => a.Key,
                a => a.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var indicesByFullTableName = indices
            .GroupBy(
                a => $"{a.DatabaseName}.{a.SchemaName}.{a.TableName}",
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                a => a.Key,
                a => a.ToList(),
                StringComparer.OrdinalIgnoreCase);

        return tables
            .GroupBy(a => $"{a.DatabaseName}.{a.SchemaName}.{a.TableName}", StringComparer.OrdinalIgnoreCase)
            .Select(a =>
            {
                var key = a.Key;
                var table = a.First();
                var tableIndices = indicesByFullTableName.GetValueOrDefault(key, []);
                var tableForeignKeyConstraints = foreignKeyConstraintsByTableFullName.GetValueOrDefault(key, []);

                return table with
                {
                    ForeignKeys = [.. table.ForeignKeys, .. tableForeignKeyConstraints],
                    Indices = [.. table.Indices, .. tableIndices]
                };
            })
            .ToList();
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, List<T>>>
        GroupByDatabaseNameBySchemaName<T>(IReadOnlyList<T> items, Func<T, string> schemaNameGetter)
        where T : IDatabaseObject
    {
        return items
            .GroupBy(x => x.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary
            (
                comparer: StringComparer.OrdinalIgnoreCase,
                keySelector: a => a.Key,
                elementSelector: a => a
                    .GroupBy(schemaNameGetter, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary
                    (
                        comparer: StringComparer.OrdinalIgnoreCase,
                        keySelector: b => b.Key,
                        elementSelector: b => b.ToList()
                    )
                    .AsIReadOnlyDictionary()
            )
            .AsIReadOnlyDictionary();
    }
}
