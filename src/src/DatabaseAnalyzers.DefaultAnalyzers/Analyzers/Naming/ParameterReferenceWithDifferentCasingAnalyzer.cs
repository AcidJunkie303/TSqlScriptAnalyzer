using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class ParameterReferenceWithDifferentCasingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var functions = script.ParsedScript.GetTopLevelDescendantsOfType<FunctionStatementBody>();
        var procedures = script.ParsedScript.GetTopLevelDescendantsOfType<ProcedureStatementBody>();

        AnalyzeFunctions(context, script, functions);
        AnalyzeProcedures(context, script, procedures);
    }

    private static void AnalyzeFunctions(IAnalysisContext context, IScriptModel script, IEnumerable<FunctionStatementBody> functions)
    {
        foreach (var function in functions)
        {
            AnalyzeFunction(context, script, function);
        }
    }

    private static void AnalyzeFunction(IAnalysisContext context, IScriptModel script, FunctionStatementBody function)
    {
        if (function.StatementList is null)
        {
            return;
        }

        AnalyzeParameters(context, script, function.Parameters, function.StatementList);
    }

    private static void AnalyzeProcedures(IAnalysisContext context, IScriptModel script, IEnumerable<ProcedureStatementBody> procedures)
    {
        foreach (var procedure in procedures)
        {
            AnalyzeProcedure(context, script, procedure);
        }
    }

    private static void AnalyzeProcedure(IAnalysisContext context, IScriptModel script, ProcedureStatementBody procedure)
    {
        if (procedure.StatementList is null)
        {
            return;
        }

        AnalyzeParameters(context, script, procedure.Parameters, procedure.StatementList);
    }

    private static void AnalyzeParameters(IAnalysisContext context, IScriptModel script, IEnumerable<ProcedureParameter> parameters, StatementList bodyStatementsList)
    {
        foreach (var parameter in parameters)
        {
            AnalyzeParameter(context, script, parameter, bodyStatementsList);
        }
    }

    private static void AnalyzeParameter(IAnalysisContext context, IScriptModel script, ProcedureParameter parameter, StatementList bodyStatementsList)
    {
        Lazy<string?> lazyFullObjectName = new(() => parameter.TryGetFirstClassObjectName(context, script));

        var variableReferencesWithDifferentCasing = bodyStatementsList
            .GetChildren<VariableReference>(recursive: true)
            .Where(a => a.Name.IsEqualToButWithDifferentCasing(parameter.VariableName.Value));

        foreach (var reference in variableReferencesWithDifferentCasing)
        {
            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(reference) ?? DatabaseNames.Unknown;
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, lazyFullObjectName.Value, reference.GetCodeRegion(), reference.Name, parameter.VariableName.Value);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5013",
            IssueType.Warning,
            "Parameter reference with different casing",
            "The parameter reference `{0}` has different casing compared to the declaration `{1}`.",
            ["Parameter name", "Declared parameter name"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
