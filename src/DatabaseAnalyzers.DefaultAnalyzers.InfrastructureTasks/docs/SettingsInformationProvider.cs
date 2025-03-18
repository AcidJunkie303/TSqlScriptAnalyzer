using System.ComponentModel;
using System.Reflection;
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
                .Where(b => b.IsAssignableTo(typeof(IDiagnosticSettings<>).MakeGenericType(b)))
                .Select(b => (Id: GetDiagnosticId(b), PropertyDescriber: GetPropertyDescribers(b)))
            )
            .Where(a => a.Id is not null)
            .ToDictionary(a => a.Id!, a => a.PropertyDescriber.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<PropertyDescriber> GetPropertyDescribers(Type type)
        => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(a => new PropertyDescriber(a.Name, GetPropertyDescription(a)));

    private static string? GetPropertyDescription(PropertyInfo property)
        => property
            .GetCustomAttribute<DescriptionAttribute>()
            ?.Description;

    private static string? GetDiagnosticId(Type type)
        => type
            .GetProperty("DiagnosticId", BindingFlags.Static | BindingFlags.Public)
            ?.GetValue(null)
            ?.ToString();

    internal sealed record PropertyDescriber(string PropertyName, string? Description);
}
