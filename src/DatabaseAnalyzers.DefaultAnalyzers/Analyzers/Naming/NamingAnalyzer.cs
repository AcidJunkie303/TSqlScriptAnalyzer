using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NamingAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;
    private readonly Aj5030Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public NamingAnalyzer(IScriptAnalysisContext context, Aj5030Settings settings)
    {
        _context = context;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        var tables = _script.ParsedScript.GetTopLevelDescendantsOfType<CreateTableStatement>(_script.ParentFragmentProvider);
        var triggers = _script.ParsedScript.GetTopLevelDescendantsOfType<TriggerStatementBody>(_script.ParentFragmentProvider);
        var variables = _script.ParsedScript.GetTopLevelDescendantsOfType<DeclareVariableStatement>(_script.ParentFragmentProvider);
        var views = _script.ParsedScript.GetTopLevelDescendantsOfType<ViewStatementBody>(_script.ParentFragmentProvider);
        var tableReferences = _script.ParsedScript.GetTopLevelDescendantsOfType<TableReferenceWithAlias>(_script.ParentFragmentProvider);
        var functions = _script.ParsedScript
            .GetTopLevelDescendantsOfType<FunctionStatementBody>(_script.ParentFragmentProvider)
            .ToList();
        var procedures = _script.ParsedScript
            .GetTopLevelDescendantsOfType<ProcedureStatementBody>(_script.ParentFragmentProvider)
            .ToList();

        IReadOnlyList<ProcedureParameter> parameters =
        [
            .. functions.SelectMany(static a => a.Parameters),
            .. procedures.SelectMany(static a => a.Parameters)
        ];

        AnalyzeViews(views);
        AnalyzeVariables(variables);
        AnalyzeTables(tables);
        AnalyzeTriggers(triggers);
        AnalyzeProcedures(procedures);
        AnalyzeFunctions(functions);
        AnalyzeParameters(parameters);
        AnalyzeTableReferences(tableReferences);
    }

    private void AnalyzeTableReferences(IEnumerable<TableReferenceWithAlias> tableReferences)
    {
        foreach (var tableReference in tableReferences)
        {
            if (string.IsNullOrEmpty(tableReference.Alias?.Value))
            {
                continue;
            }

            Analyze(tableReference, "alias", _settings.TableAliasName, AliasNameGetter, FragmentToReportGetter, AliasNameToReportGetter);
        }

        static string AliasNameGetter(TableReferenceWithAlias a) => a.Alias.Value;
        static TSqlFragment FragmentToReportGetter(TableReferenceWithAlias a) => a.Alias;
        static string AliasNameToReportGetter(TableReferenceWithAlias a) => a.Alias.Value;
    }

    private void AnalyzeParameters(IEnumerable<ProcedureParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            Analyze(parameter, "parameter", _settings.ParameterName, ParameterNameGetter, FragmentToReportGetter, ParameterNameToReportGetter);
        }

        static string ParameterNameGetter(ProcedureParameter a) => a.VariableName.Value.Trim('@');
        static TSqlFragment FragmentToReportGetter(ProcedureParameter a) => a.VariableName;
        static string ParameterNameToReportGetter(ProcedureParameter a) => a.VariableName.Value;
    }

    private void AnalyzeFunctions(IEnumerable<FunctionStatementBody> functions)
    {
        foreach (var function in functions)
        {
            Analyze(function, "function", _settings.FunctionName, FunctionNameGetter, FragmentToReportGetter, ParameterNameToReportGetter);
        }

        static string FunctionNameGetter(FunctionStatementBody a) => a.Name.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(FunctionStatementBody a) => a.Name.BaseIdentifier;
        static string ParameterNameToReportGetter(FunctionStatementBody a) => a.Name.BaseIdentifier.Value;
    }

    private void AnalyzeProcedures(IEnumerable<ProcedureStatementBody> procedures)
    {
        foreach (var procedure in procedures)
        {
            Analyze(procedure, "procedure", _settings.ProcedureName, ProcedureNameGetter, FragmentToReportGetter, ProcedureNameToReportGetter);
        }

        static string ProcedureNameGetter(ProcedureStatementBody a) => a.ProcedureReference.Name.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(ProcedureStatementBody a) => a.ProcedureReference.Name.BaseIdentifier;
        static string ProcedureNameToReportGetter(ProcedureStatementBody a) => a.ProcedureReference.Name.BaseIdentifier.Value;
    }

    private void AnalyzeTriggers(IEnumerable<TriggerStatementBody> tiggers)
    {
        foreach (var trigger in tiggers)
        {
            Analyze(trigger, "trigger", _settings.TriggerName, TriggerNameGetter, FragmentToReportGetter, TriggerNameToReportGetter);
        }

        static string? TriggerNameGetter(TriggerStatementBody a) => a.Name.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(TriggerStatementBody a) => a.Name.BaseIdentifier;
        static string? TriggerNameToReportGetter(TriggerStatementBody a) => a.Name.BaseIdentifier.Value;
    }

    private void AnalyzeVariables(IEnumerable<DeclareVariableStatement> variables)
    {
        foreach (var variable in variables)
        {
            foreach (var declaration in variable.Declarations)
            {
                if (!declaration.VariableName.Value.StartsWith('@'))
                {
                    continue;
                }

                Analyze(declaration, "variable", _settings.VariableName, VariableNameGetter, FragmentToReportGetter, VariableNameToReportGetter);
            }
        }

        static string VariableNameGetter(DeclareVariableElement a) => a.VariableName.Value.Trim('@');
        static TSqlFragment FragmentToReportGetter(DeclareVariableElement a) => a.VariableName;
        static string? VariableNameToReportGetter(DeclareVariableElement a) => a.VariableName.Value;
    }

    private void AnalyzeViews(IEnumerable<ViewStatementBody> views)
    {
        foreach (var view in views)
        {
            Analyze(view, "view", _settings.ViewName, ViewNameGetter, FragmentToReportGetter, ViewNameToReportGetter);
        }

        static string? ViewNameGetter(ViewStatementBody a) => a.SchemaObjectName.BaseIdentifier.Value;
        static TSqlFragment FragmentToReportGetter(ViewStatementBody a) => a.SchemaObjectName.BaseIdentifier;
        static string? ViewNameToReportGetter(ViewStatementBody a) => a.SchemaObjectName.BaseIdentifier.Value;
    }

    private void AnalyzeTables(IEnumerable<CreateTableStatement> tables)
    {
        foreach (var table in tables)
        {
            if (table.IsTempTable())
            {
                Analyze(table, "temp-table", _settings.TempTableName, TableNameGetter, TableFragmentToReportGetter, TableNameToReportGetter);
            }
            else
            {
                Analyze(table, "table", _settings.TableName, TableNameGetter, TableFragmentToReportGetter, TableNameToReportGetter);
            }

            foreach (var column in table.Definition.ColumnDefinitions)
            {
                Analyze(column, "column", _settings.ColumnName, ColumnNameGetter, ColumnFragmentToReportGetter, ColumnNameToReportGetter);
            }
        }

        static string? TableNameGetter(CreateTableStatement a) => a.SchemaObjectName.BaseIdentifier.Value;
        static TSqlFragment TableFragmentToReportGetter(CreateTableStatement a) => a.SchemaObjectName.BaseIdentifier;
        static string? TableNameToReportGetter(CreateTableStatement a) => a.SchemaObjectName.BaseIdentifier.Value;

        static string? ColumnNameGetter(ColumnDefinition a) => a.ColumnIdentifier.Value;
        static TSqlFragment ColumnFragmentToReportGetter(ColumnDefinition a) => a.ColumnIdentifier;
        static string? ColumnNameToReportGetter(ColumnDefinition a) => a.ColumnIdentifier.Value;
    }

    private void Analyze<T>
    (
        T statement,
        string objectTypeName,
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

        Report(fragmentToReportGetter(statement), objectTypeName, nameToReport, patternEntry.Description);
    }

    private void Report(TSqlFragment fragment, string objectTypeName, string objectName, string ruleDescription)
    {
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? DatabaseNames.Unknown;
        var fullObjectName = fragment.TryGetFirstClassObjectName(_context, _script);

        if (IsIgnored(fullObjectName ?? objectTypeName))
        {
            return;
        }

        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, fragment.GetCodeRegion(), objectTypeName, objectName, ruleDescription);
    }

    private bool IsIgnored(string fullObjectName)
    {
        if (_settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        return _settings.IgnoredObjectNamePatterns.Any(pattern => pattern.IsMatch(fullObjectName));
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
