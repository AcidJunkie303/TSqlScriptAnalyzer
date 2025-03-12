using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.NonStandard;

public sealed class ReservedWordUsageAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5060Settings>();

        foreach (var fragment in script.ParsedScript.GetChildren(recursive: true))
        {
            if (fragment is CreateTableStatement createTableStatement)
            {
                Analyze(context, script, settings, "table", createTableStatement.SchemaObjectName?.BaseIdentifier, a => a.Value);
            }
            else if (fragment is ViewStatementBody createViewStatement)
            {
                Analyze(context, script, settings, "view", createViewStatement.SchemaObjectName?.BaseIdentifier, a => a.Value);
            }
            else if (fragment is CreateProcedureStatement createProcedureStatement)
            {
                Analyze(context, script, settings, "procedure", createProcedureStatement.ProcedureReference?.Name.BaseIdentifier, a => a.Value);
            }
            else if (fragment is ColumnDefinition columnDefinition)
            {
                Analyze(context, script, settings, "column", columnDefinition.ColumnIdentifier, a => a.Value);
            }
            else if (fragment is FunctionStatementBody createFunctionStatement)
            {
                Analyze(context, script, settings, "function", createFunctionStatement.Name.BaseIdentifier, a => a.Value);
            }
        }
    }

    private static void Analyze<T>(IAnalysisContext context, IScriptModel script, Aj5060Settings settings, string objectTypeName, T? objectIdentifierFragment, Func<T, string?> nameGetter)
        where T : TSqlFragment
    {
        if (objectIdentifierFragment is null)
        {
            return;
        }

        var name = nameGetter(objectIdentifierFragment);
        if (name.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!settings.ReservedIdentifierNames.Contains(name))
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(objectIdentifierFragment) ?? DatabaseNames.Unknown;
        var fullObjectName = objectIdentifierFragment.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, objectIdentifierFragment.GetCodeRegion(),
            objectTypeName, name);
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5060",
            IssueType.Warning,
            "Reserved Word Usage",
            "The `{0}` name `{1}` is a reserved word. Use another name instead.",
            ["Object Type Name", "Reserved Word"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
