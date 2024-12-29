using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using FluentAssertions;
using Xunit;

namespace DatabaseAnalyzer.Testing.Tests;

public sealed class TestCodeProcessorTests
{
    private static readonly DiagnosticDefinitionRegistry DiagnosticDefinitionRegistry = new([TestDiagnosticDefinitions.TestDiagnostic0, TestDiagnosticDefinitions.TestDiagnostic1, TestDiagnosticDefinitions.TestDiagnostic2]);

    [Fact]
    public void WhenNoMarkup_ThenNoIssues_ThenCodeIsLikeInput()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            abc
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        result.ExpectedIssues.Should().BeEmpty();
        result.MarkupFreeSql.Should().Be(code);
    }

    [Fact]
    public void MarkupWithoutInsertionString()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0000░file.sql░MyDb.dbo.p1███bbb█ccc
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedLocation = CodeRegion.Create(1, 4, 1, 7);
        var expectedIssue = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic0, "MyDb", "file.sql", "MyDb.dbo.p1", expectedLocation);

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue);
        result.ExpectedIssues.Should().HaveCount(1);

        result.MarkupFreeSql.Should().Be("aaabbbccc");
    }

    [Fact]
    public void MarkupWithoutOneInsertionString()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0001░file.sql░MyDb.dbo.p1░X███bbb█ccc
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedLocation = CodeRegion.Create(1, 4, 1, 7);
        var expectedIssue = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic1, "MyDb", "file.sql", "MyDb.dbo.p1", expectedLocation, "X");

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue);
        result.ExpectedIssues.Should().HaveCount(1);

        result.MarkupFreeSql.Should().Be("aaabbbccc");
    }

    [Fact]
    public void MarkupWithoutTwoInsertionStrings()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0002░file.sql░MyDb.dbo.p1░X░Y███bbb█ccc
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedLocation = CodeRegion.Create(1, 4, 1, 7);
        var expectedIssue = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic2, "MyDb", "file.sql", "MyDb.dbo.p1", expectedLocation, "X", "Y");

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue);
        result.ExpectedIssues.Should().HaveCount(1);

        result.MarkupFreeSql.Should().Be("aaabbbccc");
    }

    [Fact]
    public void MarkupSpansAcrossMultipleLines()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0000░file.sql░MyDb.dbo.p1███---
                            ---
                            ---bbb█ccc
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedLocation = CodeRegion.Create(1, 4, 3, 7);
        var expectedIssue = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic0, "MyDb", "file.sql", "MyDb.dbo.p1", expectedLocation);

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue);
        result.ExpectedIssues.Should().HaveCount(1);

        result.MarkupFreeSql.Should().Be("""
                                         aaa---
                                         ---
                                         ---bbbccc
                                         """);
    }

    [Fact]
    public void MarkupSpansAcrossMultipleLinesAndEndsAtBeginningOfLine()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0000░file.sql░MyDb.dbo.p1███---
                            ---
                            █ccc
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedLocation = CodeRegion.Create(1, 4, 3, 1);
        var expectedIssue = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic0, "MyDb", "file.sql", "MyDb.dbo.p1", expectedLocation);

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue);
        result.ExpectedIssues.Should().HaveCount(1);

        result.MarkupFreeSql.Should().Be("""
                                         aaa---
                                         ---
                                         ccc
                                         """);
    }

    [Fact]
    public void WhenMultipleMarkupBlocksOnSameLine_ThenResultMustContainAllOfThem()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0002░file.sql░MyDb.dbo.p1░X░Y███bbb█cccddd█TE0002░file.sql░MyDb.dbo.p1░X░Y███eee█fff
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedRegion1 = CodeRegion.Create(1, 4, 1, 7);
        var expectedRegion2 = CodeRegion.Create(1, 13, 1, 16);
        var expectedIssue1 = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic2, "MyDb", "file.sql", "MyDb.dbo.p1", expectedRegion1, "X", "Y");
        var expectedIssue2 = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic2, "MyDb", "file.sql", "MyDb.dbo.p1", expectedRegion2, "X", "Y");

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue1);
        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue2);
        result.ExpectedIssues.Should().HaveCount(2);

        result.MarkupFreeSql.Should().Be("aaabbbcccdddeeefff");
    }

    [Fact]
    public void WhenMultipleMarkupBlocksOnDifferentLines_ThenResultMustContainAllOfThem()
    {
        // arrange
        var sut = new TestCodeProcessor(DiagnosticDefinitionRegistry);
        const string code = """
                            aaa█TE0002░file.sql░MyDb.dbo.p1░X░Y███bbb█ccc
                            111█TE0002░file.sql░MyDb.dbo.p1░X░Y███222█333
                            """;

        // act
        var result = sut.ParseTestCode(code);

        // assert
        var expectedRegion1 = CodeRegion.Create(1, 4, 1, 7);
        var expectedRegion2 = CodeRegion.Create(2, 4, 2, 7);
        var expectedIssue1 = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic2, "MyDb", "file.sql", "MyDb.dbo.p1", expectedRegion1, "X", "Y");
        var expectedIssue2 = Issue.Create(TestDiagnosticDefinitions.TestDiagnostic2, "MyDb", "file.sql", "MyDb.dbo.p1", expectedRegion2, "X", "Y");

        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue1);
        result.ExpectedIssues.Should().ContainEquivalentOf(expectedIssue2);
        result.ExpectedIssues.Should().HaveCount(2);

        result.MarkupFreeSql.Should().Be("aaabbbccc\r\n111222333");
    }
}
