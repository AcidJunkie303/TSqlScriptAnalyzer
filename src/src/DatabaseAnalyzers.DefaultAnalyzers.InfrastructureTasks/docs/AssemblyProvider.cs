using System.Reflection;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

namespace DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks.Docs;

internal static class AssemblyProvider
{
    public static IEnumerable<Assembly> GetAssemblies() =>
    [
        typeof(IDiagnosticDefinition).Assembly, // Contracts
        typeof(ExcessiveStringConcatenationAnalyzer).Assembly // DefaultAnalyzers
    ];
}
