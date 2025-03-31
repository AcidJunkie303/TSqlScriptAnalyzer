using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.NonStandard;

public sealed class ReservedWordUsageAnalyzer : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IScriptModel _script;
    private readonly Aj5060Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public ReservedWordUsageAnalyzer(IScriptAnalysisContext context, IIssueReporter issueReporter, Aj5060Settings settings)
    {
        _context = context;
        _issueReporter = issueReporter;
        _script = context.Script;
        _settings = settings;
    }

    public void AnalyzeScript()
    {
        foreach (var fragment in _script.ParsedScript.GetChildren(recursive: true))
        {
            if (fragment is CreateTableStatement createTableStatement)
            {
                Analyze("table", createTableStatement.SchemaObjectName?.BaseIdentifier, static a => a.Value);
            }
            else if (fragment is ViewStatementBody createViewStatement)
            {
                Analyze("view", createViewStatement.SchemaObjectName?.BaseIdentifier, static a => a.Value);
            }
            else if (fragment is CreateProcedureStatement createProcedureStatement)
            {
                Analyze("procedure", createProcedureStatement.ProcedureReference?.Name.BaseIdentifier, static a => a.Value);
            }
            else if (fragment is ColumnDefinition columnDefinition)
            {
                Analyze("column", columnDefinition.ColumnIdentifier, static a => a.Value);
            }
            else if (fragment is FunctionStatementBody createFunctionStatement)
            {
                Analyze("function", createFunctionStatement.Name.BaseIdentifier, static a => a.Value);
            }
        }
    }

    private void Analyze<T>(string objectTypeName, T? objectIdentifierFragment, Func<T, string?> nameGetter)
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

        if (!_settings.ReservedIdentifierNames.Contains(name))
        {
            return;
        }

        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(objectIdentifierFragment) ?? DatabaseNames.Unknown;
        var fullObjectName = objectIdentifierFragment.TryGetFirstClassObjectName(_context, _script);
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, objectIdentifierFragment.GetCodeRegion(),
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
