using BetterConsoleTables;
using FluentAssertions;

namespace DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks.Docs;

public sealed class GenerateDiagnosticOverview
{
    [Fact]
    public async Task CreateAsync()
    {
        const string targetFilePath = @"..\..\..\..\..\docs\diagnostics.md";
        const string templateFilePath = @"docs\source\_DiagnosticsTemplate.md";

        var template = await File.ReadAllTextAsync(templateFilePath);
        var diagnosticsDefinitionTable = GetDiagnosticsDefinitionTable();
        var contents = template.Replace("{DiagnosticsTable}", diagnosticsDefinitionTable, StringComparison.Ordinal);

        await File.WriteAllTextAsync(targetFilePath, contents);
        true.Should().BeTrue();
    }

    private static string GetDiagnosticsDefinitionTable()
    {
        var table = new Table(Alignment.Left) { Config = TableConfiguration.Markdown() };
        table.Config.hasInnerRows = false;
        _ = table
            .AddColumn("Diagnostic Id")
            .AddColumn("Title")
            .AddColumn("Type");

        var definitions = DiagnosticDefinitionProvider
            .GetDefinitionsFromAssemblies()
            .OrderBy(static a => a.DiagnosticId, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            _ = table.AddRow($"[{definition.DiagnosticId}](diagnostics/{definition.DiagnosticId}.md)", definition.Title, definition.IssueType);
        }

        return table.ToString();
    }
}
