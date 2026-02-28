using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.Settings;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.Tests.Fakes;
using FluentAssertions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing.Extraction;

public sealed class DatabaseObjectExtractorTests
{
    private int _scriptNameSuffix;

    [Fact]
    public void Extract_ExtractVariousObjects()
    {
        const string code = """
                            USE [DB-1]
                            GO

                            CREATE TABLE [dbo].[Genre]
                            (
                                [Id]        [INT] NOT NULL,
                                [Name]      [NVARCHAR](50) NOT NULL,
                                CONSTRAINT  [PK_Genre] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                )
                            )
                            GO

                            CREATE TABLE [dbo].[Book]
                            (
                                [Id]        [INT] NOT NULL,
                                [GenreId]   [INT] NOT NULL,
                                [Name]      [NVARCHAR](250) NOT NULL,
                                CONSTRAINT  [PK_Book] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                ),
                                CONSTRAINT  [FK_Book_Genre] FOREIGN KEY ( [GenreId] ) REFERENCES [dbo].[Genre] ( [Id] )
                            )
                            GO

                            CREATE TABLE [dbo].[Book2]
                            (
                                [Id]        [INT] NOT NULL,
                                [GenreId]   [INT] NOT NULL,
                                CONSTRAINT  [PK_Book2] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                )
                            )
                            GO


                            ALTER TABLE [dbo].[Book2] WITH CHECK ADD CONSTRAINT [FK_Book2_Genre] FOREIGN KEY([GenreId])
                            REFERENCES [dbo].[Genre] ([Id])
                            GO

                            CREATE NONCLUSTERED INDEX [IX_Book_Name] ON [dbo].[Book]
                            (
                                [Name] ASC
                            )
                            GO

                            CREATE NONCLUSTERED INDEX [IX_Book_GenreId] ON [dbo].[Book]
                            (
                                [GenreId] ASC
                            )
                            GO

                            CREATE NONCLUSTERED INDEX [IX_Genre_Name] ON [dbo].[Genre]
                            (
                                [Name] ASC
                            )
                            GO

                            CREATE FUNCTION F1 ( @Param1 INT ) RETURNS INT AS
                            BEGIN
                                RETURN @Param1 * @Param1
                            END
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            @Param1 VARCHAR(MAX)
                            AS
                            BEGIN
                                PRINT @Param1
                            END
                            GO

                            CREATE SYNONYM dbo.MySynonym FOR MyServer.MyDatabase.MySchema.MyProc
                            GO

                            USE [DB-2]
                            GO

                            CREATE VIEW dbo.V1
                            AS
                                SELECT      Id, Name
                                FROM        [dbo].[Book]
                            GO

                            CREATE VIEW dbo.ViewWithUnion
                            AS
                                SELECT      Id, Name
                                FROM        [dbo].[Book]

                                UNION

                                SELECT      Id, Name
                                FROM        [dbo].[Book2]
                            GO

                            """;
        // arrange
        var sut = new DatabaseObjectExtractor(new FakeIssueReporter(), Aj9002Settings.Default);
        var script = ParseScript("DB-1", code);

        // act
        var databasesByName = sut.Extract([script], "dbo");

        // assert
        databasesByName.Should().NotBeNull();
        databasesByName.Should().HaveCount(2);

        var db = databasesByName["DB-1"];
        db.DatabaseName.Should().Be("DB-1");
        db.SchemasByName.Should().HaveCount(1);

        var dboSchema = db.SchemasByName["dbo"];
        dboSchema.DatabaseName.Should().Be("DB-1");
        dboSchema.ObjectName.Should().Be("dbo");
        dboSchema.FunctionsByName.Should().HaveCount(1);

        var function = dboSchema.FunctionsByName["F1"];
        function.DatabaseName.Should().Be("DB-1");
        function.SchemaName.Should().Be("dbo");
        function.ObjectName.Should().Be("F1");

        dboSchema.TablesByName.Should().HaveCount(3);

        var book = dboSchema.TablesByName["Book"];
        book.DatabaseName.Should().Be("DB-1");
        book.SchemaName.Should().Be("dbo");
        book.ObjectName.Should().Be("Book");
        book.Indices.Should().HaveCount(3); // Id, GenreId, Name
        book.ForeignKeys.Should().HaveCount(1);

        var genre = dboSchema.TablesByName["Genre"];
        genre.DatabaseName.Should().Be("DB-1");
        genre.SchemaName.Should().Be("dbo");
        genre.ObjectName.Should().Be("Genre");
        genre.Indices.Should().HaveCount(2); //  Id, Name
        genre.ForeignKeys.Should().BeEmpty();

        dboSchema.ProceduresByName.Should().HaveCount(1);
        var p1 = dboSchema.ProceduresByName["P1"];
        p1.DatabaseName.Should().Be("DB-1");
        p1.SchemaName.Should().Be("dbo");
        p1.ObjectName.Should().Be("P1");
        p1.Parameters.Should().HaveCount(1);

        dboSchema.SynonymsByName.Should().HaveCount(1);
        var synonym = dboSchema.SynonymsByName["MySynonym"];
        synonym.TargetServerName.Should().Be("MyServer");
        synonym.TargetDatabaseName.Should().Be("MyDatabase");
        synonym.TargetSchemaName.Should().Be("MySchema");
        synonym.TargetObjectName.Should().Be("MyProc");

        var view1 = databasesByName["DB-2"].SchemasByName["dbo"].ViewsByName["V1"];
        view1.ObjectName.Should().Be("V1");
        view1.Columns.Count.Should().Be(2);
        view1.Columns.Any(a => a.ObjectName.EqualsOrdinal("Id")).Should().BeTrue();
        view1.Columns.Any(a => a.ObjectName.EqualsOrdinal("Name")).Should().BeTrue();

        var viewWithUnion = databasesByName["DB-2"].SchemasByName["dbo"].ViewsByName["ViewWithUnion"];
        viewWithUnion.ObjectName.Should().Be("ViewWithUnion");
        viewWithUnion.Columns.Count.Should().Be(2);
        viewWithUnion.Columns.Any(a => a.ObjectName.EqualsOrdinal("Id")).Should().BeTrue();
        viewWithUnion.Columns.Any(a => a.ObjectName.EqualsOrdinal("Name")).Should().BeTrue();
    }

    [Fact]
    public void Extract_WhenDuplicateObjectCreation_ThenDiagnose()
    {
        const string code = """
                            USE [DB-1]
                            GO

                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            GO

                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL
                            )
                            """;
        // arrange
        var issueReporter = new FakeIssueReporter();
        var sut = new DatabaseObjectExtractor(issueReporter, Aj9002Settings.Default);
        var script = ParseScript("DB-1", code);

        // act
        var objects = sut.Extract([script], "dbo");

        // assert
        objects.Should().HaveCount(1);
        issueReporter.Issues.Should().HaveCount(1);
        var issue = issueReporter.Issues[0];
        issue.DiagnosticDefinition.Should().BeSameAs(WellKnownDiagnosticDefinitions.DuplicateObjectCreationStatement);
        issue.DatabaseName.Should().Be("DB-1");
        issue.ObjectName.Should().Be("DB-1.dbo.T1");
    }

    private ScriptModel ParseScript(string databaseName, string code)
    {
        var relativeScriptFilePath = @$".\\script_{_scriptNameSuffix++}";
        var parsedScript = code.ParseSqlScript();

        return new ScriptModel(
            databaseName,
            relativeScriptFilePath,
            code,
            parsedScript,
            parsedScript.CreateParentFragmentProvider(),
            [],
            []
        );
    }
}
