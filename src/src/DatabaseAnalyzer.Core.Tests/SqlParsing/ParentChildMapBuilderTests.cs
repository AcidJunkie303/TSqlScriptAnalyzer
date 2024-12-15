using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using FluentAssertions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Core.Tests.SqlParsing;

public sealed class ParentChildMapBuilderTests
{
    [Fact]
    public void Test()
    {
        const string sql = """

                           PRINT 1
                           IF (@a = 'b')
                           BEGIN
                               PRINT 22
                           END
                           PRINT 3
                           PRINT 4

                           GO

                           CREATE OR ALTER VIEW dbo.V1
                           AS
                               SELECT
                                   Column1,
                                   COALESCE(Column2, Column3),
                                   CAST(Column4 AS INT ),
                                   ISNULL(Column5, '')
                               FROM dbo.T1
                           """;

        // arrange
        var script = sql.ParseSqlScript();

        // act
        var map = ParentChildMapBuilder.Build(script);

        // assert parent
        map.Should().NotBeNull();
        map.Root.Should().BeOfType<TSqlScript>();
        var column5Identifier = map.ParentByChild
            .Single(a => a.Key is Identifier identifer && identifer.Value.EqualsOrdinal("Column5"))
            .Value;

        column5Identifier.Should().NotBeNull();
        var column5Reference = (ColumnReferenceExpression)map.ParentByChild[column5Identifier!]!;
        column5Reference.Should().NotBeNull();

        var functionCall = (FunctionCall)map.ParentByChild[column5Reference]!;
        functionCall.Should().NotBeNull();

        var selectScalarExpression = (SelectScalarExpression)map.ParentByChild[functionCall]!;
        selectScalarExpression.Should().NotBeNull();

        var querySpecification = (QuerySpecification)map.ParentByChild[selectScalarExpression]!;
        querySpecification.Should().NotBeNull();

        var selectStatement = (SelectStatement)map.ParentByChild[querySpecification]!;
        selectStatement.Should().NotBeNull();

        var createOrAlterViewStatement = (CreateOrAlterViewStatement)map.ParentByChild[selectStatement]!;
        createOrAlterViewStatement.Should().NotBeNull();

        var batch = (TSqlBatch)map.ParentByChild[createOrAlterViewStatement]!;
        batch.Should().NotBeNull();

        var sqlScript = (TSqlScript)map.ParentByChild[batch]!;
        sqlScript.Should().NotBeNull();

        map.ParentByChild[sqlScript].Should().BeNull();

        // assert children
        var children = map.ChildrenByParent[querySpecification];
        children.Should().HaveCount(5);
        children[0].Should().BeOfType<SelectScalarExpression>();
        children[1].Should().BeOfType<SelectScalarExpression>();
        children[2].Should().BeOfType<SelectScalarExpression>();
        children[3].Should().BeOfType<SelectScalarExpression>();
        children[4].Should().BeOfType<FromClause>();
    }
}
