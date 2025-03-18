using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Different test data")]
public sealed class ObjectNameReferenceNameCasingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectNameReferenceNameCasingAnalyzer>(testOutputHelper)
{
    private const string SharedCode = """
                                      USE MyDb
                                      GO

                                      CREATE TABLE Table1
                                      (
                                          Id          INT,
                                          Name        INT
                                      )
                                      GO

                                      CREATE FUNCTION Function1()
                                      RETURNS INT
                                      AS
                                      BEGIN
                                          RETURN 1
                                      END
                                      """;

    private static readonly IAstService AstService = new AstService(AstServiceSettings.Default);

    [Fact]
    public void WhenTableNameAndColumnNameAreIdentical_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, Name
                            FROM        Table1

                            SELECT      dbo.Function1()
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WhenTableNameHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, Name
                            FROM        TABLE1
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WhenTableColumnHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, NAME
                            FROM        Table1
                            """;

        VerifyLocal(code);
    }

    private void VerifyLocal(string code)
    {
        var tester = GetDefaultTesterBuilder(code, SharedCode)
            .WithService<IAstService>(AstService)
            .Build();

        Verify(tester);
    }
}
