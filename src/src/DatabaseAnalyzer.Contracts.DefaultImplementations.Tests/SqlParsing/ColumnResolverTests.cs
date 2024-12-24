using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.Fakes;
using DatabaseAnalyzer.Testing;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

// TODO:
// do CTEs

public sealed class ColumnResolverTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void WhenDelete_WithFrom_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE
                            FROM    Table1
                            WHERE   █Value3█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Value3");
    }

    [Fact]
    public void WhenDelete_WithoutFrom_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE  Table1
                            WHERE   █Value3█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Value3");
    }

    [Fact]
    public void WhenDelete_WithSingleTable_WithFilteringInWhereClause_WithAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE  t1
                            FROM    Table1 t1
                            WHERE   █t1.Value1█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Value1");
    }

    [Fact]
    public void WhenDelete_WithJoins_WithFilteringInWhereClause_WithAliases()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE  t1
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 on t2.Id = t1.id
                            INNER   JOIN Table3 t3 on t3.Id = t2.id
                            WHERE   █t3.Value3█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table3");
        column.ColumnName.Should().Be("Value3");
    }

    [Fact]
    public void WhenDelete_WithJoins_WithFilteringInJoinCondition_WithAliases()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE  t1
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 on t2.Id = t1.id
                            INNER   JOIN Table3 t3 on t3.Id = █t2.id█
                            INNER   JOIN Table3 t4 on t4.Id = t3.id
                            WHERE   t3.Value3 = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table2");
        column.ColumnName.Should().Be("id");
    }

    [Fact]
    public void WhenSelect_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    Table1
                            WHERE   █Value1█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Value1");
    }

    [Fact]
    public void WhenSelect_WithSingleTable_WithFilteringInWhereClause_WithAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    Table1  t1
                            WHERE   █t1.Value1█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Value1");
    }

    [Fact]
    public void WhenSelect_WithJoin_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 on t2.Id = t1.id
                            INNER   JOIN Table3 t3 on t3.Id = t2.id
                            INNER   JOIN Table4 t4 on t4.Id = t3.ID
                            WHERE   █t4.Value4█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table4");
        column.ColumnName.Should().Be("Value4");
    }

    [Fact]
    public void WhenSelect_WithNestedJoin_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    Table1
                            WHERE   █Id█ =
                            (
                                SELECT TOP 1 ID
                                FROM Table2
                                WHERE Value2 = 'Hello'
                            )
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenSelect_WithJoin_WithFilteringInJoinCondition_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT  *
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 on t2.Id = t1.id
                            INNER   JOIN Table3 t3 on t3.Id = t2.id
                            INNER   JOIN Table3 t4 on t4.Id = █t3.ID█
                            WHERE   Value1 = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table3");
        column.ColumnName.Should().Be("ID");
    }

    [Fact]
    public void WhenInsertSelect_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            INSERT  INTO Table1
                            SELECT  *
                            FROM    Table2
                            WHERE   █Id█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table2");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenUpdate_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE  Table1
                            SET     Value1 = 'Hello'
                            WHERE   █Id█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenUpdate_WithSingleTable_WithFilteringInWhereClause_WithAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE  t1
                            SET     t1.Value1 = 'Hello'
                            FROM    Table1 t1
                            WHERE   █Id█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenUpdate_WithJoin_WithFilteringInWhereClause_WithAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE  t1
                            SET     t1.Value1 = 'Hello'
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 ON t2.Id = t1.Id
                            WHERE   █t2.Value2█ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table2");
        column.ColumnName.Should().Be("Value2");
    }

    [Fact]
    public void WhenUpdate_WithJoin_WithFilteringInJoinCondition_WithAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            UPDATE  t1
                            SET     t1.Value1 = 'Hello'
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 ON t2.Id = █t1.Id█
                            WHERE   t2.Value2 = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenSelectWithCte_()
    {
        const string code = """
                            USE MyDb
                            GO

                            ;WITH CTE AS
                            (
                                SELECT  *
                                FROM    Table1
                                WHERE   Value1 = 'Hello'
                            )
                            SELECT  *
                            FROM    CTE c
                            WHERE   █c.Value1█ = 'World';
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("CTE");
        column.ColumnName.Should().Be("Value1");

        // TDOO: the column class should have an additional property called 'SourceType' which in this case would indicate that it is a CTE and not a table
    }

    [Fact]
    public void WhenMerge_01()
    {
        const string code = """
                            USE MyDb
                            GO

                            MERGE       INTO Table1 AS t1
                            USING       Table2      AS t2
                            ON          t1.Id = █t2.Id█
                            WHEN MATCHED THEN
                                UPDATE SET t1.Value1 = t2.Value2
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (Id, Value1) VALUES (t2.Id, t2.Value2)
                            WHEN NOT MATCHED BY SOURCE THEN
                                DELETE;
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table2");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenMerge_WithFilteringInMergeActionClause()
    {
        const string code = """
                            USE MyDb
                            GO

                            MERGE INTO  Table1 AS t1
                            USING       Table2 AS t2
                            ON          t1.Id = t2.Id
                            WHEN MATCHED AND t1.Status <> t2.Status THEN
                                UPDATE SET t1.Status = t2.Status, t1.LastUpdated = GETDATE()
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (id, Name, Status, CreatedDate)
                                VALUES (t2.Id, t2.Name, t2.Status, GETDATE())
                            WHEN NOT MATCHED BY SOURCE AND █t1.IsActive█ = 1 THEN
                                DELETE;
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("IsActive");
    }

    [Fact]
    public void WhenMerge_WithFilteringInMergeActionClause2()
    {
        const string code = """
                            USE MyDb
                            GO

                            MERGE INTO  Table1 AS t1
                            USING       Table2 AS t2
                            ON          t1.Id = t2.Id
                            WHEN MATCHED AND █t1.Status█ <> t2.Status THEN
                                UPDATE SET t1.Status = t2.Status, t1.LastUpdated = GETDATE()
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (id, Name, Status, CreatedDate)
                                VALUES (t2.Id, t2.Name, t2.Status, GETDATE())
                            WHEN NOT MATCHED BY SOURCE AND t1.IsActive = 1 THEN
                                DELETE;
                            """;

        // arrange
        var (script, columnReference) = CreateScript(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new ColumnResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve(columnReference);

        // assert
        column.Should().NotBeNull();
        column!.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.ObjectName.Should().Be("Table1");
        column.ColumnName.Should().Be("Status");
    }

    private (ScriptModel ScriptModel, ColumnReferenceExpression ColumnReference) CreateScript(string code)
    {
        var (markupFreeCode, codeRegion) = RemoveMarkupAndGetCodeRegion(code);
        var parsedScript = markupFreeCode.ParseSqlScript();

        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine("Syntax Tree:");
        testOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(parsedScript));

        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine("Tokens:");
        testOutputHelper.WriteLine(TokenVisualizer.Visualize(parsedScript));

        var columnReference = parsedScript
            .GetChildren<ColumnReferenceExpression>(recursive: true)
            .Single(a => a.GetCodeRegion() == codeRegion);

        var scriptModel = new ScriptModel(
            "database-1",
            "script.sql",
            code,
            parsedScript,
            parsedScript.CreateParentFragmentProvider(),
            [],
            []
        );

        return (scriptModel, columnReference);

        static (string MarkupFreeSql, CodeRegion CodeRegion) RemoveMarkupAndGetCodeRegion(string code)
        {
            var startIndex = code.IndexOf('█', StringComparison.Ordinal); // string comparision required because of analyzer
            var endIndex = code.IndexOf('█', startIndex + 1); // no overload for string comparision :(
            if ((startIndex < 0) || (endIndex < 0))
            {
                throw new ArgumentException("The SQL code does not contain contain two markup markers '█'", nameof(code));
            }

            // currently, this only works if start and end marker are on the same line
            var (startLineNumber, startColumnNumber) = code.GetLineAndColumnNumber(startIndex);
            var (endLineNumber, endColumnNumber) = code.GetLineAndColumnNumber(endIndex - 1); // -1 because we'll remove the start and end marker
            var codeRegion = CodeRegion.Create(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);
            var markupFreeSql = code.Remove(endIndex, 1).Remove(startIndex, 1);

            return (markupFreeSql, codeRegion);
        }
    }
}
