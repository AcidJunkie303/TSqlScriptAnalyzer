using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Testing;
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
        var script = ScriptModelCreator.Create(code);
        var sut = new ProcedureExtractor(defaultSchema);

        // act
        var procedures = sut.Extract(script);

        // assert
        var procedure = procedures.Single();
        procedure.DatabaseName.Should().Be("MyDb");
        procedure.SchemaName.Should().Be("dbo");
        procedure.ObjectName.Should().Be("P1");
        procedure.Parameters.Should().HaveCount(1);
    }
}
