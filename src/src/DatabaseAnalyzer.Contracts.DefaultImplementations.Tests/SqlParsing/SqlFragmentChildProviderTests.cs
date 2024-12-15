using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

public sealed class SqlFragmentChildProviderTests
{
    [Fact]
    public void GetChildren_WhenNoGenericType_WhenNotRecursive_ThenGetDirectChildren()
    {
        const string sql = """

                           PRINT 303
                           SELECT COALESCE(@a, 303)

                           """;
        // arrange
        var batch = sql.ParseSqlScript().Batches[0];

        // act
        var children = SqlFragmentChildProvider.GetChildren(batch);
        children.Should().HaveCount(2);
        children[0].Should().BeOfType<PrintStatement>();
        children[1].Should().BeOfType<SelectStatement>();
    }

    [Fact]
    public void GetChildren_WhenNoGenericType_WhenRecursive_ThenGetDirectChildren()
    {
        const string sql = """

                           PRINT 303
                           SELECT COALESCE(@a, 303)

                           """;
        // arrange
        var batch = sql.ParseSqlScript().Batches[0];

        // act
        var children = SqlFragmentChildProvider.GetChildren(batch, true);
        children.Should().HaveCount(8);
    }

    [Fact]
    public void GetChildren_WhenGenericType_WhenNotRecursive_ThenGetDirectChildren()
    {
        const string sql = """

                           PRINT 303
                           SELECT COALESCE(@a, 303)

                           """;
        // arrange
        var batch = sql.ParseSqlScript().Batches[0];

        // act
        var children = SqlFragmentChildProvider.GetChildren<SelectStatement>(batch, true);
        children.Should().HaveCount(1);
    }

    [Fact]
    public void GetChildren_WhenGenericType_WhenRecursive_ThenGetDirectChildren()
    {
        const string sql = """

                           PRINT 303
                           SELECT COALESCE(@a, 303)

                           """;
        // arrange
        var batch = sql.ParseSqlScript().Batches[0];

        // act
        var children = SqlFragmentChildProvider.GetChildren<IntegerLiteral>(batch, true);
        children.Should().HaveCount(2);
    }
}
