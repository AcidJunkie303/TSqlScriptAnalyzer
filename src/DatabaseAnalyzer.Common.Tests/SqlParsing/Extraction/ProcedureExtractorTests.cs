using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Testing;
using FluentAssertions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing.Extraction;

public sealed class ProcedureExtractorTests
{
    [Fact]
    public void Extract_CheckBasicInformation()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            CREATE PROCEDURE [dbo].[P1]
                            AS
                            BEGIN
                                PRINT @Param1
                            END
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new ProcedureExtractor(defaultSchema);

        // act
        var procedures = sut.Extract(script);

        // assert
        var procedure = procedures.Single();
        procedure.DatabaseName.Should().Be("MyDb");
        procedure.SchemaName.Should().Be("dbo");
        procedure.ObjectName.Should().Be("P1");
        procedure.Parameters.Should().HaveCount(0);
    }

    [Theory]
    [InlineData("/* 00 */ @Param1 VARCHAR(MAX)                              ", false, false, false)]
    [InlineData("/* 01 */ @Param1 VARCHAR(MAX) NULL                         ", false, false, true)]
    [InlineData("/* 02 */ @Param1 VARCHAR(MAX) = N'MyDefaultValue'          ", false, true, false)]
    [InlineData("/* 03 */ @Param1 VARCHAR(MAX) = N'MyDefaultValue' OUTPUT   ", true, true, false)]
    [InlineData("/* 04 */ @Param1 VARCHAR(MAX) NULL OUTPUT                  ", true, false, true)]
    public void Theory_Extract_Parameters(string parameterLine, bool isOutput, bool hasDefaultValue, bool isNullable)
    {
        const string defaultSchema = "aaa";
        var code = $"""
                    USE MyDb
                    GO

                    CREATE PROCEDURE [dbo].[P1]
                        {parameterLine}
                    AS
                    BEGIN
                        PRINT 303
                    END
                    """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new ProcedureExtractor(defaultSchema);

        // act
        var procedures = sut.Extract(script);

        // assert
        var procedure = procedures.Single();
        procedure.Parameters.Should().HaveCount(1);

        var parameter = procedure.Parameters[0];
        parameter.HasDefaultValue.Should().Be(hasDefaultValue);
        parameter.IsNullable.Should().Be(isNullable);
        parameter.IsOutput.Should().Be(isOutput);
    }
}
