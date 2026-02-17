using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

[SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes")]
public sealed class DatabaseObjectExtractor : IDatabaseObjectExtractor
{
    private readonly IIssueReporter _issueReporter;
    private readonly Aj9002Settings _aj9002Settings;

    public DatabaseObjectExtractor(IIssueReporter issueReporter, Aj9002Settings aj9002Settings)
    {
        _issueReporter = issueReporter;
        _aj9002Settings = aj9002Settings;
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    public IReadOnlyDictionary<string, DatabaseInformation> Extract(IReadOnlyCollection<IScriptModel> scripts, string defaultSchemaName)
    {
        var schemas = Deduplicate(new SchemaExtractor(defaultSchemaName).Extract(scripts));
        var functions = Deduplicate(new FunctionExtractor(defaultSchemaName).Extract(scripts));
        var procedures = Deduplicate(new ProcedureExtractor(defaultSchemaName).Extract(scripts));
        var synonyms = Deduplicate(new SynonymExtractor(defaultSchemaName).Extract(scripts));
        var tables = Deduplicate(new TableExtractor(defaultSchemaName).Extract(scripts));
        var indices = Deduplicate(new IndexExtractor(defaultSchemaName).Extract(scripts).Where(a => !a.TableName.IsTempTableName()));
        var foreignKeyConstraints = Deduplicate(new ForeignKeyConstraintExtractor(defaultSchemaName).Extract(scripts));
        var aggregatedTables = Deduplicate(AggregateTables(tables, foreignKeyConstraints, indices));
        var views = Deduplicate(new ViewExtractor(defaultSchemaName).Extract(scripts));
        ISchemaBoundObject[] allObjects = [.. schemas, .. aggregatedTables, ..views, .. functions, .. procedures, .. synonyms];
        allObjects = RemoveAndReportDuplicates(allObjects);

        var synonymsByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(synonyms, a => a.SchemaName);
        var functionsByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(functions, a => a.SchemaName);
        var proceduresByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(procedures, a => a.SchemaName);
        var tablesByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(aggregatedTables, a => a.SchemaName);
        var viewsByDatabaseNameBySchemaName = GroupByDatabaseNameBySchemaName(views, a => a.SchemaName);

        return allObjects
            .GroupBy(a => a.DatabaseName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(
                comparer: StringComparer.OrdinalIgnoreCase,
                keySelector: db => db.Key,
                elementSelector: db => new DatabaseInformation
                (
                    db.Key,
                    db
                        .GroupBy(schema => schema.SchemaName, StringComparer.OrdinalIgnoreCase)
                        .ToFrozenDictionary(
                            comparer: StringComparer.OrdinalIgnoreCase,
                            keySelector: schema => schema.Key,
                            elementSelector: schema => new SchemaInformationWithObjects
                            (
                                db.Key,
                                schema.Key,
                                tablesByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToFrozenDictionary(static table => table.ObjectName, table => table, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary() ?? FrozenDictionary<string, TableInformation>.Empty.AsIReadOnlyDictionary(),
                                viewsByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToFrozenDictionary(static view => view.ObjectName, view => view, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary() ?? FrozenDictionary<string, ViewInformation>.Empty.AsIReadOnlyDictionary(),
                                proceduresByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToFrozenDictionary(static procedure => procedure.ObjectName, procedure => procedure, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary() ?? FrozenDictionary<string, ProcedureInformation>.Empty.AsIReadOnlyDictionary(),
                                functionsByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToFrozenDictionary(static function => function.ObjectName, function => function, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary() ?? FrozenDictionary<string, FunctionInformation>.Empty.AsIReadOnlyDictionary(),
                                synonymsByDatabaseNameBySchemaName
                                    .GetValueOrDefault(db.Key)
                                    ?.GetValueOrDefault(schema.Key)
                                    ?.ToFrozenDictionary(static synonym => synonym.ObjectName, synonym => synonym, StringComparer.OrdinalIgnoreCase)
                                    .AsIReadOnlyDictionary() ?? FrozenDictionary<string, SynonymInformation>.Empty.AsIReadOnlyDictionary(),
                                new CreateSchemaStatement(),
                                string.Empty
                            )
                        )
                )
            );
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

    private ISchemaBoundObject[] RemoveAndReportDuplicates(ISchemaBoundObject[] objects)
    {
        var indicesWithoutName = objects
            .Where(static a => a is IndexInformation { IndexName: null })
            .ToList();

        var objectsGroupedByName = objects
            // depending on the index type, some indices might not have a name
            .Where(static a => a is not IndexInformation { IndexName: null })
            .GroupBy(static a => a.FullNameParts.StringJoin(":::"), StringComparer.OrdinalIgnoreCase)
            .Select(static a => a.ToList())
            .ToList();

        foreach (var databaseObjects in objectsGroupedByName)
        {
            if (databaseObjects.Count > 1)
            {
                CheckAndReportDuplicates(databaseObjects);
            }
        }

        return objectsGroupedByName
            .Select(static a => a[0])
            .Concat(indicesWithoutName)
            .ToArray();
    }

    private List<T> Deduplicate<T>(IEnumerable<T> source)
        where T : IDatabaseObject
    {
        var grouped = source
            .GroupBy(static a => a.FullName, static (_, objects) => objects.ToList(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in grouped)
        {
            if (group.Count > 1)
            {
                CheckAndReportDuplicates(group);
            }
        }

        return grouped.ConvertAll(static a => a[0]);
    }

    private void CheckAndReportDuplicates<T>(IReadOnlyList<T> duplicateDatabaseObjects)
        where T : IDatabaseObject
    {
        var filteredDatabaseObjects = duplicateDatabaseObjects
            .Where(a => !IsScriptExcluded(a.RelativeScriptFilePath, _aj9002Settings))
            .ToList();

        if (filteredDatabaseObjects.Count < 2)
        {
            return;
        }

        Report(filteredDatabaseObjects);

        static bool IsScriptExcluded(string relativeScriptFilePath, Aj9002Settings settings)
            => settings.ExcludedFilePathPatterns.Count != 0 && settings.ExcludedFilePathPatterns.Any(pattern => pattern.IsMatch(relativeScriptFilePath));
    }

    private void Report<T>(IReadOnlyList<T> duplicateDatabaseObjects)
        where T : IDatabaseObject
    {
        _issueReporter.Report(WellKnownDiagnosticDefinitions.DuplicateObjectCreationStatement,
            duplicateDatabaseObjects[0].DatabaseName,
            duplicateDatabaseObjects[0].RelativeScriptFilePath,
            duplicateDatabaseObjects[0].FullName,
            duplicateDatabaseObjects[0].CreationStatement.GetCodeRegion(),
            duplicateDatabaseObjects[0].FullName,
            duplicateDatabaseObjects.Select(a => a.RelativeScriptFilePath).StringJoin("\n")
        );
    }

    private List<TableInformation> AggregateTables(IReadOnlyList<TableInformation> tables, IReadOnlyList<ForeignKeyConstraintInformation> foreignKeyConstraints, IReadOnlyList<IndexInformation> indices)
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
                var matchingTables = a.ToList();
                var table = matchingTables[0];
                if (matchingTables.Count > 1)
                {
                    Report(matchingTables);
                }

                var key = a.Key;
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
}
