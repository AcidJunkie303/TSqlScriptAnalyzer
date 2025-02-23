using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using DatabaseAnalyzers.DefaultAnalyzers.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class IndexNamingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5052Settings>();
        var createIndexStatements = script.ParsedScript
            .GetChildren(recursive: true)
            .Where(IsCreateIndexStatement);

        foreach (var statement in createIndexStatements)
        {
            AnalyzeCreateIndexStatement(context, script, settings, statement);
        }

        foreach (var statement in script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeCreateTableStatement(context, script, settings, statement);
        }

        foreach (var statement in script.ParsedScript.GetChildren<AlterTableAddTableElementStatement>(recursive: true))
        {
            AnalyeAlterTableStatement(context, script, settings, statement);
        }
    }

    private static bool IsCreateIndexStatement(TSqlFragment statement)
        => statement is
            CreateIndexStatement
            or CreateSpatialIndexStatement
            or CreateXmlIndexStatement
            or CreateSelectiveXmlIndexStatement
            or CreateColumnStoreIndexStatement
            or CreateFullTextIndexStatement;

    private static void AnalyzeCreateTableStatement(IAnalysisContext context, IScriptModel script, Aj5052Settings settings, CreateTableStatement statement)
    {
        var primaryKeyDefinition = statement.Definition.TableConstraints
            .OfType<UniqueConstraintDefinition>()
            .FirstOrDefault(a => a.IsPrimaryKey);

        if (primaryKeyDefinition is null)
        {
            return;
        }

        var tableSchemaName = statement.SchemaObjectName?.SchemaIdentifier?.Value ?? context.DefaultSchemaName;
        var tableName = statement.SchemaObjectName?.BaseIdentifier.Value ?? Constants.UnknownObjectName;
        AnalyzeUniqueConstraintDefinition(context, script, settings, primaryKeyDefinition, tableSchemaName, tableName);
    }

    private static void AnalyeAlterTableStatement(IAnalysisContext context, IScriptModel script, Aj5052Settings settings, AlterTableAddTableElementStatement statement)
    {
        var primaryKeyDefinition = statement.Definition.TableConstraints
            .OfType<UniqueConstraintDefinition>()
            .FirstOrDefault(a => a.IsPrimaryKey);

        if (primaryKeyDefinition is null)
        {
            return;
        }

        var tableSchemaName = statement.SchemaObjectName?.SchemaIdentifier?.Value ?? context.DefaultSchemaName;
        var tableName = statement.SchemaObjectName?.BaseIdentifier.Value ?? Constants.UnknownObjectName;

        AnalyzeUniqueConstraintDefinition(context, script, settings, primaryKeyDefinition, tableSchemaName, tableName);
    }

    private static void AnalyzeUniqueConstraintDefinition(IAnalysisContext context, IScriptModel script, Aj5052Settings settings, UniqueConstraintDefinition primaryKeyDefinition, string tableSchemaName, string tableName)
    {
        if (!primaryKeyDefinition.IsPrimaryKey)
        {
            return;
        }

        var indexProperties = primaryKeyDefinition.Clustered switch
        {
            true  => IndexProperties.PrimaryKey | IndexProperties.Clustered,
            false => IndexProperties.PrimaryKey | IndexProperties.NonClustered,
            _     => IndexProperties.PrimaryKey
        };

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(primaryKeyDefinition) ?? Constants.UnknownObjectName;
        var indexData = new IndexData
        (
            indexProperties,
            primaryKeyDefinition.ConstraintIdentifier,
            primaryKeyDefinition.ConstraintIdentifier.Value,
            databaseName,
            tableSchemaName,
            tableName,
            primaryKeyDefinition.Columns.Select(a => a.Column.MultiPartIdentifier.Identifiers.Select(x => x.Value).StringJoin(".").NullIfEmptyOrWhiteSpace() ?? Constants.UnknownObjectName).ToList()
        );

        var expectedIndexName = CreateIndexName(settings, indexData);
        if (indexData.Name.Equals(expectedIndexName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        context.IssueReporter.Report(DiagnosticDefinitions.Default, indexData.DatabaseName, script.RelativeScriptFilePath, indexData.FullObjectName, indexData.Identifier.GetCodeRegion(),
            indexData.Name, expectedIndexName, indexData.IndexProperties);
    }

    private static void AnalyzeCreateIndexStatement(IAnalysisContext context, IScriptModel script, Aj5052Settings settings, TSqlFragment fragment)
    {
        var indexData = GetIndexProperties(script, context.DefaultSchemaName, fragment);
        if (indexData is null)
        {
            return;
        }

        var expectedIndexName = CreateIndexName(settings, indexData);
        if (indexData.Name.Equals(expectedIndexName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        context.IssueReporter.Report(DiagnosticDefinitions.Default, indexData.DatabaseName, script.RelativeScriptFilePath, indexData.FullObjectName, indexData.Identifier.GetCodeRegion(),
            indexData.Name, expectedIndexName, indexData.IndexProperties);
    }

    private static string CreateIndexName(Aj5052Settings settings, IndexData indexData)
    {
        var pattern = GetPatternForIndexProperties(settings, indexData.IndexProperties);
        return PopulatePattern(pattern, indexData);
    }

    private static string PopulatePattern(string pattern, IndexData indexData)
    {
        var result = pattern
            .Replace(Aj5052Settings.Placeholders.DatabaseName, indexData.DatabaseName, StringComparison.OrdinalIgnoreCase)
            .Replace(Aj5052Settings.Placeholders.TableSchemaName, indexData.TableSchemaName, StringComparison.OrdinalIgnoreCase)
            .Replace(Aj5052Settings.Placeholders.TableName, indexData.TableName, StringComparison.OrdinalIgnoreCase);

        if (indexData.ColumnNames.Count > 0)
        {
            var flatColumnNames = indexData.ColumnNames.StringJoin("_");
            result = result.Replace(Aj5052Settings.Placeholders.ColumnNames, flatColumnNames, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string GetPatternForIndexProperties(Aj5052Settings settings, IndexProperties indexProperties)
    {
        foreach (var (properties, pattern) in settings.NamingPatternByIndexProperties)
        {
            if (indexProperties.HasFlag(properties))
            {
                return pattern;
            }
        }

        return settings.DefaultPattern;
    }

    private static IndexData? GetIndexProperties(IScriptModel script, string defaultSchemaName, TSqlFragment fragment)
        => fragment switch
        {
            CreateIndexStatement createIndexStatement                         => GetIndexProperties(script, defaultSchemaName, createIndexStatement),
            CreateSpatialIndexStatement createSpatialIndexStatement           => GetIndexProperties(script, defaultSchemaName, createSpatialIndexStatement),
            CreateXmlIndexStatement createXmlIndexStatement                   => GetIndexProperties(script, defaultSchemaName, createXmlIndexStatement),
            CreateSelectiveXmlIndexStatement createSelectiveXmlIndexStatement => GetIndexProperties(script, defaultSchemaName, createSelectiveXmlIndexStatement),
            CreateColumnStoreIndexStatement createColumnStoreIndexStatement   => GetIndexProperties(script, defaultSchemaName, createColumnStoreIndexStatement),
            CreateFullTextIndexStatement createFullTextIndexStatement         => GetIndexProperties(script, defaultSchemaName, createFullTextIndexStatement),
            _                                                                 => null
        };

    private static IndexData GetIndexProperties(IScriptModel script, string defaultSchemaName, CreateIndexStatement statement)
    {
        var indexProperties = statement.Clustered == true ? IndexProperties.Clustered : IndexProperties.None;
        indexProperties |= statement.Clustered == false ? IndexProperties.NonClustered : IndexProperties.None;
        indexProperties |= statement.FilterPredicate is null ? IndexProperties.None : IndexProperties.Filtered;
        indexProperties |= statement.IncludeColumns.IsNullOrEmpty() ? IndexProperties.None : IndexProperties.WithIncludedColumns;
        indexProperties |= statement.Unique ? IndexProperties.Unique : IndexProperties.None;

        return new IndexData
        (
            indexProperties,
            statement.Name,
            statement.Name.Value,
            script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? Constants.UnknownObjectName,
            statement.OnName.SchemaIdentifier?.Value ?? defaultSchemaName,
            statement.OnName.BaseIdentifier?.Value ?? Constants.UnknownObjectName,
            statement.Columns.Select(a => a.Column.MultiPartIdentifier.Identifiers.Select(x => x.Value).StringJoin(".").NullIfEmptyOrWhiteSpace() ?? Constants.UnknownObjectName).ToList()
        );
    }

    private static IndexData GetIndexProperties(IScriptModel script, string defaultSchemaName, CreateColumnStoreIndexStatement statement)
    {
        var indexProperties = statement.Clustered == true ? IndexProperties.Clustered : IndexProperties.None;
        indexProperties |= statement.Clustered == false ? IndexProperties.NonClustered : IndexProperties.None;
        indexProperties |= statement.FilterPredicate is null ? IndexProperties.None : IndexProperties.Filtered;

        return new IndexData(
            IndexProperties: indexProperties,
            Identifier: statement.Name,
            Name: statement.Name.Value,
            DatabaseName: script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment: statement) ?? Constants.UnknownObjectName,
            TableSchemaName: statement.OnName.SchemaIdentifier?.Value ?? defaultSchemaName,
            TableName: statement.OnName.BaseIdentifier?.Value ?? Constants.UnknownObjectName,
            ColumnNames: statement.Columns.Select(selector: a => a.MultiPartIdentifier.ToString() ?? Constants.UnknownObjectName).ToList()
        );
    }

    private static IndexData GetIndexProperties(IScriptModel script, string defaultSchemaName, CreateSpatialIndexStatement statement)
        => new(
            IndexProperties: IndexProperties.Spatial,
            Identifier: statement.Name,
            Name: statement.Name.Value,
            DatabaseName: script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment: statement) ?? Constants.UnknownObjectName,
            TableSchemaName: statement.Object.SchemaIdentifier?.Value ?? defaultSchemaName,
            TableName: statement.Object.BaseIdentifier?.Value ?? Constants.UnknownObjectName,
            ColumnNames: [statement.SpatialColumnName.Value]
        );

    private static IndexData GetIndexProperties(IScriptModel script, string defaultSchemaName, CreateXmlIndexStatement statement)
        => new(
            IndexProperties: IndexProperties.Xml,
            Identifier: statement.Name,
            Name: statement.Name.Value,
            DatabaseName: script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment: statement) ?? Constants.UnknownObjectName,
            TableSchemaName: statement.OnName.SchemaIdentifier?.Value ?? defaultSchemaName,
            TableName: statement.OnName.BaseIdentifier?.Value ?? Constants.UnknownObjectName,
            ColumnNames: [statement.XmlColumn.Value]
        );

    private static IndexData GetIndexProperties(IScriptModel script, string defaultSchemaName, CreateSelectiveXmlIndexStatement statement)
        => new(
            IndexProperties: IndexProperties.Xml,
            Identifier: statement.Name,
            Name: statement.Name.Value,
            DatabaseName: script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment: statement) ?? Constants.UnknownObjectName,
            TableSchemaName: statement.OnName.SchemaIdentifier?.Value ?? defaultSchemaName,
            TableName: statement.OnName.BaseIdentifier?.Value ?? Constants.UnknownObjectName,
            ColumnNames: [statement.XmlColumn.Value]
        );

    private static IndexData GetIndexProperties(IScriptModel script, string defaultSchemaName, CreateFullTextIndexStatement statement)
        => new(
            IndexProperties: IndexProperties.FullText,
            Identifier: statement.KeyIndexName,
            Name: statement.KeyIndexName.Value,
            DatabaseName: script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment: statement) ?? Constants.UnknownObjectName,
            TableSchemaName: statement.OnName.SchemaIdentifier?.Value ?? defaultSchemaName,
            TableName: statement.OnName.BaseIdentifier?.Value ?? Constants.UnknownObjectName,
            ColumnNames: statement.FullTextIndexColumns.Select(a => a.Name.Value).ToList()
        );

    private sealed record IndexData(
        IndexProperties IndexProperties,
        Identifier Identifier,
        string Name,
        string DatabaseName,
        string TableSchemaName,
        string TableName,
        IReadOnlyList<string> ColumnNames
    )
    {
        public string FullObjectName => $"{DatabaseName}.{TableSchemaName}.{TableName}";
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5052",
            IssueType.Warning,
            "Index Naming",
            "The index `{0}` should be named as `{1}`. Index flags: `{2}`.",
            ["Index Name", "Expected index name", "Index Properties"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
