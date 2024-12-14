using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Tables;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Tables;

public sealed class MissingCommaBeforeConstraintKeywordAnalyzerTests(ITestOutputHelper testOutputHelper) : ScriptAnalyzerTestsBase<MissingCommaBeforeConstraintKeywordAnalyzer>(testOutputHelper)
{
    [Fact]
    public void WhenCommaBeforeConstraintKeyword_ThenOk()
    {
        const string sql = """
                           CREATE TABLE Employee
                           (
                               Id INT NOT NULL,
                               Name VARCHAR(50) NOT NULL,
                               CONSTRAINT PK_Employee PRIMARY KEY (Id)
                           );
                           """;
        Verify(sql);
    }

    [Fact]
    public void WhenNoCommaBeforeConstraintKeyword_ThenDiagnose()
    {
        const string sql = """
                           CREATE TABLE Employee
                           (
                               Id INT NOT NULL,
                               Name VARCHAR(50) NOT NULL /* aa */  -- bla
                               █AJ5005░main.sql░dbo.Employee███CONSTRAINT█ PK_Employee PRIMARY KEY (Id)
                           );
                           """;
        Verify(sql);
    }
}
