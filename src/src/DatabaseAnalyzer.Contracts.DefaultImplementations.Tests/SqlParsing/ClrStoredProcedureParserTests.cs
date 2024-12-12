using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using FluentAssertions;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

public class ClrStoredProcedureParserTests
{
    [Fact]
    public void WhenWithExecuteAs_ThenItIsIgnored()
    {
        const string sql = """
                           CREATE  PROCEDURE dbo.P1
                               @Param1 UNIQUEIDENTIFIER,
                               @Param2 NVARCHAR(100) OUTPUT
                           WITH EXECUTE AS OWNER
                           AS EXTERNAL NAME A.B.C;
                           """;

        var statement = Parse(sql);
        statement.Should().NotBeNull();
        statement!.CodeRegion.Should().BeEquivalentTo(CodeRegion.Create(1, 1, 5, 23));
        statement!.Parameters.Should().HaveCount(2);
        statement!.Parameters[0].Should().BeEquivalentTo(new ParameterInformation("@Param1", new DataType("UNIQUEIDENTIFIER", null, null), false));
        statement!.Parameters[1].Should().BeEquivalentTo(new ParameterInformation("@Param2", new DataType("NVARCHAR", 100, null), true));
    }

    [Fact]
    public void WhenParameters_ThenResultMustContainParameters()
    {
        const string sql = """
                           CREATE PROCEDURE dbo.P1
                               @Param1 UNIQUEIDENTIFIER,
                               @Param2 NVARCHAR(100) OUTPUT
                           AS EXTERNAL NAME A.B.C;
                           """;

        var statement = Parse(sql);
        statement.Should().NotBeNull();
        statement!.CodeRegion.Should().BeEquivalentTo(CodeRegion.Create(1, 1, 4, 23));
        statement!.Parameters.Should().HaveCount(2);
        statement!.Parameters[0].Should().BeEquivalentTo(new ParameterInformation("@Param1", new DataType("UNIQUEIDENTIFIER", null, null), false));
        statement!.Parameters[1].Should().BeEquivalentTo(new ParameterInformation("@Param2", new DataType("NVARCHAR", 100, null), true));
    }

    [Fact]
    public void WhenCreateOrAlter_ThenIsCreateOrAlterMustBeTrue()
    {
        const string sql = """
                           CREATE OR ALTER PROCEDURE dbo.P1
                           AS EXTERNAL NAME A.[B].C;
                           """;

        var statement = Parse(sql);
        statement.Should().NotBeNull();
        statement!.IsCreateOrAlter.Should().BeTrue();
    }

    [Fact]
    public void WhenNotCreateOrAlter_ThenIsCreateOrAlterMustBeFalse()
    {
        const string sql = """
                           CREATE PROCEDURE dbo.P1
                           AS EXTERNAL NAME A.[B].C;
                           """;

        var statement = Parse(sql);
        statement.Should().NotBeNull();
        statement!.IsCreateOrAlter.Should().BeFalse();
    }

    private static SqlCreateClrStoredProcedureStatement? Parse(string sql)
    {
        const string defaultSchema = "dbo";
        var script = sql.ParseSqlScript();
        var sqlNullStatement = script.GetDescendantsOfType<SqlNullStatement>().Single();
        return ClrStoredProcedureParser.TryParse(sqlNullStatement, defaultSchema);
    }
}
