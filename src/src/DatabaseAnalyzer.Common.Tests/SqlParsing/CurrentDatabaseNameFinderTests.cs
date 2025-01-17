using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Contracts;
using FluentAssertions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing;

public sealed class CurrentDatabaseNameFinderTests
{
    [Theory]
    [InlineData(1, 1, "MyDB")]
    [InlineData(1, 8, "MyDB")]
    [InlineData(2, 1, "MyDB")]
    [InlineData(2, 3, "MyDB")]
    [InlineData(3, 1, "OtherDb")]
    public void Test(int line, int column, string expectedDatabaseName)
    {
        const string code = """
                            USE MyDB
                            GO
                            USE OtherDb

                            """;

        // arrange
        var script = code.ParseSqlScript();

        // act
        var databaseName = CurrentDatabaseNameFinder.TryFindCurrentDatabaseNameAtLocation(script, CodeLocation.Create(line, column));

        // assert
        databaseName.Should().Be(expectedDatabaseName);
    }
}
