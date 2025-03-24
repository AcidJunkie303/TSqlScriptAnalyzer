using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Consistency;

public sealed class InconsistentColumnDataTypeAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5054Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public InconsistentColumnDataTypeAnalyzer(IGlobalAnalysisContext context, Aj5054Settings settings, IObjectProvider objectProvider)
    {
        _context = context;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var columnsAndScriptsByColumnName = _objectProvider.DatabasesByName
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values.Select(x => (Script: x.ScriptModel, Table: x)))
            .SelectMany(static a => a.Table.Columns.Select(x => (a.Script, a.Table, Column: x, DataType: x.ColumnDefinition.DataType.ToDataTypeString())))
            .GroupBy(static a => a.Column.ObjectName, StringComparer.OrdinalIgnoreCase)
            .Select(a => (ColumnName: a.Key, Columns: a));

        foreach (var (_, columns) in columnsAndScriptsByColumnName)
        {
            var columnsAndScripts = columns
                .Where(a => !_settings.DatabasesToExclude.Contains(a.Column.DatabaseName))
                .Where(a => !_settings.ColumnNamesToExclude.Contains(a.Column.ObjectName))
                .Where(a => !a.DataType.IsNullOrWhiteSpace())
                .ToList();

            if (columnsAndScripts.Count == 0)
            {
                continue;
            }

            var distinctDataTypeCount = columnsAndScripts.DistinctCount(a => a.DataType, StringComparer.OrdinalIgnoreCase);
            if (distinctDataTypeCount <= 1)
            {
                continue;
            }

            var dataTypeNames = columnsAndScripts
                .Select(a => a.DataType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .StringJoin(", ");

            var objectNames = columnsAndScripts
                .Select(a => a.Column.FullColumnName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .StringJoin(", ");

            var script = columnsAndScripts[0].Script;
            var column = columnsAndScripts[0].Column;

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(column.CreationStatement) ?? DatabaseNames.Unknown;
            var fullObjectName = column.CreationStatement.TryGetFirstClassObjectName(_context, script);
            _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, column.CreationStatement.GetCodeRegion(),
                column.ObjectName, dataTypeNames, objectNames);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5054",
            IssueType.Warning,
            "Inconsistent Column Data Type",
            "The column with name `{0}` has the following different data types `{1}` within the tables `{2}`.",
            ["Column Name", "Data Type Names", "Table Names"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
