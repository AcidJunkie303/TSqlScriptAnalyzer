using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using FluentAssertions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing.Extraction;

public sealed class IndexExtractorTests
{
    [Fact]
    public void Extract_WhenSchemaIsNotSpecified_ThenResultContainsDefaultSchema()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            CREATE NONCLUSTERED INDEX [IX_T1_Value1] ON [dbo].[T1]
                            (
                            	[Value1] ASC
                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]

                            """;

        // arrange
        var script = code.ParseSqlScript();
        var sut = new IndexExtractor(defaultSchema);

        // act
        var indices = sut.Extract(script, defaultSchema);

        // assert
        indices.Should().HaveCount(1);
        indices[0].IndexName.Should().Be("IX_T1_Value1");
        indices[0].DatabaseName.Should().Be("MyDb");
        indices[0].TableSchemaName.Should().Be("dbo");
        indices[0].ColumnNames.Should().BeEquivalentTo("Value1");
        indices[0].IndexType.Should().Be(TableColumnIndexType.None);
        indices[0].IncludedColumnNames.Should().BeEmpty();
    }

    // TODO: add index creation outside 'CREATE TABLE' statement
    // TODO: add FK constraint creation outside 'CREATE TABLE' statement
}
