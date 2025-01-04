using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

public sealed class BannedDataTypeAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5006Settings>();
        var parsedScript = script.ParsedScript;

        var procedureParameters = parsedScript
            .GetChildren<CreateProcedureStatement>(recursive: true)
            .SelectMany(static a => a.Parameters);
        var functionParameters = parsedScript
            .GetChildren<CreateFunctionStatement>(recursive: true)
            .SelectMany(static a => a.Parameters);
        var tableColumns = parsedScript
            .GetChildren<CreateTableStatement>(recursive: true)
            .SelectMany(static a => a.Definition.ColumnDefinitions);
        var variableDeclarations = parsedScript
            .GetChildren<DeclareVariableElement>(recursive: true);

        AnalyzeProcedureParameters(context, script, settings.BannedProcedureParameterDataTypes, procedureParameters);
        AnalyzeFunctionParameters(context, script, settings.BannedFunctionParameterDataTypes, functionParameters);
        AnalyzeTableColumns(context, script, settings.BannedColumnDataTypes, tableColumns);
        AnalyzeVariableDeclarations(context, script, settings.BannedScriptVariableDataTypes, variableDeclarations);
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
        var dataTypeName = dataType.GetSql().Replace(" ", string.Empty, StringComparison.Ordinal);
        var isBanned = bannedTypesExpressions.Any(a => a.IsMatch(dataTypeName));
        if (!isBanned)
        {
            return;
        }

        var fullObjectName = parameter.TryGetFirstClassObjectName(context.DefaultSchemaName, script.ParsedScript, script.ParentFragmentProvider);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(parameter) ?? DatabaseNames.Unknown;
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, parameter.GetCodeRegion(), dataTypeName, pluralObjectType);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5006",
            IssueType.Warning,
            "Usage of banned data type",
            "The data type '{0}' is banned for {1}",
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
