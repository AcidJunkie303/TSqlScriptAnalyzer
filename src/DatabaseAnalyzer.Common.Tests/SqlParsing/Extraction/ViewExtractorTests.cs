using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzer.Testing.Visualization;
using FluentAssertions;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing.Extraction;

public sealed class ViewExtractorTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ViewExtractorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Extract()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE VIEW View1
                            AS
                                SELECT
                                    Column1,
                                    'Hello' Column2,
                                    1.1 AS Column3,
                                    (SELECT TOP 1 Name FROM Whatever) AS Column4,
                                    CONVERT(NVARCHAR(10), Column1) Column5,
                                    CAST(Column1 AS BIGINT) AS Column6
                                FROM dbo.Table1
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new ViewExtractor("dbo");
        AstAndTokenVisualizer.Visualize(_testOutputHelper, script);

        // act
        var views = sut.Extract(script);

        // assert
        views.Should().HaveCount(1);
        var view = views[0];
        view.SchemaName.Should().Be("dbo");

        view.Columns.Should().HaveCount(6);
        view.Columns[0].ObjectName.Should().Be("Column1");
        view.Columns[1].ObjectName.Should().Be("Column2");
        view.Columns[2].ObjectName.Should().Be("Column3");
        view.Columns[3].ObjectName.Should().Be("Column4");
        view.Columns[4].ObjectName.Should().Be("Column5");
        view.Columns[5].ObjectName.Should().Be("Column6");
    }
}
