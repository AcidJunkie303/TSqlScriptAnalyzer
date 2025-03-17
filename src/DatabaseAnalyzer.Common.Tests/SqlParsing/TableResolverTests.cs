using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.Tests.Fakes;
using DatabaseAnalyzer.Contracts.Services;
using DatabaseAnalyzer.Services;
using DatabaseAnalyzer.Services.Settings;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing;

public sealed class TableResolverTests : ResolverTestBase
{
    private static readonly IAstService AstService = new AstService(AstServiceSettings.Default);

    public TableResolverTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
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
                            INNER   JOIN ▶️Table3 t3◀️ ON t3.Id = t2.Id
                            WHERE   t3.Value4 = 303
                            """;

        // arrange
        var (script, tableReference) = CreateScript<NamedTableReference>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableResolver(issueReporter, AstService, script.ParsedScript, tableReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var table = sut.Resolve();

        // assert
        table.Should().NotBeNull();
        table!.DatabaseName.Should().Be("MyDb");
        table.SchemaName.Should().Be("dbo");
        table.ObjectName.Should().Be("Table3");
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
                            FROM    ▶️CTE c◀️
                            WHERE   c.Value1 = 'World';
                            """;

        // arrange
        var (script, tableReference) = CreateScript<NamedTableReference>(code);
        var issueReporter = new FakeIssueReporter();
        var sut = new TableResolver(issueReporter, AstService, script.ParsedScript, tableReference, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var table = sut.Resolve();

        // assert
        table.Should().NotBeNull();
        table!.DatabaseName.Should().Be("MyDb");
        table.SchemaName.Should().Be("dbo");
        table.ObjectName.Should().Be("CTE");
        table.SourceType.Should().Be(TableSourceType.Cte);

        // TDOO: the column class should have an additional property called 'SourceType' which in this case would indicate that it is a CTE and not a table
    }
}
