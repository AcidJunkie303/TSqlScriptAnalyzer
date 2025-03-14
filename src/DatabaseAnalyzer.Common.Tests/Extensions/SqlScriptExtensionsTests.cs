using DatabaseAnalyzer.Common.Extensions;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Tests.Extensions;

public sealed class SqlScriptExtensionsTests
{
    [Fact]
    public void GetTopLevelDescendantsOfType_WhenDescendantsContainsSameType_ThenOnlyTopLevelDescendantIsReturned()
    {
        const string code = """
                            SET @result = 1 + 2 + 3 + 4;
                            """;

        // arrange
        var script = code.ParseSqlScript();
        var parentFragmentProvider = script.CreateParentFragmentProvider();

        // act
        var descendants = script.GetTopLevelDescendantsOfType<BinaryExpression>(parentFragmentProvider).ToList();

        // assert
        descendants.Should().NotBeNull();
        descendants.Should().HaveCount(1);
        descendants[0].FirstExpression.GetSql().Should().Be("1 + 2 + 3");
    }

    [Fact]
    public void GetTopLevelDescendantsOfType_WithMultiple_WhenDescendantsContainsSameType_ThenOnlyTopLevelDescendantIsReturned()
    {
        const string code = """
                            SET @result = 1 + 2 + 3 + 4;
                            SET @result = 5 + 6 + 7 + 8;
                            """;

        // arrange
        var script = code.ParseSqlScript();
        var parentFragmentProvider = script.CreateParentFragmentProvider();

        // act
        var descendants = script.GetTopLevelDescendantsOfType<BinaryExpression>(parentFragmentProvider).ToList();

        // assert
        descendants.Should().NotBeNull();
        descendants.Should().HaveCount(2);
        descendants[0].FirstExpression.GetSql().Should().Be("1 + 2 + 3");
        descendants[1].FirstExpression.GetSql().Should().Be("5 + 6 + 7");
    }
}
