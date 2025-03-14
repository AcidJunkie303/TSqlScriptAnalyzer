using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NamingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5030Settings>();

        var tables = script.ParsedScript.GetTopLevelDescendantsOfType<CreateTableStatement>(script.ParentFragmentProvider);
        var triggers = script.ParsedScript.GetTopLevelDescendantsOfType<TriggerStatementBody>(script.ParentFragmentProvider);
        var variables = script.ParsedScript.GetTopLevelDescendantsOfType<DeclareVariableStatement>(script.ParentFragmentProvider);
        var views = script.ParsedScript.GetTopLevelDescendantsOfType<ViewStatementBody>(script.ParentFragmentProvider);
        var functions = script.ParsedScript
            .GetTopLevelDescendantsOfType<FunctionStatementBody>(script.ParentFragmentProvider)
            .ToList();
        var procedures = script.ParsedScript
            .GetTopLevelDescendantsOfType<ProcedureStatementBody>(script.ParentFragmentProvider)
            .ToList();

        IReadOnlyList<ProcedureParameter> parameters =
        [
            .. functions.SelectMany(static a => a.Parameters),
            .. procedures.SelectMany(static a => a.Parameters)
        ];

        AnalyzeViews(context, script, settings, views);
        AnalyzeVariables(context, script, settings, variables);
        AnalyzeTables(context, script, settings, tables);
        AnalyzeTriggers(context, script, settings, triggers);
        AnalyzeProcedures(context, script, settings, procedures);
        AnalyzeFunctions(context, script, settings, functions);
        AnalyzeParameters(context, script, settings, parameters);
    }

    private static void AnalyzeParameters(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<ProcedureParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            Analyze(context, script, parameter, "parameter", settings, settings.ParameterName, ParameterNameGetter, FragmentToReportGetter, ParameterNameToReportGetter);
        }

        static string ParameterNameGetter(ProcedureParameter a) => a.VariableName.Value.Trim('@');
        static TSqlFragment FragmentToReportGetter(ProcedureParameter a) => a.VariableName;
        static string ParameterNameToReportGetter(ProcedureParameter a) => a.VariableName.Value;
    }

    private static void AnalyzeFunctions(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<FunctionStatementBody> functions)
    {
        foreach (var function in functions)
        {
            Analyze(context, script, function, "function", settings, settings.FunctionName, FunctionNameGetter, FragmentToReportGetter, ParameterNameToReportGetter);
        }

        static string FunctionNameGetter(FunctionStatementBody a) => a.Name.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(FunctionStatementBody a) => a.Name.BaseIdentifier;
        static string ParameterNameToReportGetter(FunctionStatementBody a) => a.Name.BaseIdentifier.Value;
    }

    private static void AnalyzeProcedures(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<ProcedureStatementBody> procedures)
    {
        foreach (var procedure in procedures)
        {
            Analyze(context, script, procedure, "procedure", settings, settings.ProcedureName, ProcedureNameGetter, FragmentToReportGetter, ProcedureNameToReportGetter);
        }

        static string ProcedureNameGetter(ProcedureStatementBody a) => a.ProcedureReference.Name.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(ProcedureStatementBody a) => a.ProcedureReference.Name.BaseIdentifier;
        static string ProcedureNameToReportGetter(ProcedureStatementBody a) => a.ProcedureReference.Name.BaseIdentifier.Value;
    }

    private static void AnalyzeTriggers(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<TriggerStatementBody> tiggers)
    {
        foreach (var trigger in tiggers)
        {
            Analyze(context, script, trigger, "trigger", settings, settings.TriggerName, TriggerNameGetter, FragmentToReportGetter, TriggerNameToReportGetter);
        }

        static string? TriggerNameGetter(TriggerStatementBody a) => a.Name.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(TriggerStatementBody a) => a.Name.BaseIdentifier;
        static string? TriggerNameToReportGetter(TriggerStatementBody a) => a.Name.BaseIdentifier.Value;
    }

    private static void AnalyzeVariables(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<DeclareVariableStatement> variables)
    {
        foreach (var variable in variables)
        {
            foreach (var declaration in variable.Declarations)
            {
                if (!declaration.VariableName.Value.StartsWith('@'))
                {
                    continue;
                }

                Analyze(context, script, declaration, "variable", settings, settings.VariableName, VariableNameGetter, FragmentToReportGetter, VariableNameToReportGetter);
            }
        }

        static string VariableNameGetter(DeclareVariableElement a) => a.VariableName.Value.Trim('@');
        static TSqlFragment FragmentToReportGetter(DeclareVariableElement a) => a.VariableName;
        static string? VariableNameToReportGetter(DeclareVariableElement a) => a.VariableName.Value;
    }

    private static void AnalyzeViews(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<ViewStatementBody> views)
    {
        foreach (var view in views)
        {
            Analyze(context, script, view, "view", settings, settings.ViewName, ViewNameGetter, FragmentToReportGetter, ViewNameToReportGetter);
        }

        static string? ViewNameGetter(ViewStatementBody a) => a.SchemaObjectName.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(ViewStatementBody a) => a.SchemaObjectName.BaseIdentifier;
        static string? ViewNameToReportGetter(ViewStatementBody a) => a.SchemaObjectName.BaseIdentifier.Value;
    }

    private static void AnalyzeTables(IAnalysisContext context, IScriptModel script, Aj5030Settings settings, IEnumerable<CreateTableStatement> tables)
    {
        foreach (var table in tables)
        {
            if (table.IsTempTable())
            {
                Analyze(context, script, table, "temp-table", settings, settings.TempTableName, TableNameGetter, TableFragmentToReportGetter, TableNameToReportGetter);
            }
            else
            {
                Analyze(context, script, table, "table", settings, settings.TableName, TableNameGetter, TableFragmentToReportGetter, TableNameToReportGetter);
            }

            foreach (var column in table.Definition.ColumnDefinitions)
            {
                Analyze(context, script, column, "column", settings, settings.ColumnName, ColumnNameGetter, ColumnFragmentToReportGetter, ColumnNameToReportGetter);
            }
        }

        static string? TableNameGetter(CreateTableStatement a) => a.SchemaObjectName.BaseIdentifier.Value;
        static TSqlFragment TableFragmentToReportGetter(CreateTableStatement a) => a.SchemaObjectName.BaseIdentifier;
        static string? TableNameToReportGetter(CreateTableStatement a) => a.SchemaObjectName.BaseIdentifier.Value;

        static string? ColumnNameGetter(ColumnDefinition a) => a.ColumnIdentifier.Value;
        static TSqlFragment ColumnFragmentToReportGetter(ColumnDefinition a) => a.ColumnIdentifier;
        static string? ColumnNameToReportGetter(ColumnDefinition a) => a.ColumnIdentifier.Value;
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
    private static void Analyze<T>
    (
        IAnalysisContext context,
        IScriptModel script,
        T statement,
        string objectTypeName,
        Aj5030Settings settings,
        Aj5030Settings.PatternEntry patternEntry,
        Func<T, string?> nameGetter,
        Func<T, TSqlFragment> fragmentToReportGetter,
        Func<T, string?> nameToReportGetter
    )
        where T : TSqlFragment
    {
        var name = nameGetter(statement);
        if (name is null || patternEntry.Pattern.IsMatch(name))
        {
            return;
        }

        var nameToReport = nameToReportGetter(statement) ?? name;

        Report(context, script, fragmentToReportGetter(statement), settings, objectTypeName, nameToReport, patternEntry.Description);
    }

    private static void Report(IAnalysisContext context, IScriptModel script, TSqlFragment fragment, Aj5030Settings settings, string objectTypeName, string objectName, string ruleDescription)
    {
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? DatabaseNames.Unknown;
        var fullObjectName = fragment.TryGetFirstClassObjectName(context, script);

        if (IsIgnored(settings, fullObjectName ?? objectTypeName))
        {
            return;
        }

        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, fragment.GetCodeRegion(), objectTypeName, objectName, ruleDescription);
    }

    private static bool IsIgnored(Aj5030Settings settings, string fullObjectName)
    {
        if (settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        return settings.IgnoredObjectNamePatterns.Any(pattern => pattern.IsMatch(fullObjectName));
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5030",
            IssueType.Warning,
            "Object name violates naming convention",
            "The {0} name `{1}` does not comply with the configured naming rule: `{2}`.",
            ["Object type name", "Name", "Reason"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
