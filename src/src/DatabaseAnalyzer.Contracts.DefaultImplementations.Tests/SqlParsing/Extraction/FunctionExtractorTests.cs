using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using FluentAssertions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing.Extraction;

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
        var script = code.ParseSqlScript();
        var sut = new FunctionExtractor(defaultSchema);

        // act
        var functions = sut.Extract(script);

        // assert
        functions.Should().HaveCount(1);
    }
}
