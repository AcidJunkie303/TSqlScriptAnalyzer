using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class ParameterReferenceWithDifferentCasingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public ParameterReferenceWithDifferentCasingAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        var functions = _script.ParsedScript.GetTopLevelDescendantsOfType<FunctionStatementBody>(_script.ParentFragmentProvider);
        var procedures = _script.ParsedScript.GetTopLevelDescendantsOfType<ProcedureStatementBody>(_script.ParentFragmentProvider);

        AnalyzeFunctions(functions);
        AnalyzeProcedures(procedures);
    }

    private void AnalyzeFunctions(IEnumerable<FunctionStatementBody> functions)
    {
        foreach (var function in functions)
        {
            AnalyzeFunction(function);
        }
    }

    private void AnalyzeFunction(FunctionStatementBody function)
    {
        if (function.StatementList is null)
        {
            return;
        }

        AnalyzeParameters(function.Parameters, function.StatementList);
    }

    private void AnalyzeProcedures(IEnumerable<ProcedureStatementBody> procedures)
    {
        foreach (var procedure in procedures)
        {
            AnalyzeProcedure(procedure);
        }
    }

    private void AnalyzeProcedure(ProcedureStatementBody procedure)
    {
        if (procedure.StatementList is null)
        {
            return;
        }

        AnalyzeParameters(procedure.Parameters, procedure.StatementList);
    }

    private void AnalyzeParameters(IEnumerable<ProcedureParameter> parameters, StatementList bodyStatementsList)
    {
        foreach (var parameter in parameters)
        {
            AnalyzeParameter(parameter, bodyStatementsList);
        }
    }

    private void AnalyzeParameter(ProcedureParameter parameter, StatementList bodyStatementsList)
    {
        Lazy<string?> lazyFullObjectName = new(() => parameter.TryGetFirstClassObjectName(_context, _script));

        var variableReferencesWithDifferentCasing = bodyStatementsList
            .GetChildren<VariableReference>(recursive: true)
            .Where(a => a.Name.IsEqualToButWithDifferentCasing(parameter.VariableName.Value));

        foreach (var reference in variableReferencesWithDifferentCasing)
        {
            var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(reference) ?? DatabaseNames.Unknown;
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, lazyFullObjectName.Value, reference.GetCodeRegion(), reference.Name, parameter.VariableName.Value);
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
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
