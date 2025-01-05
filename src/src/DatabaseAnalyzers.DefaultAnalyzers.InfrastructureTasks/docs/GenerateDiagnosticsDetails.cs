using System.Globalization;
using BetterConsoleTables;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using FluentAssertions;

namespace DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks.Docs;

public sealed class GenerateDiagnosticsDetails
{
    private readonly string _insertionStringRowTemplate;
    private readonly string _noSettingsContents;
    private readonly string _settingsTemplate;
    private readonly string _template;

    public GenerateDiagnosticsDetails()
    {
        const string templateFilePath = @".\docs\source\_DiagnosticDetailTemplate.md";
        const string settingsFilePath = @".\docs\source\_Settings.md";
        const string noSettingsFilePath = @".\docs\source\_NoSettings.md";
        const string insertionStringRowTemplateFilePath = @".\docs\source\_InsertionStringRowTemplate.md";

        _insertionStringRowTemplate = File.ReadAllText(insertionStringRowTemplateFilePath);
        _noSettingsContents = File.ReadAllText(noSettingsFilePath);
        _settingsTemplate = File.ReadAllText(settingsFilePath);
        _template = File.ReadAllText(templateFilePath);
    }

    [Fact]
    public async Task CreateAsync()
    {
        var propertyDescriptionsByDiagnosticSettings = SettingsInformationProvider.GetPropertyDescriptionsByDiagnosticSettings();

        var definitions = DiagnosticDefinitionProvider
            .GetDefinitionsFromAssemblies()
            .OrderBy(a => a.DiagnosticId, StringComparer.OrdinalIgnoreCase);

        foreach (var diagnostic in definitions)
        {
            var propertyDescribers = propertyDescriptionsByDiagnosticSettings.GetValueOrDefault(diagnostic.DiagnosticId);
            await CreatDiagnosticFileAsync(diagnostic, propertyDescribers);
        }

        true.Should().BeTrue();
    }

    private async Task CreatDiagnosticFileAsync(IDiagnosticDefinition definition, List<SettingsInformationProvider.PropertyDescriber>? settingsPropertyDescribers)
    {
        var mainFileContents = await GetDiagnosticDetailMainFileContentsAsync(definition);
        var settingsFileContents = await GetSettingsContentsAsync(definition, settingsPropertyDescribers);
        var contents = _template
            .Replace("{DiagnosticId}", definition.DiagnosticId, StringComparison.Ordinal)
            .Replace("{Title}", definition.Title, StringComparison.Ordinal)
            .Replace("{Main}", mainFileContents, StringComparison.Ordinal)
            .Replace("{Settings}", settingsFileContents, StringComparison.Ordinal)
            .Replace("{DiagnosticId}", definition.DiagnosticId, StringComparison.Ordinal)
            .Replace("{Title}", definition.Title, StringComparison.Ordinal)
            .Replace("{MessageTemplate}", definition.MessageTemplate, StringComparison.Ordinal)
            .Replace("{IssueType}", definition.IssueType.ToString(), StringComparison.Ordinal)
            .Replace("{InsertionStrings}", GetInsertionStringRows(definition), StringComparison.Ordinal);

        var path = $@"..\..\..\..\..\..\docs\diagnostics\{definition.DiagnosticId.ToUpperInvariant()}.md";
        await File.WriteAllTextAsync(path, contents);
    }

    private string GetInsertionStringRows(IDiagnosticDefinition definition)
        => definition.InsertionStringDescriptions
            .Select((str, index) => _insertionStringRowTemplate
                .Replace("{Index}", index.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{Description}", str, StringComparison.Ordinal)
            )
            .StringJoin(string.Empty);

    private async Task<string> GetSettingsContentsAsync(IDiagnosticDefinition definition, List<SettingsInformationProvider.PropertyDescriber>? settingsPropertyDescribers)
    {
        var settingsContents = await GetDiagnosticDetailSettingsFileContentsAsync(definition);
        if (settingsContents.IsNullOrWhiteSpace())
        {
            return _noSettingsContents;
        }

        if (settingsPropertyDescribers.IsNullOrEmpty())
        {
            return _noSettingsContents;
        }

        var propertiesTable = CreateSettingsPropertiesTable(settingsPropertyDescribers);

        return _settingsTemplate
            .Replace("{SettingsJson}", settingsContents, StringComparison.Ordinal)
            .Replace("{SettingsProperties}", propertiesTable, StringComparison.Ordinal);
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
            : string.Empty;
    }
}
