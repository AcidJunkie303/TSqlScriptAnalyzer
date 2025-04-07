using System.Globalization;
using System.Text;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Consistency;

public sealed class InconsistentColumnDataTypeAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5054Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public InconsistentColumnDataTypeAnalyzer(IGlobalAnalysisContext context, IIssueReporter issueReporter, Aj5054Settings settings, IObjectProvider objectProvider)
    {
        _context = context;
        _issueReporter = issueReporter;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var columnsWithSameNameBlock = _objectProvider.DatabasesByName
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values.Select(x => (Script: x.ScriptModel, Table: x)))
            .SelectMany(static a => a.Table.Columns.Select(x => (a.Script, Column: x, DataType: x.ColumnDefinition.DataType.ToDataTypeString(quote: false))))
            .GroupBy(static a => a.Column.ObjectName, StringComparer.OrdinalIgnoreCase)
            .Select(static a => a.AsEnumerable());

        foreach (var columns in columnsWithSameNameBlock)
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

            var dataTypesAndColumnNames = CreateDataTypesAndColumnNames(columnsAndScripts);

            var script = columnsAndScripts[0].Script;
            var column = columnsAndScripts[0].Column;

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(column.CreationStatement) ?? DatabaseNames.Unknown;
            var fullObjectName = column.CreationStatement.TryGetFirstClassObjectName(_context, script);
            _issueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, column.CreationStatement.GetCodeRegion(),
                column.ObjectName, dataTypesAndColumnNames);
        }
    }

    private string CreateDataTypesAndColumnNames(List<(IScriptModel Script, ColumnInformation Column, string DataType)> columnsAndScripts)
    {
        int maxLengthLeftColumn;
        IEnumerable<(string Part1, string Part2)> parts;
        if (_settings.LayoutType == Aj5054LayoutType.DataTypeThenFullColumnName)
        {
            maxLengthLeftColumn = columnsAndScripts.Max(a => a.DataType.Length);
            parts = columnsAndScripts
                .Select(a => (Part1: a.DataType, Part2: a.Column.FullColumnName))
                .OrderBy(a => a.Part1, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.Part2, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            maxLengthLeftColumn = columnsAndScripts.Max(a => a.Column.FullColumnName.Length);
            parts = columnsAndScripts
                .Select(a => (Part1: a.Column.FullColumnName, Part2: a.DataType))
                .OrderBy(a => a.Part1, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.Part2, StringComparer.OrdinalIgnoreCase);
        }

        var buffer = new StringBuilder();
        foreach (var (part1, part2) in parts)
        {
            if (buffer.Length != 0)
            {
                buffer.AppendLine();
            }

            buffer.Append(CultureInfo.InvariantCulture, $"{part1.PadRight(maxLengthLeftColumn)} | {part2}");
        }

        return buffer.ToString();
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5054",
            IssueType.Warning,
            "Inconsistent Column Data Type",
            "The column with name `{0}` has different data types: ```{1}```",
            ["Column Name", "Full column names and data types"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
