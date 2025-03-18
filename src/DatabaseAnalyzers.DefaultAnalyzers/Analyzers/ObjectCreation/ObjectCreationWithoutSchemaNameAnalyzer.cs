using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class ObjectCreationWithoutSchemaNameAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ObjectCreationWithoutSchemaNameAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        Analyze(
            _script.ParsedScript
                .GetChildren<CreateTableStatement>(recursive: true)
                .Where(a => !a.SchemaObjectName.BaseIdentifier.Value.IsTempTableName()),
            static s => s.SchemaObjectName.SchemaIdentifier?.Value,
            static s => s.SchemaObjectName.GetCodeRegion(),
            "table");

        Analyze(
            _script.ParsedScript.GetChildren<ViewStatementBody>(recursive: true),
            static s => s.SchemaObjectName.SchemaIdentifier?.Value,
            static s => s.SchemaObjectName.GetCodeRegion(),
            "view");

        Analyze(
            _script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true),
            static s => s.ProcedureReference.Name.SchemaIdentifier?.Value,
            static s => s.ProcedureReference.Name.GetCodeRegion(),
            "procedure");

        Analyze(
            _script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true),
            static s => s.Name.SchemaIdentifier?.Value,
            static s => s.Name.GetCodeRegion(),
            "function");

        Analyze(
            _script.ParsedScript.GetChildren<TriggerStatementBody>(recursive: true),
            static s => s.Name.SchemaIdentifier?.Value,
            static s => s.Name.GetCodeRegion(),
            "trigger");
    }

    private void Analyze<T>(IEnumerable<T> statements, Func<T, string?> schemaNameGetter, Func<T, CodeRegion> nameLocationGetter, string typeName)
        where T : TSqlStatement
    {
        foreach (var statement in statements)
        {
            Analyze(statement, schemaNameGetter, nameLocationGetter, typeName);
        }
    }

    private void Analyze<T>(T statement, Func<T, string?> schemaNameGetter, Func<T, CodeRegion> nameLocationGetter, string typeName)
        where T : TSqlStatement
    {
        var schemaName = schemaNameGetter(statement);
        if (!schemaName.IsNullOrWhiteSpace())
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, nameLocationGetter(statement), typeName, fullObjectName ?? "Unknown");
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
