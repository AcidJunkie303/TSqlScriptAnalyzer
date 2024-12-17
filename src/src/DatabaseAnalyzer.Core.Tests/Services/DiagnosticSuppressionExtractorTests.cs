using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Services;
using FluentAssertions;

namespace DatabaseAnalyzer.Core.Tests.Services;

public sealed class DiagnosticSuppressionExtractorTests
{
    [Fact]
    public void WhenSuppressionIsInEndOfLineComment_ThenExtract()
    {
        const string code = """
                            -- #pragma diagnostic disable AJ1111 -> Bla
                            """;
        // arrange
        var script = code.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(1);
        suppressions.Should().ContainEquivalentOf(new DiagnosticSuppression("AJ1111", new CodeLocation(1, 4), SuppressionAction.Disable, "Bla"));
    }

    [Fact]
    public void WhenSuppressionIsInMultiLineComment_ThenExtract()
    {
        const string code = """
                            /*
                              #pragma diagnostic disable AJ1111 -> Hello World
                            */
                            """;
        // arrange
        var script = code.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(1);
        suppressions.Should().ContainEquivalentOf(new DiagnosticSuppression("AJ1111", new CodeLocation(2, 3), SuppressionAction.Disable, "Hello World"));
    }

    [Fact]
    public void WhenMultipleSuppressionIdsInSameSuppression_ThenExtractAll()
    {
        const string code = """
                            -- #pragma diagnostic disable AJ1111 , AJ2222 -> Whatever
                            """;
        // arrange
        var script = code.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(2);
        suppressions[0].Should().Be(new DiagnosticSuppression("AJ1111", new CodeLocation(1, 4), SuppressionAction.Disable, "Whatever"));
        suppressions[1].Should().Be(new DiagnosticSuppression("AJ2222", new CodeLocation(1, 4), SuppressionAction.Disable, "Whatever"));
    }

    [Fact]
    public void AllTogether()
    {
        const string code = """
                            -- #pragma diagnostic disable AJ1111 -> aa
                            -- #pragma diagnostic disable AJ2222,AJ3333 -> bb
                            -- #pragma diagnostic restore AJ3333 , AJ2222 -> cc
                            -- #pragma diagnostic restore AJ1111 -> dd
                            """;
        // arrange
        var script = code.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(6);
        suppressions[0].Should().Be(new DiagnosticSuppression("AJ1111", new CodeLocation(1, 4), SuppressionAction.Disable, "aa"));
        suppressions[1].Should().Be(new DiagnosticSuppression("AJ2222", new CodeLocation(2, 4), SuppressionAction.Disable, "bb"));
        suppressions[2].Should().Be(new DiagnosticSuppression("AJ3333", new CodeLocation(2, 4), SuppressionAction.Disable, "bb"));
        suppressions[3].Should().Be(new DiagnosticSuppression("AJ3333", new CodeLocation(3, 4), SuppressionAction.Restore, ""));
        suppressions[4].Should().Be(new DiagnosticSuppression("AJ2222", new CodeLocation(3, 4), SuppressionAction.Restore, ""));
        suppressions[5].Should().Be(new DiagnosticSuppression("AJ1111", new CodeLocation(4, 4), SuppressionAction.Restore, ""));
    }
}
