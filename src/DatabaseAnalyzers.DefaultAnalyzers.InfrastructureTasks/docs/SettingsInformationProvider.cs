using System.ComponentModel;
using System.Reflection;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks.Docs;

internal static class SettingsInformationProvider
{
    public static IReadOnlyDictionary<string, List<PropertyDescriber>> GetPropertyDescriptionsByDiagnosticSettings()
    {
        return AssemblyProvider
            .GetAssemblies()
            .SelectMany(a => a
                .GetTypes()
                .Where(b => b.IsClass)
                .Where(b => !b.IsAbstract)
                .Where(b => b.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRawDiagnosticSettings<>)))
                .Select(b =>
                {
                    var settingsSource = b.GetCustomAttribute<SettingsSourceAttribute>();
                    var diagnosticId = settingsSource?.Name;
                    var kind = settingsSource?.Kind;
                    var propertyDescriptions = GetPropertyDescribers(b).Where(x => x.Description is not null).ToList();

                    return (DiagnosticId: diagnosticId, Kind: kind, PropertyDescriptions: propertyDescriptions);
                })
                .Where(b => b.DiagnosticId is not null && b is { Kind: not null, PropertyDescriptions.Count: > 0 })
            )
            .ToDictionary(a => a.DiagnosticId!, a => a.PropertyDescriptions, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<PropertyDescriber> GetPropertyDescribers(Type type)
        => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(a => new PropertyDescriber(a.Name, GetPropertyDescription(a)));

    private static string? GetPropertyDescription(PropertyInfo property)
        => property
            .GetCustomAttribute<DescriptionAttribute>()
            ?.Description;

    internal sealed record PropertyDescriber(string PropertyName, string? Description);
}
