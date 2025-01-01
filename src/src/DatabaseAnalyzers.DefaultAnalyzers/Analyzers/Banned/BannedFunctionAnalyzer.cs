using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

public sealed class BannedFunctionAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5040Settings>();

        var schemaObjectFunctions = script.ParsedScript.GetChildren<SchemaObjectFunctionTableReference>(recursive: true);
        var globalFunctions = script.ParsedScript.GetChildren<GlobalFunctionTableReference>(recursive: true);
        var scalarFunctions = script.ParsedScript
            .GetChildren<SelectScalarExpression>(recursive: true)
            .Where(static a => a.Expression is FunctionCall);

        AnalyzeFunctionInvocation(
            context,
            script,
            settings,
            globalFunctions,
            static x => x.Name.Value,
            static x => x.Name.GetCodeRegion());

        AnalyzeFunctionInvocation(
            context,
            script,
            settings,
            scalarFunctions,
            static x => ((FunctionCall)x.Expression).FunctionName.Value,
            static x => ((FunctionCall)x.Expression).FunctionName.GetCodeRegion());

        AnalyzeFunctionInvocation(
            context,
            script,
            settings,
            schemaObjectFunctions,
            static x => x.SchemaObject.Identifiers.Select(static a => a.Value).StringJoin('.'),
            static x => x.SchemaObject.GetCodeRegion());
    }

    private static void AnalyzeFunctionInvocation<T>(IAnalysisContext context, IScriptModel script, Aj5040Settings settings, IEnumerable<T> functions, Func<T, string> functionNameGetter, Func<T, CodeRegion> nameLocationGetter)
        where T : TSqlFragment
    {
        foreach (var function in functions)
        {
            AnalyzeFunctionInvocation(context, script, settings, function, functionNameGetter, nameLocationGetter);
        }
    }

    private static void AnalyzeFunctionInvocation<T>(IAnalysisContext context, IScriptModel script, Aj5040Settings settings, T function, Func<T, string> functionNameGetter, Func<T, CodeRegion> nameLocationGetter)
        where T : TSqlFragment
    {
        var functionName = functionNameGetter(function);
        if (functionName.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!settings.BannedFunctionNamesByReason.TryGetValue(functionName, out var reason))
        {
            return;
        }

        var databaseName = function.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
        var fullObjectName = function.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, nameLocationGetter(function), functionName, reason);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            Aj5040Settings.DiagnosticId,
            IssueType.Warning,
            "Usage of banned function",
            "The function '{0}' is banned. {1}"
        );
    }
}
