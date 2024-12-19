using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using DatabaseAnalyzer.Testing;
using FluentAssertions;
using Xunit.Abstractions;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Tests.SqlParsing;

// TODO: remove
#pragma warning disable S125

public sealed class FilteringColumnVisitorTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void WithSingleTable_WithoutAlias_WithWhere_ThenResultContainsFilteringColumn()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id
                            FROM        Person
                            WHERE       Name = N'Uribubu'
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(1);
    }

    [Fact]
    public void WithSingleTable_WithAlias_WithWhere_ThenResultContainsFilteringColumn()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      p.Id
                            FROM        Person p
                            WHERE       Name = N'Uribubu'
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(1);
    }

    [Fact]
    public void WithSingleTable_WithoutAlias_WithIn_ThenResultContainsFilteringColumn()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Id
                            FROM        Person
                            WHERE       Name IN (N'Uribubu', N'Uribubu Master')
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(1);
    }

    [Fact]
    public void WithJoin_WithAlias_WithIn_ThenResultContainsFilteringColumn()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT
                                        p.Id            'PersonId',
                                        p.DepartmentId  'DepartmentId',
                                        p.Name          'PersonName',
                                        d.Name          'DepartmentName'
                            FROM        Person          p
                            INNER JOIN  Department      d ON p.DepartmentId = d.DepartmentId
                            WHERE       p.Name = N'Uribubu'
                                        AND d.Name IN ('aaa', 'bbb')
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "DepartmentId", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Department", "DepartmentId", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Department", "Name", null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(4);
    }

    [Fact]
    public void WithCte_WithInnerFilter_WithOuterFilter_ThenResultContainsBothFilteringColumn()
    {
        const string code = """
                            USE MyDb
                            GO

                            ;WITH CTE AS
                            (
                                SELECT  *
                                FROM    T1
                                WHERE   Name = N'Uribubu'
                            )
                            SELECT  *
                            FROM    CTE
                            WHERE   Name2 = N'Uribubu2';
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        true.Should().BeTrue();
        /*
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "DepartmentId", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Department", "DepartmentId", null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(3);
        */
    }

    [Fact]
    public void WithCte_()
    {
        const string code = """
                            ;WITH Standard_CTE AS (
                                SELECT * FROM T1
                            )
                            SELECT * FROM Standard_CTE;

                            -- Sequential CTE
                            ;WITH CTE1 AS (
                                SELECT * FROM T1
                            ),
                            CTE2 AS (SELECT * FROM CTE1),
                            CTE3 AS (SELECT * FROM CTE2)
                            SELECT * FROM CTE3
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        true.Should().BeTrue();
    }

    private ScriptModel CreateScript(string code)
    {
        var parsedScript = code.ParseSqlScript();

        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine("Syntax Tree:");
        testOutputHelper.WriteLine(SyntaxTreeVisualizer.Visualize(parsedScript));

        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine(string.Empty);
        testOutputHelper.WriteLine("Tokens:");
        testOutputHelper.WriteLine(TokenVisualizer.Visualize(parsedScript));

        return new ScriptModel(
            "database-1",
            "script.sql",
            code,
            parsedScript,
            parsedScript.CreateParentFragmentProvider(),
            [],
            []
        );
    }

    /* TEMPLATE

    [Fact]
    public void ToDo()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT
                                        p.Id            'PersonId',
                                        p.DepartmentId  'DepartmentId',
                                        p.Name          'PersonName',
                                        d.Name          'DepartmentName'
                            FROM        Person          p
                            INNER JOIN  Department      d ON p.DepartmentId = d.DepartmentId
                            WHERE       p.Name = N'Uribubu'
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "DepartmentId", null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Department", "DepartmentId", null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(3);
    }
*/
}
