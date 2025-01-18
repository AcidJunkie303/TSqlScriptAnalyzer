using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class ObjectInvocationWithoutSchemaNameAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectInvocationWithoutSchemaNameAnalyzer>(testOutputHelper)
{
    private static readonly Aj5049Settings IgnoredBuiltInFunctions = new Aj5049SettingsRaw
    {
        IgnoredObjectNamePatterns = ["ABS", "STRING_SPLIT"]
    }.ToSettings();

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
    public void WithUserCreatedScalarFunction_WhenInvokingWithSchemaName_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT dbo.MyScalarFunc() AS MyValue
                            """;
        Verify(Aj5049Settings.Default, code);
    }

    [Fact]
    public void WithUserCreatedScalarFunction_WhenInvokingWithoutSchemaName_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT ▶️AJ5049💛script_0.sql💛💛scalar function💛MyScalarFunc✅MyScalarFunc()◀️ AS MyValue
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

    [Fact]
    public void WithBuiltInScalarFunction_WhenNotIgnored_WhenInvokedWithoutSchema_ThenReport()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT ▶️AJ5049💛script_0.sql💛💛scalar function💛ABS✅ABS(-10)◀️ AS AbsoluteValue
                            """;
        Verify(Aj5049Settings.Default, code);
    }

    [Fact]
    public void WithBuiltInScalarFunction_WhenIgnored_WhenInvokedWithoutSchema_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT ABS(-10) AS AbsoluteValue
                            """;

        Verify(IgnoredBuiltInFunctions, code);
    }
}
