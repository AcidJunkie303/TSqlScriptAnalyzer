using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using FluentAssertions;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.Extensions;

public sealed class SqlCreateAlterFunctionStatementBaseExtensionsTests
{
    [Fact]
    public void TryGetBody_WhenInlineTableValuedFunction_ThenReturnBody()
    {
        const string sql = """
                           CREATE FUNCTION dbo.F1 ()
                           RETURNS TABLE
                           AS
                           RETURN
                           (
                               SELECT 1 as C1
                           )
                           """;
        // arrange
        var function = GetFunction(sql);

        // act
        var body = function.TryGetBody();

        // assert
        body.Should().BeOfType<SqlInlineFunctionBodyDefinition>();
    }

    [Fact]
    public void TryGetBody_WhenMultiStatementTableValuedFunction_ThenReturnBody()
    {
        const string sql = """
                           CREATE FUNCTION F1 ()
                           RETURNS @Result TABLE
                           (
                           	   Column1 INT
                           )
                           AS
                           BEGIN
                               RETURN
                           END
                           """;
        // arrange
        var function = GetFunction(sql);

        // act
        var body = function.TryGetBody();

        // assert
        body.Should().BeOfType<SqlMultistatementFunctionBodyDefinition>();
    }

    [Fact]
    public void TryGetBody_WhenScalarFunction_ThenReturnBody()
    {
        const string sql = """
                           CREATE FUNCTION F1 ()
                           RETURNS INT
                           AS
                           BEGIN
                           	    RETURN 1
                           END
                           """;
        // arrange
        var function = GetFunction(sql);

        // act
        var body = function.TryGetBody();

        // assert
        body.Should().BeOfType<SqlMultistatementFunctionBodyDefinition>(); // weird but ok
    }

    private static SqlCreateAlterFunctionStatementBase GetFunction(string sql)
        => sql
            .ParseSqlScript()
            .GetDescendantsOfType<SqlCreateAlterFunctionStatementBase>()
            .Single();
}
