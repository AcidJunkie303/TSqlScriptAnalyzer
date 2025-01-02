using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using FluentAssertions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

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
