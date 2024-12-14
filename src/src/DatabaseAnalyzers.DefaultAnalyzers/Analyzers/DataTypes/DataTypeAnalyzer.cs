using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DataTypes;

public sealed class DataTypeAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5006Settings>();

        var variableDeclarations = script.ParsedScript.GetDescendantsOfType<SqlVariableDeclaration>();
        var createTableStatements = script.ParsedScript.GetDescendantsOfType<SqlCreateTableStatement>();
        var createProcedureStatements = script.ParsedScript.GetDescendantsOfType<SqlCreateProcedureStatement>();
        var createFunctionStatements = script.ParsedScript.GetDescendantsOfType<SqlCreateAlterFunctionStatementBase>();
        var createClrProcedureStatements = script.ParsedScript
            .GetDescendantsOfType<SqlNullStatement>()
            .Select(a => a.TryParseCreateClrStoredProcedureStatement(context.DefaultSchemaName))
            .WhereNotNull();

        AnalyzeVariableDeclarations(context, settings, script, variableDeclarations);
        AnalyzeTables(context, settings, script, createTableStatements);
        AnalyzeProcedures(context, settings, script, createProcedureStatements);
        AnalyzeFunctions(context, settings, script, createFunctionStatements);
        AnalyzeClrProcedures(context, settings, script, createClrProcedureStatements);
    }

    private static void AnalyzeVariableDeclarations(IAnalysisContext context, Aj5006Settings settings, IScriptModel script, IEnumerable<SqlVariableDeclaration> variableDeclarations)
    {
        foreach (var variableDeclaration in variableDeclarations)
        {
            var dataType = variableDeclaration.Type.GetDataType();
            AnalyzeDataType(context, script, dataType, variableDeclaration, settings.BannedScriptVariableDataTypes, "variables");
        }
    }

    private static void AnalyzeClrProcedures(IAnalysisContext context, Aj5006Settings settings, IScriptModel script, IEnumerable<SqlCreateClrStoredProcedureStatement> createClrProcedureStatements)
    {
        foreach (var createClrProcedureStatement in createClrProcedureStatements)
        {
            foreach (var parameter in createClrProcedureStatement.Parameters)
            {
                AnalyzeDataType(context, script, parameter.DataType, createClrProcedureStatement.CreationStatement, settings.BannedProcedureParameterDataTypes, "procedures");
            }
        }
    }

    private static void AnalyzeTables(IAnalysisContext context, Aj5006Settings settings, IScriptModel script, IEnumerable<SqlCreateTableStatement> createTableStatements)
    {
        foreach (var columnDefinition in createTableStatements.SelectMany(a => a.Definition.ColumnDefinitions))
        {
            var dataType = columnDefinition.DataType.GetDataType();
            AnalyzeDataType(context, script, dataType, columnDefinition, settings.BannedColumnDataTypes, "tables");
        }
    }

    private static void AnalyzeProcedures(IAnalysisContext context, Aj5006Settings settings, IScriptModel script, IEnumerable<SqlCreateProcedureStatement> createProcedureStatements)
    {
        foreach (var parameter in createProcedureStatements.SelectMany(a => a.Definition.Parameters))
        {
            var dataType = parameter.GetDataType();
            AnalyzeDataType(context, script, dataType, parameter, settings.BannedProcedureParameterDataTypes, "procedure parameters");
        }
    }

    private static void AnalyzeFunctions(IAnalysisContext context, Aj5006Settings settings, IScriptModel script, IEnumerable<SqlCreateAlterFunctionStatementBase> createFunctionStatements)
    {
        foreach (var parameter in createFunctionStatements.SelectMany(a => a.Definition.Parameters))
        {
            var dataType = parameter.GetDataType();
            AnalyzeDataType(context, script, dataType, parameter, settings.BannedFunctionParameterDataTypes, "functions");
        }
    }

    private static void AnalyzeDataType(IAnalysisContext context, IScriptModel script, IDataType dataType, SqlCodeObject codeObject, IReadOnlyCollection<Regex> bannedTypesExpressions, string pluralObjectType)
    {
        var isBanned = bannedTypesExpressions.Any(a => a.IsMatch(dataType.Name) || a.IsMatch(dataType.FullName));
        if (!isBanned)
        {
            return;
        }

        var fullObjectName = codeObject.TryGetFullObjectName(context.DefaultSchemaName);

        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, codeObject, dataType.FullName, pluralObjectType);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5006",
            IssueType.Warning,
            "Usage of banned data type",
            "The data type '{0}' is banned for {1}"
        );
    }
}
