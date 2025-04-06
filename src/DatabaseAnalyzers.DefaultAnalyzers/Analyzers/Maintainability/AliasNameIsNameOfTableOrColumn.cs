using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class AliasNameIsNameOfTableOrColumn : IScriptAnalyzer
{
    private readonly IScriptAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly IScriptModel _script;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public AliasNameIsNameOfTableOrColumn(IScriptAnalysisContext context, IIssueReporter issueReporter, IObjectProvider objectProvider)
    {
        _context = context;
        _issueReporter = issueReporter;
        _objectProvider = objectProvider;
        _script = context.Script;
    }

    public void AnalyzeScript()
    {
        foreach (var tableReference in _script.ParsedScript.GetChildren<TableReferenceWithAlias>(recursive: true))
        {
            AnalyzeTableReference(tableReference);
        }
    }

    private void AnalyzeTableReference(TableReferenceWithAlias tableReference)
    {
        var alias = tableReference.Alias?.Value;
        if (alias.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!IsAliasNameSameAsTableOrViewName(alias))
        {
            return;
        }

        var fullObjectName = tableReference.TryGetFirstClassObjectName(_context.DefaultSchemaName, _script.ParsedScript, _script.ParentFragmentProvider);
        var databaseName = _script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(tableReference) ?? _script.DatabaseName;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, _script.RelativeScriptFilePath, fullObjectName, tableReference.Alias!.GetCodeRegion(), alias);
    }

    private bool IsAliasNameSameAsTableOrViewName(string alias)
    {
        if (_script.DatabaseName.IsNullOrWhiteSpace())
        {
            return false;
        }

        var database = _objectProvider.DatabasesByName.GetValueOrDefault(_script.DatabaseName);
        if (database is null)
        {
            return false;
        }

        foreach (var schema in database.SchemasByName.Values)
        {
            if (schema.TablesByName.ContainsKey(alias) || schema.ViewsByName.ContainsKey(alias))
            {
                return true;
            }
        }

        return false;
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5067",
            IssueType.Warning,
            "Alias is name of existing table or view",
            "The alias `{0}` shares the name with an existing table or view.",
            ["Alias name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
