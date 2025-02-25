using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class NamingAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5030Settings>();

        var functions = script.ParsedScript
            .GetTopLevelDescendantsOfType<FunctionStatementBody>(script.ParentFragmentProvider)
            .ToList();
        var procedures = script.ParsedScript
            .GetTopLevelDescendantsOfType<ProcedureStatementBody>(script.ParentFragmentProvider)
            .ToList();
        var tables = script.ParsedScript.GetTopLevelDescendantsOfType<CreateTableStatement>(script.ParentFragmentProvider);
        var triggers = script.ParsedScript.GetTopLevelDescendantsOfType<TriggerStatementBody>(script.ParentFragmentProvider);
        var primaryKeyConstraints = script.ParsedScript.GetTopLevelDescendantsOfType<UniqueConstraintDefinition>(script.ParentFragmentProvider).Where(static a => a.IsPrimaryKey);
        var variables = script.ParsedScript.GetTopLevelDescendantsOfType<DeclareVariableStatement>(script.ParentFragmentProvider);
        var views = script.ParsedScript.GetTopLevelDescendantsOfType<ViewStatementBody>(script.ParentFragmentProvider);

        IReadOnlyList<ProcedureParameter> parameters =
        [
            .. functions.SelectMany(static a => a.Parameters),
            .. procedures.SelectMany(static a => a.Parameters)
        ];

        foreach (var view in views)
        {
            Analyze(context, script, view, "view", settings.ViewName, static a => a.SchemaObjectName.BaseIdentifier.Value, static a => a.SchemaObjectName.BaseIdentifier);
        }

        foreach (var variable in variables)
        {
            foreach (var declaration in variable.Declarations)
            {
                if (!declaration.VariableName.Value.StartsWith('@'))
                {
                    continue;
                }

                Analyze(context, script, declaration, "variable", settings.VariableName, static a => a.VariableName.Value.Trim('@'), static a => a.VariableName);
            }
        }

        foreach (var constraint in primaryKeyConstraints)
        {
            Analyze(context, script, constraint, "primary key constraint", settings.PrimaryKeyConstraintName, static a => a.ConstraintIdentifier?.Value, static a => a.ConstraintIdentifier);
        }

        foreach (var procedure in procedures)
        {
            Analyze(context, script, procedure, "procedure", settings.ProcedureName, static a => a.ProcedureReference.Name.BaseIdentifier.Value, static a => a.ProcedureReference.Name.BaseIdentifier);
        }

        foreach (var function in functions)
        {
            Analyze(context, script, function, "function", settings.FunctionName, static a => a.Name.BaseIdentifier.Value, static a => a.Name.BaseIdentifier);
        }

        foreach (var parameter in parameters)
        {
            Analyze(context, script, parameter, "parameter", settings.ParameterName, static a => a.VariableName.Value.Trim('@'), static a => a.VariableName);
        }

        foreach (var trigger in triggers)
        {
            Analyze(context, script, trigger, "trigger", settings.TriggerName, static a => a.Name.BaseIdentifier.Value, static a => a.Name.BaseIdentifier);
        }

        foreach (var table in tables)
        {
            Analyze(context, script, table, "table", settings.TableName, static a => a.SchemaObjectName.BaseIdentifier.Value, static a => a.SchemaObjectName.BaseIdentifier);

            foreach (var column in table.Definition.ColumnDefinitions)
            {
                Analyze(context, script, column, "column", settings.ColumnName, static a => a.ColumnIdentifier.Value, static a => a.ColumnIdentifier);
            }
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, T statement, string objectTypeName, Aj5030Settings.PatternEntry patternEntry, Func<T, string?> nameGetter, Func<T, TSqlFragment> fragmentToReportGetter)
        where T : TSqlFragment
    {
        var name = nameGetter(statement);
        if (name is null || patternEntry.Pattern.IsMatch(name))
        {
            return;
        }

        Report(context, script, fragmentToReportGetter(statement), objectTypeName, name, patternEntry.Description);
    }

    private static void Report(IAnalysisContext context, IScriptModel script, TSqlFragment fragment, string objectTypeName, string objectName, string ruleDescription)
    {
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragment) ?? DatabaseNames.Unknown;
        var fullObjectName = fragment.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, fragment.GetCodeRegion(), objectTypeName, objectName, ruleDescription);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5030",
            IssueType.Warning,
            "Object name violates naming convention",
            "The {0} name `{1}` does not comply with the configured naming rules: {2}.",
            ["Object type name", "Name", "Reason"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
