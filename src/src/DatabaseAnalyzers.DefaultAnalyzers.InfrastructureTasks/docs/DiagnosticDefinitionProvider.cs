using System.Reflection;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks.Docs;

#pragma warning disable

internal static class DiagnosticDefinitionProvider
{
    public static IEnumerable<IDiagnosticDefinition> GetDefinitionsFromAssemblies()
        => AssemblyProvider
            .GetAssemblies()
            .SelectMany(GetDefinitionsFromAssembly);

    private static IEnumerable<IDiagnosticDefinition> GetDefinitionsFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var definition in GetDiagnosticDefinitionsFromType(type))
            {
                yield return definition;
            }
        }
    }

    private static IEnumerable<IDiagnosticDefinition> GetDiagnosticDefinitionsFromType(Type type)
    {
        var properties = type
            .GetProperties()
            .Where(a => a.CanRead)
            .Where(a => a.GetMethod?.IsPublic ?? false)
            .Where(a => a.GetMethod?.IsStatic ?? false)
            .Where(a => a.PropertyType.IsAssignableTo(typeof(IDiagnosticDefinition)));

        foreach (var property in properties)
        {
            var value = property.GetValue(null);
            if (value is IDiagnosticDefinition definition)
            {
                yield return definition;
            }
        }

        foreach (var nestedType in type.GetNestedTypes())
        {
            foreach (var nestedDefinition in GetDiagnosticDefinitionsFromType(nestedType))
            {
                yield return nestedDefinition;
            }
        }
    }
}
