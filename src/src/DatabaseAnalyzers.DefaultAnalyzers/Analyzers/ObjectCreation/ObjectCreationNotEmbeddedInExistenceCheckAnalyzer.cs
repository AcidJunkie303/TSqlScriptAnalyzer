using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class ObjectCreationNotEmbeddedInExistenceCheckAnalyzer : IScriptAnalyzer
{
    // IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[V1]'))
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5025Settings>();
        foreach (var statement in script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeTableCreation(context, script, settings, statement);
        }
    }

    private static void AnalyzeTableCreation(IAnalysisContext context, IScriptModel script, Aj5025Settings settings, CreateTableStatement statement)
    {
        if (settings.ExistenceCheckPatternForTableCreation.IsNullOrWhiteSpace())
        {
            return;
        }

        var (tableSchemaName, tableName) = GetTableNames(statement, context.DefaultSchemaName);
        if (tableName.IsTempTableName())
        {
            return;
        }

        var expectedExistenceCheckCode = CreateTableExistenceCheckCode(settings.ExistenceCheckPatternForTableCreation, tableSchemaName, tableName);
        var parentStatement = statement.GetParent(script.ParentFragmentProvider);
        var parentStatementCode = GetParentStatementCode();

        if (parentStatementCode.EqualsOrdinal(expectedExistenceCheckCode))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion(), expectedExistenceCheckCode);

        string? GetParentStatementCode() =>
            parentStatement is null
                ? null
                : script.ParsedScript.ScriptTokenStream
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
