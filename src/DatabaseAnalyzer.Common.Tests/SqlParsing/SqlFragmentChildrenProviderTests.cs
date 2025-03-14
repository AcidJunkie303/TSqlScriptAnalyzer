using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Tests.SqlParsing;

public sealed class SqlFragmentChildrenProviderTests
{
    [Fact]
    public void GetChildren_WhenNoGenericType_WhenNotRecursive_ThenGetDirectChildren()
    {
        const string code = """

                            PRINT 303
                            SELECT COALESCE(@a, 303)

                            """;
        // arrange
        var batch = code.ParseSqlScript().Batches[0];

        // act
        var children = SqlFragmentChildrenProvider.GetChildren(batch);
        children.Should().HaveCount(2);
        children[0].Should().BeOfType<PrintStatement>();
        children[1].Should().BeOfType<SelectStatement>();
    }

    [Fact]
    public void GetChildren_WhenNoGenericType_WhenRecursive_ThenGetDirectChildren()
    {
        const string code = """

                            PRINT 303
                            SELECT COALESCE(@a, 303)

                            """;
        // arrange
        var batch = code.ParseSqlScript().Batches[0];

        // act
        var children = SqlFragmentChildrenProvider.GetChildren(batch, recursive: true);
        children.Should().HaveCount(8);
    }

    [Fact]
    public void GetChildren_WhenGenericType_WhenNotRecursive_ThenGetDirectChildren()
    {
        const string code = """

                            PRINT 303
                            SELECT COALESCE(@a, 303)

                            """;
        // arrange
        var batch = code.ParseSqlScript().Batches[0];

        // act
        var children = batch.GetChildren<SelectStatement>(recursive: true);
        children.Should().HaveCount(1);
    }

    [Fact]
    public void GetChildren_WhenGenericType_WhenRecursive_ThenGetDirectChildren()
    {
        const string code = """

                            PRINT 303
                            SELECT COALESCE(@a, 303)

                            """;
        // arrange
        var batch = code.ParseSqlScript().Batches[0];

        // act
        var children = batch.GetChildren<IntegerLiteral>(recursive: true);
        children.Should().HaveCount(2);
    }
}
