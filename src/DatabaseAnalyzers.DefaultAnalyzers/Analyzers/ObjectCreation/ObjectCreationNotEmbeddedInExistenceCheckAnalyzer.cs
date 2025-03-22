using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class ObjectCreationNotEmbeddedInExistenceCheckAnalyzer : IScriptAnalyzer
{
    // IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[V1]'))
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5025Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ObjectCreationNotEmbeddedInExistenceCheckAnalyzer(IScriptAnalysisContext context, Aj5025Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeTableCreation(statement);
        }
    }

    private void AnalyzeTableCreation(CreateTableStatement statement)
    {
        if (_settings.ExistenceCheckPatternForTableCreation.IsNullOrWhiteSpace())
        {
            return;
        }

        var (tableSchemaName, tableName) = GetTableNames(statement, _context.DefaultSchemaName);
        if (tableName.IsTempTableName())
        {
            return;
        }

        var expectedExistenceCheckCode = CreateTableExistenceCheckCode(_settings.ExistenceCheckPatternForTableCreation, tableSchemaName, tableName);
        var parentStatement = statement.GetParent(_script.ParentFragmentProvider);
        var parentStatementCode = GetParentStatementCode();

        if (parentStatementCode.EqualsOrdinal(expectedExistenceCheckCode))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion(), expectedExistenceCheckCode);

        string? GetParentStatementCode() =>
            parentStatement is null
                ? null
                : _script.ParsedScript.ScriptTokenStream
                    .Skip(parentStatement!.FirstTokenIndex)
                    .Take(statement.FirstTokenIndex - parentStatement.FirstTokenIndex)
                    .Select(a => a.Text)
                    .StringJoin(string.Empty)
                    .Trim();
    }

    private static string CreateTableExistenceCheckCode(string pattern, string tableSchemaName, string tableName)
        => pattern
            .Replace(Placeholders.TableSchemaName, tableSchemaName, StringComparison.OrdinalIgnoreCase)
            .Replace(Placeholders.TableName, tableName, StringComparison.OrdinalIgnoreCase);

    private static (string TableSchemaName, string TableName) GetTableNames(CreateTableStatement statement, string defaultSchemaName)
        => (statement.SchemaObjectName.GetSchemaName(defaultSchemaName), statement.SchemaObjectName.BaseIdentifier.Value);

    private static class Placeholders
    {
        public const string TableSchemaName = "{TableSchemaName}";
        public const string TableName = "{TableName}";
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5025",
            IssueType.Warning,
            "Object Creation not embedded in negative existence check",
            "The object creation statement is not embedded in the following negative existence check `{0}`.",
            ["Expected Negative Existence Check Statement"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
