using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

public sealed class ParentFragmentProviderFactoryTests
{
    [Fact]
    [SuppressMessage("Design", "MA0051:Method is too long")]
    public void Build_WhenScriptIsProvided_ThenBuildWorkingProvider()
    {
        const string code = """
                            PRINT 1

                            GO

                            SELECT COALESCE(@a, 303)
                            """;
        // arrange
        var script = code.ParseSqlScript();

        // act
        var sut = ParentFragmentProviderFactory.Build(script);

        // assert parent
        sut.Should().NotBeNull();
        sut.Root.Should().BeSameAs(script);

        var batch2 = script.Batches[1];
        var selectStatement = (SelectStatement) batch2.Statements[0];
        var querySpecification = (QuerySpecification) selectStatement.QueryExpression;
        var selectElements = (SelectScalarExpression) querySpecification.SelectElements[0];
        var coalesceExpression = (CoalesceExpression) selectElements.Expression;
        var literal303 = coalesceExpression.Expressions[1];

        sut.GetParent(literal303).Should().BeSameAs(coalesceExpression);
        sut.GetParent(coalesceExpression).Should().BeSameAs(selectElements);
        sut.GetParent(selectElements).Should().BeSameAs(querySpecification);
        sut.GetParent(querySpecification).Should().BeSameAs(selectStatement);
        sut.GetParent(selectStatement).Should().BeSameAs(batch2);
        sut.GetParent(batch2).Should().BeSameAs(script);
        sut.GetParent(script).Should().BeNull();

        sut
            .GetParents(literal303)
            .Should()
            .BeEquivalentTo(new TSqlFragment[]
            {
                coalesceExpression,
                selectElements,
                querySpecification,
                selectStatement,
                batch2,
                script
            });
    }

    [Fact]
    public void Build_WhenScriptIsEmpty_ThenBuildProvider()
    {
        const string code = """

                            """;

        // arrange
        var script = code.ParseSqlScript();

        // act
        var sut = ParentFragmentProviderFactory.Build(script);

        // assert parent
        sut.Should().NotBeNull();
        sut.Root.Should().BeSameAs(script);
    }
}
