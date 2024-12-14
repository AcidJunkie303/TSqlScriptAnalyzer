using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using FluentAssertions;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.Extensions;

public sealed class SqlCreateAlterProcedureStatementBaseExtensionsTests
{
    [Fact]
    public void TryGetBody_ThenReturnBody()
    {
        const string sql = """
                           ALTER PROCEDURE [dbo].[P1]
                           AS
                           BEGIN
                               SELECT 1
                           END
                           """;
        // arrange
        var procedure = GetProcedure(sql);

        // act
        var body = procedure.TryGetBody();

        // assert
        body.Should().NotBeNull();
        body!.StartLocation.LineNumber.Should().Be(3);
    }

    private static SqlCreateAlterProcedureStatementBase GetProcedure(string sql)
        => sql
            .ParseSqlScript()
            .GetDescendantsOfType<SqlCreateAlterProcedureStatementBase>()
            .Single();
}
