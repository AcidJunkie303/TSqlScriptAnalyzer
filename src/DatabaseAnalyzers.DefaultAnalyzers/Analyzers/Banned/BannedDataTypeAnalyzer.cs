using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Banned;

public sealed class BannedDataTypeAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5006Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public BannedDataTypeAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5006Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _settings = settings;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        var parsedScript = _context.Script.ParsedScript;

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

        AnalyzeProcedureParameters(_settings.BannedProcedureParameterDataTypes, procedureParameters);
        AnalyzeFunctionParameters(_settings.BannedFunctionParameterDataTypes, functionParameters);
        AnalyzeTableColumns(_settings.BannedColumnDataTypes, tableColumns);
        AnalyzeVariableDeclarations(_settings.BannedScriptVariableDataTypes, variableDeclarations);
    }

    private void AnalyzeProcedureParameters(IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<ProcedureParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            AnalyzeDataType(parameter.DataType, parameter, bannedDataTypes, "procedure parameters");
        }
    }

    private void AnalyzeFunctionParameters(IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<ProcedureParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            AnalyzeDataType(parameter.DataType, parameter, bannedDataTypes, "function parameters");
        }
    }

    private void AnalyzeTableColumns(IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<ColumnDefinitionBase> columns)
    {
        foreach (var column in columns)
        {
            AnalyzeDataType(column.DataType, column, bannedDataTypes, "table columns");
        }
    }

    private void AnalyzeVariableDeclarations(IReadOnlyCollection<Regex> bannedDataTypes, IEnumerable<DeclareVariableElement> declarations)
    {
        foreach (var declaration in declarations)
        {
            AnalyzeDataType(declaration.DataType, declaration, bannedDataTypes, "variables");
        }
    }

    private void AnalyzeDataType(DataTypeReference? dataType, TSqlFragment parameter, IReadOnlyCollection<Regex> bannedTypesExpressions, string pluralObjectType)
    {
        if (dataType is null)
        {
            return;
        }

        var dataTypeName = dataType.GetSql().Replace(" ", string.Empty, StringComparison.Ordinal);
        var isBanned = bannedTypesExpressions.Any(a => a.IsMatch(dataTypeName));
        if (!isBanned)
        {
            return;
        }

        var fullObjectName = parameter.TryGetFirstClassObjectName(_context.DefaultSchemaName, _script.ParsedScript, _script.ParentFragmentProvider);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(parameter) ?? DatabaseNames.Unknown;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, parameter.GetCodeRegion(), dataTypeName, pluralObjectType);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5006",
            IssueType.Warning,
            "Usage of banned data type",
            "The data type `{0}` is banned for {1}",
            ["Data type name", "Object type"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
