using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class AliasNameIsNameOfTableOrColumn : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly ParallelOptions _parallelOptions;
    private readonly IObjectProvider _objectProvider;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public AliasNameIsNameOfTableOrColumn(IGlobalAnalysisContext context, IIssueReporter issueReporter, ParallelOptions parallelOptions, IObjectProvider objectProvider)
    {
        _context = context;
        _issueReporter = issueReporter;
        _parallelOptions = parallelOptions;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var allTableAndViewNames = _objectProvider.DatabasesByName.Values
            .SelectMany(a => a.SchemasByName.Values)
            .SelectMany(a => (IEnumerable<string>)
            [
                .. a.ViewsByName.Select(x => x.Key),
                .. a.TablesByName.Select(x => x.Key)
            ])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(_context.Scripts, _parallelOptions, script => AnalyzeScript(script, allTableAndViewNames));
    }

    private void AnalyzeScript(IScriptModel script, HashSet<string> allTableAndViewNames)
    {
        foreach (var tableReference in script.ParsedScript.GetChildren<TableReferenceWithAlias>(recursive: true))
        {
            AnalyzeTableReference(script, allTableAndViewNames, tableReference);
        }
    }

    private void AnalyzeTableReference(IScriptModel script, HashSet<string> allTableAndViewNames, TableReferenceWithAlias tableReference)
    {
        var alias = tableReference.Alias?.Value;
        if (alias.IsNullOrWhiteSpace())
        {
            return;
        }

        if (!allTableAndViewNames.Contains(alias))
        {
            return;
        }

        var fullObjectName = tableReference.TryGetFirstClassObjectName(_context.DefaultSchemaName, script.ParsedScript, script.ParentFragmentProvider);
        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(tableReference) ?? script.DatabaseName;
        _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, tableReference.Alias!.GetCodeRegion(), alias);
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
