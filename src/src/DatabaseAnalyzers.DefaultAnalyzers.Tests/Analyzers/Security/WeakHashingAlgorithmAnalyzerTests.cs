using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Security;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Security;

public sealed class WeakHashingAlgorithmAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<WeakHashingAlgorithmAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData("█AJ5020░script_0.sql░░MD2███MD2█")]
    [InlineData("█AJ5020░script_0.sql░░MD4███MD4█")]
    [InlineData("█AJ5020░script_0.sql░░MD5███MD5█")]
    [InlineData("█AJ5020░script_0.sql░░SHA███SHA█")]
    [InlineData("█AJ5020░script_0.sql░░SHA1███SHA1█")]
    [InlineData("SHA2_256")]
    [InlineData("SHA2_512")]
    public void VariousHashAlgorithms(string hashAlgorithm)
    {
        var code = $"""
                    USE MyDb
                    GO
                    SELECT HASHBYTES({hashAlgorithm}, 'tb-303')
                    """;
        Verify(code);
    }
}
