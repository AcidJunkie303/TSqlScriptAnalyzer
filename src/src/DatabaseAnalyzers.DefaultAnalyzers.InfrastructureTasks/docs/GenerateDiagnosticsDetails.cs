using BetterConsoleTables;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using FluentAssertions;

namespace DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks.Docs;

public sealed class GenerateDiagnosticsDetails
{
    [Fact]
    public async Task CreateAsync()
    {
        const string templateFilePath = @".\docs\source\_DiagnosticDetailTemplate.md";
        var propertyDescriptionsByDiagnosticSettings = SettingsInformationProvider.GetPropertyDescriptionsByDiagnosticSettings();
        var template = await File.ReadAllTextAsync(templateFilePath);
        var definitions = DiagnosticDefinitionProvider
            .GetDefinitionsFromAssemblies()
            .OrderBy(a => a.DiagnosticId, StringComparer.OrdinalIgnoreCase);

        foreach (var diagnostic in definitions)
        {
            var propertyDescribers = propertyDescriptionsByDiagnosticSettings.GetValueOrDefault(diagnostic.DiagnosticId);
            await CreatDiagnosticFileAsync(diagnostic, template, propertyDescribers);
        }

        true.Should().BeTrue();
    }

    private static string CreateSettingsPropertiesTable(List<SettingsInformationProvider.PropertyDescriber> propertyDescribers)
    {
        var table = new Table(Alignment.Left) { Config = TableConfiguration.Markdown() };
        table.Config.hasInnerRows = false;
        _ = table
            .AddColumn("Property Name")
            .AddColumn("Description");

        foreach (var propertyDescriber in propertyDescribers)
        {
            _ = table.AddRow(propertyDescriber.PropertyName, propertyDescriber.Description ?? "*No description provided*");
        }

        return table.ToString();
    }

    private static async Task CreatDiagnosticFileAsync(IDiagnosticDefinition definition, string template, List<SettingsInformationProvider.PropertyDescriber>? settingsPropertyDescribers)
    {
        var mainFileContents = await GetDiagnosticDetailMainFileContentsAsync(definition);
        var settingsFileContents = await GetDiagnosticDetailSettingsFileContentsAsync(definition);
        var settingsPropertiesTable = settingsPropertyDescribers.IsNullOrEmpty()
            ? "*none*"
            : CreateSettingsPropertiesTable(settingsPropertyDescribers);
        var contents = template
            .Replace("{DiagnosticId}", definition.DiagnosticId, StringComparison.Ordinal)
            .Replace("{Title}", definition.Title, StringComparison.Ordinal)
            .Replace("{Main}", mainFileContents, StringComparison.Ordinal)
            .Replace("{Settings}", settingsFileContents, StringComparison.Ordinal)
            .Replace("{SettingsProperties}", settingsPropertiesTable, StringComparison.Ordinal);

        var path = $@"..\..\..\..\..\..\docs\diagnostics\{definition.DiagnosticId.ToUpperInvariant()}.md";
        await File.WriteAllTextAsync(path, contents);
    }

    private static async Task<string> GetDiagnosticDetailMainFileContentsAsync(IDiagnosticDefinition definition)
    {
        var path = $@".\docs\source\{definition.DiagnosticId.ToUpperInvariant()}.main.md";
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path)
            : string.Empty;
    }

    private static async Task<string> GetDiagnosticDetailSettingsFileContentsAsync(IDiagnosticDefinition definition)
    {
        var path = $@".\docs\source\{definition.DiagnosticId.ToUpperInvariant()}.settings.md";
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path)
            : "*none*";
    }
}
