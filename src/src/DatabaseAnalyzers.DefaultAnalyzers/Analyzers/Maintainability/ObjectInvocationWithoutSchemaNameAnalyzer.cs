using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class ObjectInvocationWithoutSchemaNameAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5049Settings>();

        var procedureCalls = script.ParsedScript.GetChildren<ExecutableProcedureReference>(recursive: true);
        var scalarFunctionCalls = script.ParsedScript.GetChildren<FunctionCall>(recursive: true);
        var tableValuedFunctionCalls = script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true);

        foreach (var procedureCall in procedureCalls)
        {
            AnalyzeProcedureCall(context, script, settings, procedureCall);
        }

        foreach (var functionCall in scalarFunctionCalls)
        {
            AnalyzeScalarFunctionCall(context, script, settings, functionCall);
        }

        foreach (var tableValuedFunctionCall in tableValuedFunctionCalls)
        {
            AnalyzeTableValuedFunctionCall(context, script, settings, tableValuedFunctionCall);
        }
    }

    private static void AnalyzeProcedureCall(IAnalysisContext context, IScriptModel script, Aj5049Settings settings, ExecutableProcedureReference procedureCall)
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

        if (IsObjectNameIgnored(settings, pureProcedureName))
        {
            return;
        }

        Report(context, script, procedureReference, "procedure", pureProcedureName);
    }

    private static void AnalyzeScalarFunctionCall(IAnalysisContext context, IScriptModel script, Aj5049Settings settings, FunctionCall functionCall)
    {
        if (functionCall.CallTarget is not null)
        {
            return;
        }

        if (IsObjectNameIgnored(settings, functionCall.FunctionName.Value))
        {
            return;
        }

        Report(context, script, functionCall, "scalar function", functionCall.FunctionName.Value);
    }

    private static void AnalyzeTableValuedFunctionCall(IAnalysisContext context, IScriptModel script, Aj5049Settings settings, SchemaObjectFunctionTableReference functionReference)
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

        if (IsObjectNameIgnored(settings, pureFunctionName))
        {
            return;
        }

        Report(context, script, functionReference, "table valued function", pureFunctionName);
    }

    private static bool IsObjectNameIgnored(Aj5049Settings settings, string objectName)
        => settings
            .IgnoredObjectNamePatterns
            .Any(a => a.IsMatch(objectName));

    private static void Report(IAnalysisContext context, IScriptModel script, TSqlFragment invocation, string objectTypeName, string objectName)
    {
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(invocation) ?? DatabaseNames.Unknown;
        var fullObjectName = invocation.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, invocation.GetCodeRegion(), objectTypeName, objectName);
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
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
