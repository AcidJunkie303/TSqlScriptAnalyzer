using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Testing;
using FluentAssertions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing.Extraction;

public sealed class FunctionExtractorTests
{
    [Fact]
    public void Extract_CheckBasicInformation()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            CREATE FUNCTION [dbo].[F1]
                            ()
                            RETURNS TABLE
                            AS
                            RETURN
                            (
                                SELECT 0 as C1
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new FunctionExtractor(defaultSchema);

        // act
        var functions = sut.Extract(script);

        // assert
        var function = functions.Single();
        function.DatabaseName.Should().Be("MyDb");
        function.SchemaName.Should().Be("dbo");
        function.ObjectName.Should().Be("F1");
        function.Parameters.Should().HaveCount(0);
        // assert
        functions.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("/* 00 */ @Param1 VARCHAR(MAX)                              ", false, false)]
    [InlineData("/* 01 */ @Param1 VARCHAR(MAX) NULL                         ", false, true)]
    [InlineData("/* 02 */ @Param1 VARCHAR(MAX) = N'MyDefaultValue'          ", true, false)]
    public void Theory_Extract_Parameters(string parameterLine, bool hasDefaultValue, bool isNullable)
    {
        const string defaultSchema = "aaa";
        var code = $"""
                    USE MyDb
                    GO

                    CREATE FUNCTION [dbo].[F1]
                    (
                        {parameterLine}
                    )
                    RETURNS TABLE
                    AS
                    RETURN
                    (
                        SELECT 0 as C1
                    )
                    """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new FunctionExtractor(defaultSchema);

        // act
        var functions = sut.Extract(script);

        // assert
        var function = functions.Single();
        function.Parameters.Should().HaveCount(1);

        var parameter = function.Parameters[0];
        parameter.HasDefaultValue.Should().Be(hasDefaultValue);
        parameter.IsNullable.Should().Be(isNullable);
        parameter.IsOutput.Should().BeFalse();
    }
}
