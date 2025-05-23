using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Consistency;

public sealed class InconsistentColumnNameCasingAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly Aj5055Settings _settings;
    private readonly IObjectProvider _objectProvider;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public InconsistentColumnNameCasingAnalyzer(IGlobalAnalysisContext context, IIssueReporter issueReporter, Aj5055Settings settings, IObjectProvider  objectProvider)
    {
        _context = context;
        _issueReporter = issueReporter;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var columnsAndScriptsByColumnName =_objectProvider.DatabasesByName
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values.Select(x => (Script: x.ScriptModel, Table: x)))
            .Where(a => !_settings.ExcludedDatabaseNames.Contains(a.Table.DatabaseName))
            .SelectMany(static a => a.Table.Columns.Select(x => (a.Script, a.Table, Column: x)))
            .GroupBy(static a => a.Column.ObjectName, StringComparer.OrdinalIgnoreCase)
            .Select(a => (ColumnName: a.Key, ColumnsData: a.ToList()));

        foreach (var columnData in columnsAndScriptsByColumnName.Select(a => a.ColumnsData))
        {
            var casingVariations = columnData
                .Select(a => a.Column.ObjectName)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (casingVariations.Count < 2)
            {
                continue;
            }

            var flatCasingVariations = casingVariations.StringJoin(", ");
            var objectNames = columnData
                .Select(a => a.Column.FullColumnName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .StringJoin(", ");

            var column = columnData[0].Column;
            var script = columnData[0].Script;

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(column.CreationStatement) ?? DatabaseNames.Unknown;
            var fullObjectName = column.CreationStatement.TryGetFirstClassObjectName(_context, script);
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, column.CreationStatement.GetCodeRegion(),
                column.ObjectName, flatCasingVariations, objectNames);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5055",
            IssueType.Warning,
            "Inconsistent Column Name Casing",
            "The column with name `{0}` has different casing variations (`{1}`) within the tables `{2}`.",
            ["Column Name", "Casing Variations  ", "Table Names"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
