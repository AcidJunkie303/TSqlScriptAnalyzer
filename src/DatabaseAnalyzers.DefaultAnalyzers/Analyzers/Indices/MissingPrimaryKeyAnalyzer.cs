using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingPrimaryKeyAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5026Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public MissingPrimaryKeyAnalyzer(IGlobalAnalysisContext context, Aj5026Settings settings, IObjectProvider objectProvider)
    {
        _context = context;
        _settings = settings;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        var allTables = _objectProvider.DatabasesByName
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values);

        foreach (var table in allTables)
        {
            Analyze(table);
        }
    }

    private void Analyze(TableInformation table)
    {
        if (table.ObjectName.IsTempTableName())
        {
            return;
        }

        if (table.Indices.Any(static a => a.IndexType.HasFlag(TableColumnIndexTypes.PrimaryKey)))
        {
            return;
        }

        if (IsTableIgnored(table))
        {
            return;
        }

        var databaseName = table.ScriptModel.ParsedScript.TryFindCurrentDatabaseNameAtFragment(table.CreationStatement) ?? DatabaseNames.Unknown;
        _context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, table.ScriptModel.RelativeScriptFilePath, table.FullName, table.CreationStatement.GetCodeRegion(), table.FullName);
    }

    private bool IsTableIgnored(TableInformation table)
        => _settings.FullTableNamesToIgnore.Any(a => a.IsMatch(table.FullName));

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5026",
            IssueType.Warning,
            "Table has no primary key",
            "The table '{0}' has no primary key defined.",
            ["Table name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
