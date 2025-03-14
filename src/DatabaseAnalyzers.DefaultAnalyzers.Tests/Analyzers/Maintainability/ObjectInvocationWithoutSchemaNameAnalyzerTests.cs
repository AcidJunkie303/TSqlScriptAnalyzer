using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class ObjectInvocationWithoutSchemaNameAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectInvocationWithoutSchemaNameAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WithUserCreatedProcedure_WhenInvokingWithSchemaName_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC dbo.MyProcedure @Id = 303
                            """;
        Verify(Aj5049Settings.Default, code);
    }

    [Fact]
    public void WithUserCreatedProcedure_WhenInvokingWithoutSchemaName_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC ▶️AJ5049💛script_0.sql💛💛procedure💛MyProcedure✅MyProcedure◀️ @Id = 303
                            """;
        Verify(Aj5049Settings.Default, code);
    }

    [Fact]
    public void WithUserCreatedTableValuedFunction_WhenInvokingWithSchemaName_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT * FROM dbo.MyTableValuedFunction()
                            """;
        Verify(Aj5049Settings.Default, code);
    }

    [Fact]
    public void WithUserCreatedTableValuedFunction_WhenInvokingWithoutSchemaName_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT * FROM  ▶️AJ5049💛script_0.sql💛💛table valued function💛MyTableValuedFunction✅MyTableValuedFunction()◀️
                            """;
        Verify(Aj5049Settings.Default, code);
    }
}
