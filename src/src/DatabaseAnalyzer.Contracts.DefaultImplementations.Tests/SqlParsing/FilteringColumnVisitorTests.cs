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
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
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
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
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
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
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
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "Name", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Person", "DepartmentId", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Department", "DepartmentId", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
            options => options.Excluding(p => p.Fragment));
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "Department", "Name", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(4);
    }

    [Fact]
    public void Test___()
    {
        const string code = """
                            USE MyDb
                            GO

                            delete t1
                            FROM Table1 t1
                            inner join Table2 t2 on t2.Id = t1.id
                            where t2.Value2 = 303

                            """;

        // arrange
        var script = CreateScript(code);
        var filteringColumnExpressionFinder = new FilteringColumnExpressionFinder("dbo", script.ParsedScript);
        filteringColumnExpressionFinder.ExplicitVisit(script.ParsedScript);
        var filteringColumns = filteringColumnExpressionFinder.Columns;
        filteringColumns.Should().NotBeNull();
/*
        var resolver = new ColumnResolver(script.ParsedScript);
        foreach (var filteringColumn in filteringColumns)
        {
            var column = resolver.ResolveColumnSource(filteringColumn.Column);
            if (column.ObjectName is null)
            {
                continue;
            }

            testOutputHelper.WriteLine($"{filteringColumn.Column.MultiPartIdentifier.ToUnquotedIdentifier()}    ->    {column.DatabaseName}.{column.SchemaName}.{column.ObjectName}");
        }
*/
        // assert
    }

    [Fact]
    public void ToDo()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE T1
                            WHERE ID = 303

                            DELETE FROM T1
                            WHERE ID = 1

                            INSERT INTO T1
                            SELECT * FROM T2

                            select
                            *
                            from T1 table1
                            WHERE Value1 = (
                            	SELECT TOP 1 Value2
                            	FROM T2 table2
                            	WHERE table1.Id = 3
                            )

                            UPDATE t
                            SET t.Value = 303
                            FROM Table1 t
                            WHERE t.Id = 909

                            --SELECT  303 AS Id
                            --INTO    #t

                            SELECT  *
                            FROM    #t
                            WHERE   Id = 303

                            SELECT  *
                            FROM    Table1         t1
                            INNER   JOIN Table2   t2 ON t1.Id = t2.Id
                            WHERE   t2.Id = 303

                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        true.Should().BeTrue();
    }

    [Fact]
    public void ToDo_VariousFormsOfDelete()
    {
        const string code = """
                            USE MyDb
                            GO

                            DELETE  Table1
                            WHERE   ID = 303

                            DELETE
                            FROM    Table1
                            WHERE   ID = 1

                            DELETE  t1
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 on t2.Id = t1.id
                            WHERE   t2.Value2 = 303

                            DELETE  t1
                            FROM    Table1 t1
                            INNER   JOIN Table2 t2 on t2.Id = t1.id
                            INNER   JOIN Table3 t3 on t3.Id = t2.id
                            WHERE   t3.Value3 = 303

                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        sut.ExplicitVisit(script.ParsedScript);

        // assert
        true.Should().BeTrue();
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
                            FROM    CTE c
                            WHERE   c.Name2 = N'Uribubu2';
                            """;

        // arrange
        var script = CreateScript(code);
        var sut = new FilteringColumnVisitor("dbo");

        // act
        script.ParsedScript.Accept(sut);

        // assert
        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "T1", "Name", ScopedSqlFragmentVisitor.SourceType.TableOrView, null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().ContainEquivalentOf(
            new FilteringColumnVisitor.FilteringColumn("MyDb", "dbo", "CTE", "Name2", ScopedSqlFragmentVisitor.SourceType.Cte, null!),
            options => options.Excluding(p => p.Fragment));

        sut.FilteringColumns.Should().HaveCount(2);
    }

    [Fact]
    public void WithCte_()
    {
        const string code = """
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
