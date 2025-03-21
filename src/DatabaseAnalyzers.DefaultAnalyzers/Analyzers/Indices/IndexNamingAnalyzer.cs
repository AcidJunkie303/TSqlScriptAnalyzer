using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using DatabaseAnalyzers.DefaultAnalyzers.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class IndexNamingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5052Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public IndexNamingAnalyzer(IScriptAnalysisContext context, Aj5052Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        var createIndexStatements = _script.ParsedScript
            .GetChildren(recursive: true)
            .Where(IsCreateIndexStatement);

        foreach (var statement in createIndexStatements)
        {
            AnalyzeCreateIndexStatement(statement);
        }

        foreach (var statement in _script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeCreateTableStatement(statement);
        }

        foreach (var statement in _script.ParsedScript.GetChildren<AlterTableAddTableElementStatement>(recursive: true))
        {
            AnalyzeAlterTableStatement(statement);
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

    private void AnalyzeCreateTableStatement(CreateTableStatement statement)
    {
        if (statement.IsTempTable())
        {
            return;
        }

        var primaryKeyDefinition = statement.Definition.TableConstraints
            .OfType<UniqueConstraintDefinition>()
            .FirstOrDefault(a => a.IsPrimaryKey);

        if (primaryKeyDefinition is null)
        {
            return;
        }

        var tableSchemaName = statement.SchemaObjectName?.SchemaIdentifier?.Value ?? _context.DefaultSchemaName;
        var tableName = statement.SchemaObjectName?.BaseIdentifier.Value ?? Constants.UnknownObjectName;
        AnalyzeUniqueConstraintDefinition(primaryKeyDefinition, tableSchemaName, tableName);
    }

    private void AnalyzeAlterTableStatement(AlterTableAddTableElementStatement statement)
    {
        var primaryKeyDefinition = statement.Definition.TableConstraints
            .OfType<UniqueConstraintDefinition>()
            .FirstOrDefault(a => a.IsPrimaryKey);

        if (primaryKeyDefinition is null)
        {
            return;
        }

        var tableSchemaName = statement.SchemaObjectName?.SchemaIdentifier?.Value ?? _context.DefaultSchemaName;
        var tableName = statement.SchemaObjectName?.BaseIdentifier.Value ?? Constants.UnknownObjectName;

        AnalyzeUniqueConstraintDefinition(primaryKeyDefinition, tableSchemaName, tableName);
    }

    private void AnalyzeUniqueConstraintDefinition(UniqueConstraintDefinition primaryKeyDefinition, string tableSchemaName, string tableName)
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

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(primaryKeyDefinition) ?? Constants.UnknownObjectName;
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

        var expectedIndexName = CreateIndexName(indexData);
        if (indexData.Name.Equals(expectedIndexName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _context.IssueReporter.Report(DiagnosticDefinitions.Default, indexData.DatabaseName, _script.RelativeScriptFilePath, indexData.FullObjectName, indexData.Identifier.GetCodeRegion(),
            indexData.Name, expectedIndexName, indexData.IndexProperties);
    }

    private void AnalyzeCreateIndexStatement(TSqlFragment fragment)
    {
        if (fragment is CreateIndexStatement createIndexStatement)
        {
            var tableName = createIndexStatement.OnName?.BaseIdentifier?.Value;

            if (tableName?.IsTempTableName() == true)
            {
                return;
            }
        }

        var indexData = GetIndexProperties(_context.DefaultSchemaName, fragment);
        if (indexData is null)
        {
            return;
        }

        var expectedIndexName = CreateIndexName(indexData);
        if (indexData.Name.Equals(expectedIndexName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _context.IssueReporter.Report(DiagnosticDefinitions.Default, indexData.DatabaseName, _script.RelativeScriptFilePath, indexData.FullObjectName, indexData.Identifier.GetCodeRegion(),
            indexData.Name, expectedIndexName, indexData.IndexProperties);
    }

    private string CreateIndexName(IndexData indexData)
    {
        var pattern = GetPatternForIndexProperties(indexData.IndexProperties);
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

    private string GetPatternForIndexProperties(IndexProperties indexProperties)
    {
        foreach (var (properties, pattern) in _settings.NamingPatterns)
        {
            if (indexProperties.HasFlag(properties))
            {
                return pattern;
            }
        }

        return _settings.DefaultPattern;
    }

    private IndexData? GetIndexProperties(string defaultSchemaName, TSqlFragment fragment)
        => fragment switch
        {
            CreateIndexStatement createIndexStatement                         => GetIndexProperties(_script, defaultSchemaName, createIndexStatement),
            CreateSpatialIndexStatement createSpatialIndexStatement           => GetIndexProperties(_script, defaultSchemaName, createSpatialIndexStatement),
            CreateXmlIndexStatement createXmlIndexStatement                   => GetIndexProperties(_script, defaultSchemaName, createXmlIndexStatement),
            CreateSelectiveXmlIndexStatement createSelectiveXmlIndexStatement => GetIndexProperties(_script, defaultSchemaName, createSelectiveXmlIndexStatement),
            CreateColumnStoreIndexStatement createColumnStoreIndexStatement   => GetIndexProperties(_script, defaultSchemaName, createColumnStoreIndexStatement),
            CreateFullTextIndexStatement createFullTextIndexStatement         => GetIndexProperties(_script, defaultSchemaName, createFullTextIndexStatement),
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
            "The index `{0}` should be named as `{1}`. Index properties are `{2}`.",
            ["Index Name", "Expected index name", "Index Properties"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
