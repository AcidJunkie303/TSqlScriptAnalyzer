using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class ProcedureInvocationWithMissingParameterValuesAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ProcedureInvocationWithMissingParameterValuesAnalyzer>(testOutputHelper)
{
    private const string SharedCode = """
                                      USE MyDb
                                      GO

                                      CREATE PROCEDURE [dbo].[P1]
                                          @NotNullable                  INT,
                                          @Nullable                     INT NULL,
                                          @NotNullableWithDefaultValue  INT         = 303,
                                          @NullableWithDefaultValue     INT NULL    = 303
                                      AS
                                      BEGIN
                                          PRINT 303
                                      END
                                      """;

    [Fact]
    public void WhenAllParametersSpecified_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC P1 @Nullable = 1, @NotNullable = 1, @NotNullableWithDefaultValue = 1, @NullableWithDefaultValue = 1
                            """;
        Verify(Settings.AllRequired, code, SharedCode);
    }

    [Fact]
    public void WhenMandatoryParameterNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            -- no value provided for @NotNullable
                            EXEC ▶️AJ5062💛script_0.sql💛💛MyDb.dbo.P1💛@NotNullable✅P1 @Nullable = 1, @NotNullableWithDefaultValue = 1, @NullableWithDefaultValue = 1◀️
                            """;
        Verify(Settings.AllRequired, code, SharedCode);
    }

    [Fact]
    public void WhenParameterWithDefaultValueNotSpecified_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC ▶️AJ5062💛script_0.sql💛💛MyDb.dbo.P1💛@NotNullableWithDefaultValue✅P1 @Nullable = 1, @NotNullable = 1, @NullableWithDefaultValue = 1◀️
                            """;
        Verify(Settings.AllRequired, code, SharedCode);
    }

    [Fact]
    public void WhenNoParameters_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            EXEC P1 @Nullable = 1, @WithDefaultValue = 1, @NotNullable = 1, @NullableWithDefaultValue = 1
                            """;
        Verify(Settings.AllAllowed, code, SharedCode);
    }

    private static class Settings
    {
        public static Aj5062Settings AllRequired { get; } = Aj5062Settings.Default;
        public static Aj5062Settings AllAllowed { get; } = new(ValueRequiredForNullableParameters: false, ValueRequiredForParametersWithDefaultValue: false);
    }
}
