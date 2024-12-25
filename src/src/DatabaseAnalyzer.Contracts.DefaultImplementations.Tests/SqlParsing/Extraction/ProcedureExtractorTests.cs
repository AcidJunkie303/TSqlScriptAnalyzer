using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using FluentAssertions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing.Extraction;

public sealed class ProcedureExtractorTests
{
    [Fact]
    public void Extract()
    {
        const string defaultSchema = "aaa";
        const string code = """
                            USE MyDb
                            GO
                            CREATE PROCEDURE [dbo].[P1]
                                @Param1 VARCHAR(MAX)
                            AS
                            BEGIN
                                PRINT @Param1
                            END
                            """;

        // arrange
        var script = code.ParseSqlScript();
        var sut = new ProcedureExtractor(defaultSchema);

        // act
        var procedures = sut.Extract(script, "main.sql");

        // assert
        var procedure = procedures.Single();
        procedure.DatabaseName.Should().Be("MyDb");
        procedure.SchemaName.Should().Be("dbo");
        procedure.ProcedureName.Should().Be("P1");
        procedure.Parameters.Should().HaveCount(1);
    }
}
