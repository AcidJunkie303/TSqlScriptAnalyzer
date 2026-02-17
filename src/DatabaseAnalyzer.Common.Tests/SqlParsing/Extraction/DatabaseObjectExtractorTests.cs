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
                            CREATE TABLE [dbo].[T2]
                            (
                               [Id] [int] NOT NULL,
                               [Name] [nvarchar](50) NOT NULL
                            )
                            GO
                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [int] NOT NULL,
                                [Status] [int] NOT NULL,
                                CONSTRAINT [PK_T1] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                ),
                                CONSTRAINT [FK_T1_T2] FOREIGN KEY( [OtherId]) REFERENCES [dbo].[T2] ([T2_Id])
                            )
                            GO

                            CREATE NONCLUSTERED INDEX [IX_T1_Status] ON [dbo].[T1]
                            (
                                [Status] ASC
                            )
                            GO

                            CREATE NONCLUSTERED INDEX [IX_T2_Name] ON [dbo].[T2]
                            (
                                [Name] ASC
                            )
                            GO

                            CREATE FUNCTION F1
                            ( @Param1 INT ) RETURNS INT AS
                            BEGIN RETURN @Param1 * @Param1 END
                            GO

                            CREATE PROCEDURE [dbo].[P1]
                            @Param1 VARCHAR(MAX)
                            AS
                            BEGIN PRINT @Param1 END

                            CREATE SYNONYM dbo.MySynonym FOR MyServer.MyDatabase.MySchema.MyProc
                            GO

                            USE [DB-2]
                            GO

                            CREATE VIEW dbo.V1
                            AS
                                SELECT Id, Name
                                FROM [dbo].[T2]
                            GO

                            """;
        // arrange
        var sut = new DatabaseObjectExtractor(new FakeIssueReporter(), Aj9002Settings.Default);
        var script = ParseScript("DB-1", code);

        // act
        var objects = sut.Extract([script], "dbo");

        // assert
        objects.Should().NotBeNull();
        objects.Should().HaveCount(2);

        var db = objects["DB-1"];
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

        dboSchema.TablesByName.Should().HaveCount(2);

        var t1 = dboSchema.TablesByName["T1"];
        t1.DatabaseName.Should().Be("DB-1");
        t1.SchemaName.Should().Be("dbo");
        t1.ObjectName.Should().Be("T1");
        t1.Indices.Should().HaveCount(2); // Id & Status
        t1.ForeignKeys.Should().HaveCount(1);

        var t2 = dboSchema.TablesByName["T2"];
        t2.DatabaseName.Should().Be("DB-1");
        t2.SchemaName.Should().Be("dbo");
        t2.ObjectName.Should().Be("T2");
        t2.Indices.Should().HaveCount(1); // Name

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

        objects["DB-2"].SchemasByName["dbo"].ViewsByName["V1"].ObjectName.Should().Be("V1");
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
