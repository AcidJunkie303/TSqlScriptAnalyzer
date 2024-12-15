using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

// TODO: remove
#pragma warning disable

public sealed class SqlHelpersTests
{
    [Fact]
    public void GetParent_When___()
    {
        const string sql = """
                           SELECT
                               Column1,
                               COALESCE(Column2, Column3),
                               CAST(Column4 AS INT ),
                               ISNULL(Column5, '')
                           FROM dbo.T1
                           """;
        // arrange
        var script = sql.ParseSqlScript();
        var selectStatement = (SelectStatement)script.Batches[0].Statements[0];

        var queryExpressiopns = selectStatement.QueryExpression;

        // act
        var parents = SqlHelpers.GetParents(queryExpressiopns);
    }
}
