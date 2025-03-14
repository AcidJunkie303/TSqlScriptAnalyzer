using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class ProcedureInvocationWithoutExplicitParametersAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5059Settings>();

        foreach (var procedureCall in script.ParsedScript.GetChildren<ExecutableProcedureReference>(recursive: true))
        {
            AnalyzeProcedureCall(context, script, settings, procedureCall);
        }
    }

    private static void AnalyzeProcedureCall(IAnalysisContext context, IScriptModel script, Aj5059Settings settings, ExecutableProcedureReference procedureCall)
    {
        if (procedureCall.Parameters.Count == 0)
        {
            return;
        }

        var procedureName = procedureCall.ProcedureReference?.ProcedureReference?.Name;
        if (procedureName is null)
        {
            return;
        }

        var schemaName = procedureName.SchemaIdentifier?.Value ?? context.DefaultSchemaName;
        var pureProcedureName = procedureName.BaseIdentifier.Value;
        if (pureProcedureName.IsNullOrWhiteSpace())
        {
            return;
        }

        var twoPartProcedureName = $"{schemaName}.{pureProcedureName}";
        if (IsObjectNameIgnored(settings, twoPartProcedureName))
        {
            return;
        }

        if (procedureCall.Parameters.All(a => a.Variable is not null))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(procedureCall) ?? DatabaseNames.Unknown;
        var fullObjectName = procedureCall.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, procedureCall.GetCodeRegion(), twoPartProcedureName);
    }

    private static bool IsObjectNameIgnored(Aj5059Settings settings, string objectName)
        => settings
            .IgnoredProcedureNamePatterns
            .Any(a => a.IsMatch(objectName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5059",
            IssueType.Warning,
            "Procedure Call without explicit Parameter Names",
            "The procedure invocation of `{0}` does not specify explicit parameter names for all arguments.",
            ["Invoked procedure name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
