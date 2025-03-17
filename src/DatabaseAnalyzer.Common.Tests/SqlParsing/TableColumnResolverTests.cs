using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.Tests.Fakes;
using DatabaseAnalyzer.Contracts.Services;
using DatabaseAnalyzer.Services;
using DatabaseAnalyzer.Services.Settings;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing;

// TODO: CTEs

public sealed class TableColumnResolverTests : ResolverTestBase
{
    private static readonly IAstService AstService = new AstService(AstServiceSettings.Default);

    public TableColumnResolverTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void WhenDelete_WithFrom_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE
                            FROM    Table1
                            WHERE   ▶️Value3◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
        column.ColumnName.Should().Be("Value3");
    }

    [Fact]
    public void WhenDelete_WithoutFrom_WithSingleTable_WithFilteringInWhereClause_WithoutAlias()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE  Table1
                            WHERE   ▶️Value3◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHERE   ▶️t1.Value1◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHERE   ▶️t3.Value3◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table3");
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
                            INNER   JOIN Table3 t3 on t3.Id = ▶️t2.id◀️
                            INNER   JOIN Table3 t4 on t4.Id = t3.id
                            WHERE   t3.Value3 = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var sut = new TableColumnResolver(new FakeIssueReporter(), AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table2");
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
                            WHERE   ▶️Value1◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHERE   ▶️t1.Value1◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHERE   ▶️t4.Value4◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table4");
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
                            WHERE   ▶️Id◀️ =
                            (
                                SELECT TOP 1 ID
                                FROM Table2
                                WHERE Value2 = 'Hello'
                            )
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            INNER   JOIN Table3 t4 on t4.Id = ▶️t3.ID◀️
                            WHERE   Value1 = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table3");
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
                            WHERE   ▶️Id◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table2");
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
                            WHERE   ▶️Id◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHERE   ▶️Id◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHERE   ▶️t2.Value2◀️ = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table2");
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
                            INNER   JOIN Table2 t2 ON t2.Id = ▶️t1.Id◀️
                            WHERE   t2.Value2 = 303
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
        column.ColumnName.Should().Be("Id");
    }

    [Fact]
    public void WhenSelectWithCte()
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
                            WHERE   ▶️c.Value1◀️ = 'World';
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("CTE");
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
                            ON          t1.Id = ▶️t2.Id◀️
                            WHEN MATCHED THEN
                                UPDATE SET t1.Value1 = t2.Value2
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (Id, Value1) VALUES (t2.Id, t2.Value2)
                            WHEN NOT MATCHED BY SOURCE THEN
                                DELETE;
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table2");
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
                            WHEN NOT MATCHED BY SOURCE AND ▶️t1.IsActive◀️ = 1 THEN
                                DELETE;
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
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
                            WHEN MATCHED AND ▶️t1.Status◀️ <> t2.Status THEN
                                UPDATE SET t1.Status = t2.Status, t1.LastUpdated = GETDATE()
                            WHEN NOT MATCHED BY TARGET THEN
                                INSERT (id, Name, Status, CreatedDate)
                                VALUES (t2.Id, t2.Name, t2.Status, GETDATE())
                            WHEN NOT MATCHED BY SOURCE AND t1.IsActive = 1 THEN
                                DELETE;
                            """;

        // arrange
        var (script, columnReference) = CreateScript<ColumnReferenceExpression>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableColumnResolver(issueReporter, AstService, script.ParsedScript, columnReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var column = sut.Resolve();

        // assert
        column.Should().NotBeNull();
        column.DatabaseName.Should().Be("MyDb");
        column.SchemaName.Should().Be("dbo");
        column.TableName.Should().Be("Table1");
        column.ColumnName.Should().Be("Status");
    }
}
