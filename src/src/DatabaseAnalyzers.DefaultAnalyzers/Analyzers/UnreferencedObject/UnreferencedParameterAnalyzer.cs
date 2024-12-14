using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedParameterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        var functions = script.ParsedScript.GetTopLevelDescendantsOfType<SqlCreateAlterFunctionStatementBase>();
        var procedures = script.ParsedScript.GetTopLevelDescendantsOfType<SqlCreateAlterProcedureStatementBase>();

        AnalyzeFunctions(context, script.RelativeScriptFilePath, functions);
        AnalyzeProcedures(context, script.RelativeScriptFilePath, procedures);
    }

    private static void AnalyzeFunctions(IAnalysisContext context, string relativeScriptFilePath, IEnumerable<SqlCreateAlterFunctionStatementBase> functions)
    {
        foreach (var function in functions)
        {
            AnalyzeFunction(context, relativeScriptFilePath, function);
        }
    }

    private static void AnalyzeFunction(IAnalysisContext context, string relativeScriptFilePath, SqlCreateAlterFunctionStatementBase function)
    {
        var body = function.TryGetBody();
        if (body is null)
        {
            return;
        }

        AnalyzeParameters(context, relativeScriptFilePath, function.Definition.Parameters, body.Tokens);
    }

    private static void AnalyzeProcedures(IAnalysisContext context, string relativeScriptFilePath, IEnumerable<SqlCreateAlterProcedureStatementBase> procedures)
    {
        foreach (var procedure in procedures)
        {
            AnalyzeProcedure(context, relativeScriptFilePath, procedure);
        }
    }

    private static void AnalyzeProcedure(IAnalysisContext context, string relativeScriptFilePath, SqlCreateAlterProcedureStatementBase procedure)
    {
        var body = procedure.TryGetBody();
        if (body is null)
        {
            return;
        }

        AnalyzeParameters(context, relativeScriptFilePath, procedure.Definition.Parameters, body.Tokens);
    }

    private static void AnalyzeParameters(IAnalysisContext context, string relativeScriptFilePath, IEnumerable<SqlParameterDeclaration> parameters, IEnumerable<Token> bodyTokens)
    {
        var tokens = bodyTokens.ToList();

        foreach (var parameter in parameters)
        {
            AnalyzeParameter(context, relativeScriptFilePath, parameter, tokens);
        }
    }

    private static void AnalyzeParameter(IAnalysisContext context, string relativeScriptFilePath, SqlParameterDeclaration parameter, IReadOnlyCollection<Token>? bodyTokens)
    {
        if (bodyTokens.IsNullOrEmpty())
        {
            return;
        }

        if (IsParameterReferenced(parameter.Name, bodyTokens))
        {
            return;
        }

        var fullObjectName = parameter.TryGetFullObjectName(context.DefaultSchemaName);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, relativeScriptFilePath, fullObjectName, parameter, parameter.Name);
    }

    private static bool IsParameterReferenced(string parameterName, IReadOnlyCollection<Token> tokens)
        => tokens.Any(token => token.Text.EqualsOrdinalIgnoreCase(parameterName) && token.IsVariable());

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5011",
            IssueType.Warning,
            "Unreferenced parameter",
            "The parameter '{0}' is not referenced"
        );
    }
}
