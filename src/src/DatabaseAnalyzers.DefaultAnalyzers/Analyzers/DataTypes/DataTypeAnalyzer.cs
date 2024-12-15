using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DataTypes;

// TODO: Remove
#pragma warning disable

public sealed class DataTypeAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5006Settings>();

        var procedureParameters = script
            .ParsedScript.GetDescendantsOfType<CreateProcedureStatement>()
            .SelectMany(a => a.Parameters);
        var functionParameters = script
            .ParsedScript.GetDescendantsOfType<CreateFunctionStatement>()
            .SelectMany(a => a.Parameters);
        var tableColumns = script
            .ParsedScript.GetDescendantsOfType<CreateTableStatement>()
            .SelectMany(a => a.Definition.ColumnDefinitions);
        var variableDeclarations = script
            .ParsedScript.GetDescendantsOfType<DeclareVariableElement>();

        AnalyzeProcedureParameters(context, script, settings.BannedProcedureParameterDataTypes, procedureParameters);
        AnalyzeFunctionParameters(context, script, settings.BannedFunctionParameterDataTypes, functionParameters);
        AnalyzeTableColumns(context, script, settings.BannedColumnDataTypes, tableColumns);
        AnalyzeVariableDeclarations(context, script, settings.BannedScriptVariableDataTypes, variableDeclarations);

        //AnalyzeProcedures(context, settings, script, createProcedureStatements);
        /*


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
        */
    }

    private static void AnalyzeProcedureParameters(IAnalysisContext context, IScriptModel script, IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<ProcedureParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            AnalyzeDataType(context, script, parameter.DataType, parameter, bannedDataTypes, "procedure parameters");
        }
    }

    private static void AnalyzeFunctionParameters(IAnalysisContext context, IScriptModel script, IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<ProcedureParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            AnalyzeDataType(context, script, parameter.DataType, parameter, bannedDataTypes, "function parameters");
        }
    }

    private static void AnalyzeTableColumns(IAnalysisContext context, IScriptModel script, IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<ColumnDefinitionBase> columns)
    {
        foreach (var column in columns)
        {
            AnalyzeDataType(context, script, column.DataType, column, bannedDataTypes, "table columns");
        }
    }

    private static void AnalyzeVariableDeclarations(IAnalysisContext context, IScriptModel script, IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<DeclareVariableElement> declarations)
    {
        foreach (var declaration in declarations)
        {
            AnalyzeDataType(context, script, declaration.DataType, declaration, bannedDataTypes, "variables");
        }
    }

    private static void AnalyzeDataType(IAnalysisContext context, IScriptModel script, DataTypeReference dataType, TSqlFragment parameter, IReadOnlyCollection<Regex> bannedTypesExpressions, string pluralObjectType)
    {
        var dataTypeName = dataType.GetSql().Replace(" ", string.Empty);
        var isBanned = bannedTypesExpressions.Any(a => a.IsMatch(dataTypeName));
        if (!isBanned)
        {
            return;
        }

        var fullObjectName = parameter.TryGetFirstClassObjectName(context.DefaultSchemaName);

        context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, parameter, dataTypeName, pluralObjectType);
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
/*
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
*/
/*


    private static void AnalyzeFunctions(IAnalysisContext context, Aj5006Settings settings, IScriptModel script, IEnumerable<SqlCreateAlterFunctionStatementBase> createFunctionStatements)
    {
        foreach (var parameter in createFunctionStatements.SelectMany(a => a.Definition.Parameters))
        {
            var dataType = parameter.GetDataType();
            AnalyzeDataType(context, script, dataType, parameter, settings.BannedFunctionParameterDataTypes, "functions");
        }
    }
*/
    /*
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
*/
}
