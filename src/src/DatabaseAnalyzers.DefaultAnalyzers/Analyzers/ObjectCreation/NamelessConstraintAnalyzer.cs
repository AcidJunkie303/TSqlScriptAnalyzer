using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class NamelessConstraintAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var statement in script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeTableCreation(context, script, statement);
        }

        foreach (var statement in script.ParsedScript.GetChildren<AlterTableAddTableElementStatement>(recursive: true))
        {
            AnalyzeTableAlteration(context, script, statement);
        }
    }

    private static void AnalyzeTableAlteration(IAnalysisContext context, IScriptModel script, AlterTableAddTableElementStatement statement)
    {
        foreach (var columnDefinition in (statement.Definition?.ColumnDefinitions).EmptyIfNull())
        {
            AnalyzeColumn(context, script, columnDefinition);
        }
    }

    private static void AnalyzeTableCreation(IAnalysisContext context, IScriptModel script, CreateTableStatement statement)
    {
        foreach (var columnDefinition in statement.Definition.ColumnDefinitions)
        {
            AnalyzeColumn(context, script, columnDefinition);
        }
    }

    private static void AnalyzeColumn(IAnalysisContext context, IScriptModel script, ColumnDefinition column)
    {
        foreach (var constraint in column.Constraints.EmptyIfNull())
        {
            AnalyzeConstraint(context, script, constraint);
        }
    }

    private static void AnalyzeConstraint(IAnalysisContext context, IScriptModel script, ConstraintDefinition constraint)
    {
        // only the following constraint types can have a name
        if (constraint is not (DefaultConstraintDefinition or UniqueConstraintDefinition or CheckConstraintDefinition))
        {
            return;
        }

        if (!(constraint.ConstraintIdentifier?.Value).IsNullOrWhiteSpace())
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(constraint) ?? DatabaseNames.Unknown;
        var fullObjectName = constraint.TryGetFirstClassObjectName(context, script);
        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, constraint.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5039",
            IssueType.Formatting,
            "Nameless constraints",
            "Do not create nameless constraints. It will make schema comparison, execution plan comparison etc. more difficult.",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
