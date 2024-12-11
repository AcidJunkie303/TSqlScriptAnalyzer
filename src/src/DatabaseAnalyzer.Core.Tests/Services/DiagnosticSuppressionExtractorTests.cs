using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Services;
using FluentAssertions;

namespace DatabaseAnalyzer.Core.Tests.Services;

public sealed class DiagnosticSuppressionExtractorTests
{
    [Fact]
    public void WhenSuppressionIsInEndOfLineComment_ThenExtract()
    {
        const string sql = """
                           -- #pragma diagnostic disable AJ1111
                           """;
        // arrange
        var script = sql.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(1);
        suppressions.Should().ContainEquivalentOf(new Suppression("AJ1111", 1, 4, SuppressionAction.Disable));
    }

    [Fact]
    public void WhenSuppressionIsInMultiLineComment_ThenExtract()
    {
        const string sql = """
                           /*
                             #pragma diagnostic disable AJ1111
                           */
                           """;
        // arrange
        var script = sql.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(1);
        suppressions.Should().ContainEquivalentOf(new Suppression("AJ1111", 2, 3, SuppressionAction.Disable));
    }

    [Fact]
    public void WhenMultipleSuppressionIdsInSameSuppression_ThenExtractAll()
    {
        const string sql = """
                           -- #pragma diagnostic disable AJ1111 , AJ2222
                           """;
        // arrange
        var script = sql.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(2);
        suppressions[0].Should().Be(new Suppression("AJ1111", 1, 4, SuppressionAction.Disable));
        suppressions[1].Should().Be(new Suppression("AJ2222", 1, 4, SuppressionAction.Disable));
    }

    [Fact]
    public void AllTogether()
    {
        const string sql = """
                           -- #pragma diagnostic disable AJ1111
                           -- #pragma diagnostic disable AJ2222,AJ3333
                           -- #pragma diagnostic restore AJ3333 , AJ2222
                           -- #pragma diagnostic restore AJ1111
                           """;
        // arrange
        var script = sql.ParseSqlScript();
        var sut = new DiagnosticSuppressionExtractor();

        // act
        var suppressions = sut
            .ExtractSuppressions(script)
            .ToList();

        // assert
        suppressions.Should().HaveCount(6);
        suppressions[0].Should().Be(new Suppression("AJ1111", 1, 4, SuppressionAction.Disable));
        suppressions[1].Should().Be(new Suppression("AJ2222", 2, 4, SuppressionAction.Disable));
        suppressions[2].Should().Be(new Suppression("AJ3333", 2, 4, SuppressionAction.Disable));
        suppressions[3].Should().Be(new Suppression("AJ3333", 3, 4, SuppressionAction.Restore));
        suppressions[4].Should().Be(new Suppression("AJ2222", 3, 4, SuppressionAction.Restore));
        suppressions[5].Should().Be(new Suppression("AJ1111", 4, 4, SuppressionAction.Restore));
    }
}
