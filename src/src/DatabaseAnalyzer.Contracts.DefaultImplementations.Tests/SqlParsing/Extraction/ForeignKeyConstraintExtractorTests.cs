using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Testing;
using FluentAssertions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing.Extraction;

public sealed class ForeignKeyConstraintExtractorTests
{
    [Fact]
    public void Extract()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            ALTER TABLE [aaa].[T1]  WITH CHECK ADD  CONSTRAINT [FK_T1_T2] FOREIGN KEY([OtherId])
                            REFERENCES [bbb].[T2] ([Id])
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new ForeignKeyConstraintExtractor(defaultSchema);

        // act
        var fkConstraints = sut.Extract(script);

        // assert
        fkConstraints.Should().NotBeNull();
        fkConstraints[0].ObjectName.Should().Be("FK_T1_T2");
        fkConstraints[0].DatabaseName.Should().Be("MyDb");
        fkConstraints[0].SchemaName.Should().Be("aaa");
        fkConstraints[0].TableName.Should().Be("T1");
        fkConstraints[0].ColumnName.Should().Be("OtherId");
        fkConstraints[0].ReferencedTableSchemaName.Should().Be("bbb");
        fkConstraints[0].ReferencedTableName.Should().Be("T2");
        fkConstraints[0].ReferencedTableColumnName.Should().Be("Id");
    }
}
