using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Runtime;

public sealed class OutputParameterNotAssignedOnAllExecutionPathsAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<OutputParameterNotAssignedOnAllExecutionPathsAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenNoOutputParameter_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                @Param1 INT
                            AS
                            BEGIN
                                PRINT @Param1
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenOutputParameterAssigned_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5016💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 INT OUTPUT◀️
                            AS
                            BEGIN
                                PRINT 303 -- No assignment here
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenOutputParameterAssignedInElseBranch_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5016💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 INT OUTPUT◀️
                            AS
                            BEGIN
                                IF (1=1)
                                BEGIN
                                    SET @Param1 = 303
                                END
                                ELSE
                                BEGIN
                                    PRINT 303 -- No assignment here
                                END
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenOutputParameterAssignedInBothBranches_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                @Param1 INT OUTPUT
                            AS
                            BEGIN
                                IF (1=1)
                                BEGIN
                                    SET @Param1 = 303
                                END ELSE BEGIN
                                    SET @Param1 = 303
                                END
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenOutputParameterAssignedSelectOnly_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5016💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 INT OUTPUT◀️
                            AS
                            BEGIN
                                SELECT      @Param1 = Column1
                                FROM        Table1
                                WHERE       Id = 303
                            END
                            """;

        Verify(code);
    }

    [Theory]
    [InlineData("@Param1 INT OUTPUT", "1=1")]
    [InlineData("▶️AJ5016💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 INT OUTPUT◀️", "0=1")]
    public void Theory_WhenOutputParameterAssignedInWhileLoopOnly(string parameter, string predicate)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE PROCEDURE [dbo].[P1]
                        {parameter}
                    AS
                    BEGIN
                        WHILE {predicate}
                        BEGIN
                            SET @Param1 = 303
                        END
                    END
                    """;

        Verify(code);
    }

    [Fact]
    public void WhenSetInTryBlockOnly_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5016💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 INT OUTPUT◀️
                            AS
                            BEGIN
                                BEGIN TRY
                                    SET @Param1 = 303
                                END TRY
                                BEGIN CATCH
                                    PRINT 303 -- No assignment here
                                END CATCH
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenSetInCatchBlockOnly_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                ▶️AJ5016💛script_0.sql💛MyDb.dbo.P1💛@Param1✅@Param1 INT OUTPUT◀️
                            AS
                            BEGIN
                                BEGIN TRY
                                    PRINT 303 -- No assignment here
                                END TRY
                                BEGIN CATCH
                                    SET @Param1 = 303
                                END CATCH
                            END
                            """;

        Verify(code);
    }

    [Fact]
    public void WhenSetInTryAndBlock_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                                @Param1 INT OUTPUT
                            AS
                            BEGIN
                                BEGIN TRY
                                    SET @Param1 = 303
                                END TRY
                                BEGIN CATCH
                                    SET @Param1 = 303
                                END CATCH
                            END
                            """;

        Verify(code);
    }
}
