using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;

public sealed class NamelessConstraintAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IScriptModel _script;

    public NamelessConstraintAnalyzer(IScriptAnalysisContext context)
    {
        _context = context;
        _script = context.Script;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void AnalyzeScript()
    {
        foreach (var statement in _script.ParsedScript.GetChildren<CreateTableStatement>(recursive: true))
        {
            AnalyzeTableCreation(statement);
        }

        foreach (var statement in _script.ParsedScript.GetChildren<AlterTableAddTableElementStatement>(recursive: true))
        {
            AnalyzeTableAlteration(statement);
        }
    }

    private void AnalyzeTableAlteration(AlterTableAddTableElementStatement statement)
    {
        foreach (var columnDefinition in (statement.Definition?.ColumnDefinitions).EmptyIfNull())
        {
            AnalyzeColumn(columnDefinition);
        }
    }

    private void AnalyzeTableCreation(CreateTableStatement statement)
    {
        foreach (var columnDefinition in statement.Definition.ColumnDefinitions)
        {
            AnalyzeColumn(columnDefinition);
        }
    }

    private void AnalyzeColumn(ColumnDefinition column)
    {
        foreach (var constraint in column.Constraints.EmptyIfNull())
        {
            AnalyzeConstraint(constraint);
        }

        if (column.DefaultConstraint is not null)
        {
            AnalyzeConstraint(column.DefaultConstraint);
        }
    }

    private void AnalyzeConstraint(ConstraintDefinition constraint)
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

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(constraint) ?? DatabaseNames.Unknown;
        var fullObjectName = constraint.TryGetFirstClassObjectName(_context, _script);
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, constraint.GetCodeRegion());
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5039",
            IssueType.Formatting,
            "Nameless constraints",
            "Do not create nameless constraints. It will make schema and execution plan comparison more difficult.",
            [],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
