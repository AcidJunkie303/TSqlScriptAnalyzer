using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutSchemaNameAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        Analyze(
            context,
            script,
            script.ParsedScript
                .GetChildren<CreateTableStatement>(recursive: true)
                .Where(a => !a.SchemaObjectName.BaseIdentifier.Value.IsTempTableName()),
            static s => s.SchemaObjectName.SchemaIdentifier?.Value,
            static s => s.SchemaObjectName.GetCodeRegion(),
            "table");

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ViewStatementBody>(recursive: true),
            static s => s.SchemaObjectName.SchemaIdentifier?.Value,
            static s => s.SchemaObjectName.GetCodeRegion(),
            "view");

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true),
            static s => s.ProcedureReference.Name.SchemaIdentifier?.Value,
            static s => s.ProcedureReference.Name.GetCodeRegion(),
            "procedure");

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true),
            static s => s.Name.SchemaIdentifier?.Value,
            static s => s.Name.GetCodeRegion(),
            "function");

        Analyze(
            context,
            script,
            script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true),
            static s => s.Name.SchemaIdentifier?.Value,
            static s => s.Name.GetCodeRegion(),
            "trigger");
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, IEnumerable<T> statements, Func<T, string?> schemaNameGetter, Func<T, CodeRegion> nameLocationGetter, string typeName)
        where T : TSqlStatement
    {
        foreach (var statement in statements)
        {
            Analyze(context, script, statement, schemaNameGetter, nameLocationGetter, typeName);
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, T statement, Func<T, string?> schemaNameGetter, Func<T, CodeRegion> nameLocationGetter, string typeName)
        where T : TSqlStatement
    {
        var schemaName = schemaNameGetter(statement);
        if (!schemaName.IsNullOrWhiteSpace())
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, nameLocationGetter(statement), typeName, fullObjectName ?? "Unknown");
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5037",
            IssueType.Warning,
            "Object creation without schema name",
            "The creation statement of the {0} `{1}` doesn't use a schema name.",
            ["Object type name", "Object name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
