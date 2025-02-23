using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Testing;
using FluentAssertions;
using FluentAssertions.Execution;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing.Extraction;

public sealed class TableExtractorTests
{
    [Fact]
    public void Extract_WhenSchemaIsNotSpecified_ThenResultContainsDefaultSchema()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            CREATE TABLE [T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor(defaultSchema);

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].SchemaName.Should().Be(defaultSchema);
    }

    [Fact]
    public void Extract_WhenSchemaIsSpecified_ThenResultContainsSpecifiedSchema()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE TABLE [xxx].[T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].SchemaName.Should().Be("xxx");
    }

    [Fact]
    public void Extract_WhenNoUseDatabaseStatement_ThenError()
    {
        const string code = """
                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var exception = Record.Exception(() => sut.Extract(script));

        // assert
        exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Extract_WhenUseDatabaseStatement_ThenResultContainsDatabaseName()
    {
        const string code = """
                            USE TB303

                            GO

                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].DatabaseName.Should().Be("TB303");
    }

    [Fact]
    public void Extract_AllColumnsAreExtracted()
    {
        const string code = """
                            USE MyDb

                            GO

                            CREATE TABLE [xxx].[T1]
                            (
                                [Id] [int] NOT NULL,
                                [Value1] [char](50) NOT NULL,
                                [Value2] [varchar](10) NULL,
                                [Value3] [varchar](max) NULL,
                                [Value4] [decimal](38,10) NULL
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].Columns.Should().HaveCount(5);
    }

    [Fact]
    public void Extract_WhenContainsNoPrimaryKey_ThenResultDoesNotContainIndex()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].Indices.Should().BeEmpty();
    }

    [Fact]
    public void Extract_WhenContainsPrimaryKey_ThenResultContainsIndex()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL,
                                CONSTRAINT [PK_T1] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                )
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].Indices.Should().NotBeNull();
        tables[0].Indices.Should().HaveCount(1);
        tables[0].Indices[0].IndexName.Should().Be("PK_T1");
        tables[0].Indices[0].IndexTypes.Should().Be(TableColumnIndexTypes.Clustered | TableColumnIndexTypes.PrimaryKey | TableColumnIndexTypes.Unique);
    }

    [Fact]
    public void Extract_WhenColumnContainsUniqueKeyword_ThenColumnHasIsUniqueSetToTrue_ThenIndexContainsUniqueIndex()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL,
                                [Email] [varchar](250) NOT NULL UNIQUE,
                                CONSTRAINT [PK_T1] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                )
                            )
                            """;

        // arrange
        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);
        tables[0].Indices.Should().NotBeNull();
        tables[0].Indices.Should().HaveCount(2);
        tables[0].Indices.Should().ContainEquivalentOf(
            new IndexInformation("MyDb", "dbo", "T1", "PK_T1", TableColumnIndexTypes.Clustered | TableColumnIndexTypes.PrimaryKey | TableColumnIndexTypes.Unique, "Id".ToFrozenSet(StringComparer.Ordinal), FrozenSet<string>.Empty, null!, "main.sql"),
            static options => options.Excluding(static x => x.CreationStatement));
        tables[0].Indices.Should().ContainEquivalentOf(
            new IndexInformation("MyDb", "dbo", "T1", null, TableColumnIndexTypes.Unique, "Email".ToFrozenSet(StringComparer.Ordinal), FrozenSet<string>.Empty, null!, "main.sql"),
            static options => options.Excluding(static x => x.CreationStatement));
    }

    [Fact]
    public void Extract()
    {
        const string code = """
                            USE MyDb
                            GO
                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL,
                                CONSTRAINT [FK_T1_T2] FOREIGN KEY( [OtherId]) REFERENCES [dbo].[T2] ([T2_Id])
                            )
                            """;

        // arrange
        AssertionOptions.FormattingOptions.MaxDepth = 2;
        AssertionScope.Current.FormattingOptions.MaxDepth = 2;

        var script = ScriptModelCreator.Create(code);
        var sut = new TableExtractor("dbo");

        // act
        var tables = sut.Extract(script);

        // assert
        tables.Should().HaveCount(1);

        var expectedForeignKey = new ForeignKeyConstraintInformation("MyDb", "dbo", "T1", "OtherId", "FK_T1_T2", "dbo", "T2", "T2_Id", null!, "main.sql");
        tables[0].ForeignKeys.Should().NotBeNullOrEmpty();
        tables[0].ForeignKeys.Should().HaveCount(1);
        tables[0].ForeignKeys.Should().ContainEquivalentOf(
            expectedForeignKey,
            static options => options
                .Excluding(static x => x.CreationStatement)
                .IgnoringCyclicReferences()
        );
    }
}
