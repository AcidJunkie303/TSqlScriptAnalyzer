using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Maintainability;

public sealed class AliasNameIsNameOfTableOrColumnTests(ITestOutputHelper testOutputHelper)
    : GlobalAnalyzerTestsBase<AliasNameIsNameOfTableOrColumn>(testOutputHelper)
{
    [Theory]
    [InlineData("xyz", "xyz")]
    [InlineData("table1", "▶️AJ5067💛script_0.sql💛💛table1✅table1◀️")]
    public void Theory(string aliasName, string aliasInsertionCode)
    {
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE Table1
                    (
                        Id INT PRIMARY KEY,
                        Name NVARCHAR(50)
                    )
                    GO
                    CREATE TABLE SomeTable
                    (
                        Id INT PRIMARY KEY,
                        Name NVARCHAR(50)
                    )
                    GO

                    SELECT     {aliasName}.Name
                    FROM       SomeTable {aliasInsertionCode}
                    """;
        Verify(code);
    }
}
