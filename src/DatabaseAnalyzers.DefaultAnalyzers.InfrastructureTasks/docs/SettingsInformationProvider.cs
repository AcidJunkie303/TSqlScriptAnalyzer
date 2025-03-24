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
            .SelectMany(static a => a
                .GetTypes()
                .Where(static b => b.IsClass)
                .Where(static b => !b.IsAbstract)
                .Where(static b => b.GetInterfaces().Any(static x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRawDiagnosticSettings<>)))
                .Select(static b =>
                {
                    var settingsSource = b.GetCustomAttribute<SettingsSourceAttribute>();
                    var diagnosticId = settingsSource?.Name;
                    var kind = settingsSource?.Kind;
                    var propertyDescriptions = GetPropertyDescribers(b).Where(static x => x.Description is not null).ToList();

                    return (DiagnosticId: diagnosticId, Kind: kind, PropertyDescriptions: propertyDescriptions);
                })
                .Where(static b => b.DiagnosticId is not null && b is { Kind: not null, PropertyDescriptions.Count: > 0 })
            )
            .ToDictionary(static a => a.DiagnosticId!, static a => a.PropertyDescriptions, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<PropertyDescriber> GetPropertyDescribers(Type type)
        => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static a => new PropertyDescriber(a.Name, GetPropertyDescription(a)));

    private static string? GetPropertyDescription(PropertyInfo property)
        => property
            .GetCustomAttribute<DescriptionAttribute>()
            ?.Description;

    internal sealed record PropertyDescriber(string PropertyName, string? Description);
}
