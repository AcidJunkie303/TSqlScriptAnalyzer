using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class ObjectInvocationWithoutSchemaNameAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5049Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ObjectInvocationWithoutSchemaNameAnalyzer(IScriptAnalysisContext context, Aj5049Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        var procedureCalls = _script.ParsedScript.GetChildren<ExecutableProcedureReference>(recursive: true);
        var tableValuedFunctionCalls = _script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true);

        foreach (var procedureCall in procedureCalls)
        {
            AnalyzeProcedureCall(procedureCall);
        }

        foreach (var tableValuedFunctionCall in tableValuedFunctionCalls)
        {
            AnalyzeTableValuedFunctionCall(tableValuedFunctionCall);
        }
    }

    private void AnalyzeProcedureCall(ExecutableProcedureReference procedureCall)
    {
        var procedureReference = procedureCall.ProcedureReference?.ProcedureReference;
        if (procedureReference is null)
        {
            return;
        }

        var procedureName = procedureReference.Name;
        if (procedureName is null)
        {
            return;
        }

        var schema = procedureName.SchemaIdentifier?.Value;
        if (!schema.IsNullOrWhiteSpace())
        {
            return;
        }

        var pureProcedureName = procedureName.BaseIdentifier.Value;
        if (pureProcedureName.IsNullOrWhiteSpace())
        {
            return;
        }

        if (IsObjectNameIgnored(pureProcedureName))
        {
            return;
        }

        Report(procedureReference, "procedure", pureProcedureName);
    }

    private void AnalyzeTableValuedFunctionCall(SchemaObjectFunctionTableReference functionReference)
    {
        var pureFunctionName = functionReference.SchemaObject.BaseIdentifier?.Value;
        if (pureFunctionName.IsNullOrWhiteSpace())
        {
            return;
        }

        var schemaName = functionReference.SchemaObject.SchemaIdentifier?.Value;
        if (!schemaName.IsNullOrWhiteSpace())
        {
            return;
        }

        if (IsObjectNameIgnored(pureFunctionName))
        {
            return;
        }

        Report(functionReference, "table valued function", pureFunctionName);
    }

    private bool IsObjectNameIgnored(string objectName)
        => _settings
            .IgnoredObjectNamePatterns
            .Any(a => a.IsMatch(objectName));

    private void Report(TSqlFragment invocation, string objectTypeName, string objectName)
    {
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(invocation) ?? DatabaseNames.Unknown;
        var fullObjectName = invocation.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, invocation.GetCodeRegion(), objectTypeName, objectName);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5049",
            IssueType.Warning,
            "Object Invocation without explicitly specified schema name",
            "The invocation of `{0}` `{1}` is missing the schema name ",
            ["Object type name", "Invoked object name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
