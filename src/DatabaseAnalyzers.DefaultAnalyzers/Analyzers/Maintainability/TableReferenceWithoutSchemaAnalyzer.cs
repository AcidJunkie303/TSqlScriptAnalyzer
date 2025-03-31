using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class TableReferenceWithoutSchemaAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5066Settings _settings;
    private readonly ITableResolverFactory _tableResolverFactory;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public TableReferenceWithoutSchemaAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5066Settings settings, ITableResolverFactory tableResolverFactory)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
        _tableResolverFactory = tableResolverFactory;
    }

    public void AnalyzeScript()
    {
        foreach (var tableReference in _script.ParsedScript.GetChildren<NamedTableReference>(recursive: true))
        {
            AnalyzeTableReference(tableReference);
        }
    }

    [SuppressMessage("Major Code Smell", "S4017:Method signatures should not contain nested generic types")]
    private void AnalyzeTableReference(NamedTableReference tableReference)
    {
        var tableName = tableReference.SchemaObject?.BaseIdentifier?.Value;
        if (tableName.IsNullOrWhiteSpace())
        {
            return;
        }

        var schemaName = tableReference.SchemaObject?.SchemaIdentifier?.Value;
        if (!schemaName.IsNullOrWhiteSpace())
        {
            return;
        }

        if (tableName.IsTempTableName())
        {
            return;
        }

        var tableResolver = _tableResolverFactory.CreateTableResolver(_context);
        var table = tableResolver.Resolve(tableReference);
        if (table.IsNullOrMissingAliasReference() || table.SourceType == TableSourceType.Cte)
        {
            return;
        }

        var isMostLikelyTableAlias = !table.ObjectName.EqualsOrdinalIgnoreCase(tableName);
        if (isMostLikelyTableAlias)
        {
            return;
        }

        if (IsTableNameIgnored(tableName))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(tableReference) ?? DatabaseNames.Unknown;
        var fullObjectName = tableReference.TryGetFirstClassObjectName(_context, _script);
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, tableReference.GetCodeRegion(), tableName);
    }

    private bool IsTableNameIgnored(string objectName)
        => _settings
            .IgnoredTableNames
            .Any(a => a.IsMatch(objectName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5066",
            IssueType.Warning,
            "Table reference without schema name",
            "The table reference `{0}` doesn't use a schema name",
            ["Schema name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
