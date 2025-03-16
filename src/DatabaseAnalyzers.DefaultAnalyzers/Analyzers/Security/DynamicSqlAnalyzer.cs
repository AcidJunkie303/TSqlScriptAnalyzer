using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Security;

public sealed class DynamicSqlAnalyzer : IScriptAnalyzer
{
    private readonly IAnalysisContext _context;
    private readonly IScriptModel _script;

    public DynamicSqlAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<ExecuteStatement>(recursive: true))
        {
            Analyze(statement);
        }
    }

    private void Analyze(ExecuteStatement statement)
    {
        if (!IsDynamicSql())
        {
            return;
        }

        var fullObjectName = statement.TryGetFirstClassObjectName(_context, _script);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, statement.GetCodeRegion());

        bool IsDynamicSql()
        {
            if (statement.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference executableProcedureReference)
            {
                var procedureReference = executableProcedureReference.ProcedureReference;
                if (procedureReference is null)
                {
                    return false;
                }

                var procedureName = procedureReference.ProcedureReference.Name?.BaseIdentifier?.Value;
                if (!procedureName.EqualsOrdinalIgnoreCase("sp_executeSql"))
                {
                    return false;
                }

                var firstParameter = executableProcedureReference.Parameters.FirstOrDefault();
                return firstParameter?.ParameterValue is VariableReference;
            }

            return statement.ExecuteSpecification.ExecutableEntity switch
            {
                ExecutableProcedureReference                                                                 => false,
                ExecutableStringList executableStringList when executableStringList.Strings.IsNullOrEmpty()  => false,
                ExecutableStringList executableStringList when !executableStringList.Strings.IsNullOrEmpty() => !executableStringList.Strings.All(s => s is StringLiteral),
                _                                                                                            => false
            };
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5000",
            IssueType.Warning,
            "Dynamic SQL",
            "Executing dynamic or external provided SQL code can be dangerous and should be avoided.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
