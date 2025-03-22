using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using DatabaseAnalyzers.DefaultAnalyzers.Model;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Indices;

public sealed class IndexNamingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<IndexNamingAnalyzer>(testOutputHelper)
{
    private static readonly Aj5052Settings Settings = new
    (
        DefaultPattern: "IX_{TableSchemaName}_{TableName}_{ColumnNames}",
        NamingPatterns:
        [
            new Aj5052SettingsEntry(IndexProperties.PrimaryKey, "PK_{TableSchemaName}_{TableName}"),
            new Aj5052SettingsEntry(IndexProperties.Unique, "UX_{TableSchemaName}_{TableName}_{ColumnNames}")
        ]
    );

    [Fact]
    public void WhenNoRuleApplies_ThenFallbackToDefault()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE NONCLUSTERED INDEX [IX_dbo_T1_Status] ON [dbo].[T1]
                            (
                                [Status] ASC
                            )
                            GO
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void UniqueIndexWithOneColumn()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE UNIQUE INDEX UX_dbo_T1_Status
                            ON T1 (Status);
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void UniqueIndexWithTwoColumns()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE UNIQUE INDEX UX_dbo_T1_ColumnX_ColumnY
                            ON T1 (ColumnX, ColumnY);
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WhenUniqueIndexHasWrongName_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE UNIQUE INDEX ▶️AJ5052💛script_0.sql💛MyDb.dbo.T1💛IX_Bla💛UX_dbo_T1_ColumnX💛Unique✅IX_Bla◀️
                            ON T1 (ColumnX);
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WhenPkConstraintInTableCreationIsHavingTheWrongName_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE [dbo].[T1]
                            (
                                [Id] [INT] NOT NULL,
                                CONSTRAINT ▶️AJ5052💛script_0.sql💛MyDb.dbo.T1💛PK_Bla💛PK_dbo_T1💛PrimaryKey, Clustered✅PK_Bla◀️ PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                )
                            )
                            """;

        Verify(Settings, code);
    }

    [Fact]
    public void WithAlterTable_WhenPkConstraintIsDifferent_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE T1
                            ADD CONSTRAINT ▶️AJ5052💛script_0.sql💛MyDb.dbo.T1💛PK_Bla💛PK_dbo_T1💛PrimaryKey, Clustered✅PK_Bla◀️ PRIMARY KEY CLUSTERED (Id);

                            """;

        Verify(Settings, code);
    }
}
