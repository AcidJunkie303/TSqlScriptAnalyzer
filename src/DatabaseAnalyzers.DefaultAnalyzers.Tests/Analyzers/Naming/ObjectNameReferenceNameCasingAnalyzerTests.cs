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

                                      CREATE FUNCTION Func1() RETURNS INT AS BEGIN RETURN 1 END
                                      GO

                                      CREATE SYNONYM aaa.SynonymProc FOR MyDb.dbo.Func1
                                      GO

                                      CREATE FUNCTION dbo.Func2() RETURNS TABLE AS RETURN ( SELECT 0 as C1 )

                                      """;

    private static readonly IAstService AstService = new AstService(AstServiceSettings.Default);

    [Fact]
    public void WithTable_WhenTableNameAndColumnNameAreIdentical_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, Name
                            FROM        Table1
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WithTable_WhenTableNameHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, Name
                            FROM        ▶️AJ5061💛script_0.sql💛💛table💛TABLE1💛Table1💛MyDb.dbo.Table1✅TABLE1◀️
                            """;
        VerifyLocal(code);
    }

    [Fact]
    public void WithTableColumn_WhenTableColumnHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id, ▶️AJ5061💛script_0.sql💛💛column💛NAME💛Name💛MyDb.dbo.Table1.Name✅NAME◀️
                            FROM        Table1
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WithScalarFunction__WhenFunctionNameHasSameCasing_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT Func1()
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WithScalarFunction_WhenFunctionNameHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT ▶️AJ5061💛script_0.sql💛💛function💛FuNc1💛Func1💛MyDb.dbo.Func1✅dbo.FuNc1()◀️
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WithTableValuedFunction__WhenFunctionNameHasSameCasing_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT * FROM Func2()
                            """;

        VerifyLocal(code);
    }

    [Fact]
    public void WithTableValuedFunction_WhenFunctionNameHasDifferentCasing_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT * FROM ▶️AJ5061💛script_0.sql💛💛function💛FuNc2💛Func2💛MyDb.dbo.Func2✅dbo.FuNc2()◀️
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
