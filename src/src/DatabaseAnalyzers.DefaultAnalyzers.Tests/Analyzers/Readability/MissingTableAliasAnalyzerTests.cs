using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Readability;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Readability;

public sealed class MissingTableAliasAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingTableAliasAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenSingleTable_WhenColumnReferenceInWhereClauseHasNoAlias_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      Name
                            FROM        Table1
                            WHERE       DepartmentName = 'Uribubu'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenSingleTable_WhenColumnReferenceInWhereClauseHasAlias_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      t.Name
                            FROM        Table1 t
                            WHERE       t.DepartmentName = 'Uribubu'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenMultipleTable_WhenColumnReferenceInWhereClauseHasAlias_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      t1.Name
                            FROM        Table1  t1
                            INNER JOIN  Table2  t2 ON t1.Id = t2.OtherId
                            WHERE       t2.DepartmentName = 'Uribubu'
                            """;
        Verify(code);
    }

    [Fact]
    public void WhenMultipleTable_WhenColumnReferenceInWhereClauseHasNoAlias_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            SELECT      t1.Name
                            FROM        Table1  t1
                            INNER JOIN  Table2  t2 ON t1.Id = t2.OtherId
                            WHERE       █AJ5016░main.sql░░DepartmentName███DepartmentName█ = 'Uribubu'
                            """;
        Verify(code);
    }
}
