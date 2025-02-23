using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
public sealed class DatabaseObjectExtractor : IDatabaseObjectExtractor
{
    private readonly IIssueReporter _issueReporter;

    public DatabaseObjectExtractor(IIssueReporter issueReporter)
    {
        _issueReporter = issueReporter;
    }

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
        var views = new ViewExtractor(defaultSchemaName).Extract(scripts).ToList();
        ISchemaBoundObject[] allObjects = [.. schemas, .. aggregatedTables, .. functions, .. procedures];
        allObjects = RemoveAndReportDuplicates(allObjects);

        functions = allObjects.OfType<FunctionInformation>().ToList();
        aggregatedTables = allObjects.OfType<TableInformation>().ToList();
        procedures = allObjects.OfType<ProcedureInformation>().ToList();

        var functionsByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(functions, a => a.SchemaName);
        var proceduresByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(procedures, a => a.SchemaName);
        var tablesByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(aggregatedTables, a => a.SchemaName);
        var viewsByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(views, a => a.SchemaName);

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
                                    ?.ToDictionary(static table => table.ObjectName, table => table, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary()
                                ?? FrozenDictionary<string, TableInformation>.Empty.AsIReadOnlyDictionary(),
                                viewsByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToDictionary(static table => table.ObjectName, table => table, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary()
                                ?? FrozenDictionary<string, ViewInformation>.Empty.AsIReadOnlyDictionary(),
                                proceduresByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToDictionary(static procedure => procedure.ObjectName, procedure => procedure, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary()
                                ?? FrozenDictionary<string, ProcedureInformation>.Empty.AsIReadOnlyDictionary(),
                                functionsByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToDictionary(static function => function.ObjectName, function => function, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary()
                                ?? FrozenDictionary<string, FunctionInformation>.Empty.AsIReadOnlyDictionary(),
                                new CreateSchemaStatement(),
                                string.Empty
                            )
                        )
                )
            );
    }

    private ISchemaBoundObject[] RemoveAndReportDuplicates(ISchemaBoundObject[] objects)
    {
        var indicesWithoutName = objects
            .Where(a => a is IndexInformation { IndexName: null })
            .ToList();

        var objectsGroupedByName = objects
            // depending on the index type, some indices might not have a name
            .Where(a => a is not IndexInformation { IndexName: null })
            .GroupBy(a => a.FullNameParts.StringJoin(":::"), StringComparer.OrdinalIgnoreCase)
            .Select(a => a.ToList())
            .ToList();

        foreach (var databaseObjects in objectsGroupedByName)
        {
            if (databaseObjects.Count > 1)
            {
                var databaseObject = databaseObjects[0];
                var scriptFilePaths = databaseObjects.Select(static a => $"{a.RelativeScriptFilePath}").StringJoin(", ");

                _issueReporter.Report(WellKnownDiagnosticDefinitions.DuplicateObjectCreationStatement,
                    databaseObject.DatabaseName,
                    databaseObject.RelativeScriptFilePath,
                    databaseObject.FullNameParts.StringJoin('.'),
                    databaseObject.CreationStatement.GetCodeRegion(),
                    databaseObject.FullNameParts.StringJoin('.'),
                    scriptFilePaths
                );
            }
        }

        return objectsGroupedByName
            .Where(static a => a.Count == 1)
            .Select(static a => a[0])
            .Concat(indicesWithoutName)
            .ToArray();
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
            .GroupBy(a => $"{a.DatabaseName}.{a.SchemaName}.{a.ObjectName}", StringComparer.OrdinalIgnoreCase)
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
                    ).AsIReadOnlyDictionary()
            ).AsIReadOnlyDictionary();
    }
}
