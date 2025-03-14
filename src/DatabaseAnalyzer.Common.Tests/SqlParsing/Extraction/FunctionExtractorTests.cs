using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Testing;
using FluentAssertions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing.Extraction;

public sealed class FunctionExtractorTests
{
    [Fact]
    public void Extract()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            CREATE FUNCTION [dbo].[F1]
                            (
                                @Param1 INT
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
        functions.Should().HaveCount(1);
    }
}
