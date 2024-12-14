using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

// TODO: Types to handle
//  - Trigger
//  - CLR Trigger
public sealed partial class ObjectCreationWithoutOrAlterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var createViewStatements = GetDescendantsOfType<SqlCreateViewStatement>(script.ParsedScript);
        var createProcedureStatements = GetDescendantsOfType<SqlCreateProcedureStatement>(script.ParsedScript);
        var createFunctionStatements = GetDescendantsOfType<SqlCreateAlterFunctionStatementBase>(script.ParsedScript);
        var createClrProcedureStatements = script.ParsedScript
            .GetDescendantsOfType<SqlNullStatement>()
            .Select(a => a.TryParseCreateClrStoredProcedureStatement(context.DefaultSchemaName))
            .WhereNotNull();

        Analyze(context, script, createViewStatements);
        Analyze(context, script, createProcedureStatements);
        Analyze(context, script, createFunctionStatements);
        Analyze(context, script, createClrProcedureStatements);
    }

    private static IEnumerable<SqlCodeObject> GetDescendantsOfType<T>(SqlScript script)
        where T : SqlCodeObject
        => script
            .GetDescendantsOfType<T>();

    private static void Analyze(IAnalysisContext context, IScriptModel script, IEnumerable<SqlCodeObject> creationStatements)
    {
        foreach (var statement in creationStatements)
        {
            var isCreateOrAlter = IsCreateOrAlterFinder().IsMatch(statement.Sql);

            if (isCreateOrAlter)
            {
                continue;
            }

            var fullObjectName = statement.TryGetFullObjectName(context.DefaultSchemaName);
            Report(context.IssueReporter, script, fullObjectName, CodeRegion.From(statement));
        }
    }

    private static void Analyze(IAnalysisContext context, IScriptModel script, IEnumerable<SqlCreateClrStoredProcedureStatement> creationStatements)
    {
        foreach (var statement in creationStatements)
        {
            if (statement.IsCreateOrAlter)
            {
                continue;
            }

            var fullObjectName = statement.CreationStatement.TryGetFullObjectName(context.DefaultSchemaName);
            Report(context.IssueReporter, script, fullObjectName, statement.CodeRegion);
        }
    }

    private static void Report(IIssueReporter issueReporter, IScriptModel script, string? fullObjectName, CodeRegion codeRegion)
        => issueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, codeRegion);

    [GeneratedRegex(@"\ACREATE\s+OR\s+ALTER\s", RegexOptions.None, 100)]
    private static partial Regex IsCreateOrAlterFinder();

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5009",
            IssueType.Formatting,
            "Object creation without 'OR ALTER' clause",
            "Object creation without 'OR ALTER' clause."
        );
    }
}
