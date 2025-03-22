using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.ObjectCreation;

public sealed class ObjectCreationNotEmbeddedInExistenceCheckAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectCreationNotEmbeddedInExistenceCheckAnalyzer>(testOutputHelper)
{
    private static readonly Aj5025Settings Settings = new
    (
        "IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[{TableSchemaName}].[{TableName}]'))"
    );

    [Fact]
    public void WithTable_WhenEmbeddedInCorrectExistenceCheck_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[T1]'))
                            CREATE TABLE [dbo].[T1]
                            (
                               [Id] [INT]
                            )
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WithTable_WhenEmbeddedInIncorrectExistenceCheck_ThenOk()
    {
        // some spaces inserted after `EXISTS`
        const string code = """
                            USE MyDb
                            GO

                            IF NOT EXISTS   (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[T1]'))
                            ▶️AJ5025💛script_0.sql💛MyDb.dbo.T1💛IF NOT EXISTS (SELECT 1 FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[T1]'))✅CREATE TABLE [dbo].[T1]
                            (
                               [Id] [INT]
                            )◀️

                            """;

        Verify(Settings, code);
    }
}
