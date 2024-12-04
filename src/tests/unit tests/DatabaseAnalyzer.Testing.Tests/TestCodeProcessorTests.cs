using DatabaseAnalyzer.Contracts;
using Xunit;

namespace DatabaseAnalyzer.Testing.Tests;

public sealed class TestCodeProcessorTests
{
    private static DiagnosticDefinitionRegistry DiagnosticDefinitionRegistry = new(new DiagnosticDefinition);

    [Fact]
    public void Test1()
    {
    }
}

public static class DiagnosticDefinitions
{
    private sealed class TestDiagnostic1 : IDiagnosticDefinition
    {

    }
}
