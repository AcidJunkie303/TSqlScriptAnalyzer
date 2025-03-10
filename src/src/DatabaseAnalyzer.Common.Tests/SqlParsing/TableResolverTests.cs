using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.Tests.Fakes;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing;

// TODO: CTEs

public sealed class TableResolverTests : ResolverTestBase
{
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
        var sut = new TableResolver(issueReporter, script.ParsedScript, "script.sql", script.ParentFragmentProvider, "dbo");

        // act
        var table = sut.Resolve(tableReference);

        // assert
        table.Should().NotBeNull();
        table!.DatabaseName.Should().Be("MyDb");
        table.SchemaName.Should().Be("dbo");
        table.ObjectName.Should().Be("Table3");
    }
}
