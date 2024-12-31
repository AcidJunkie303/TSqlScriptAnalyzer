using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.ObjectCreation;

public sealed class NamelessConstraintAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NamelessConstraintAnalyzer>(testOutputHelper)
{
    #region Primary Key Constraints

    [Fact]
    public void WithCreateTable_WhenCreatingNamedPrimaryKeyConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                CONSTRAINT  [PK_T1] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                )
                            )
                            """;

        Verify(code);
    }

    [Fact]
    public void WithCreateTable_WhenCreatingUnnamedPrimaryKeyConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL █AJ5039░script_0.sql░MyDb.dbo.Table1███PRIMARY KEY█,
                                Value1      NVARCHAR(128) NOT NULL
                            )

                            """;

        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingNamedPrimaryKeyConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE     Table1
                            ADD CONSTRAINT  PK_Table1 PRIMARY KEY (Id);
                            """;

        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingUnnamedPrimaryKeyConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO


                            ALTER TABLE     Table1
                            ADD             Id INT NOT NULL █AJ5039░script_0.sql░MyDb.dbo.Table1███PRIMARY KEY█

                            """;

        Verify(code);
    }

    #endregion Primary Key Constraints

    #region Unique Constraints

    [Fact]
    public void WithCreateTable_WhenCreatingNamedUniqueConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                Value1      INT NOT NULL,
                                CONSTRAINT  UQ_Table1_Value1 UNIQUE (Value1)
                            )
                            """;

        Verify(code);
    }

    [Fact]
    public void WithCreateTable_WhenCreatingUnnamedUniqueConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                Value1      INT NOT NULL █AJ5039░script_0.sql░MyDb.dbo.Table1███UNIQUE█
                            )

                            """;

        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingNamedUniqueConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE Table1
                            ADD CONSTRAINT UQ_Table1_Id  UNIQUE (Id);
                            """;

        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingUnnamedUniqueConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE Table1
                            ADD         Id INT NOT NULL █AJ5039░script_0.sql░MyDb.dbo.Table1███UNIQUE█

                            """;

        Verify(code);
    }

    #endregion Unique Constraints

    #region Default Constraints

    [Fact(Skip = "Default constraints cannot be added inline during table creation")]
    public void WithCreateTable_WhenCreatingNamedDefaultConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                Value1      DATETIME NOT NULL,
                                CONSTRAINT  DF_Table1_Value1 DEFAULT GETDATE() FOR Value1,
                            )
                            """;

        Verify(code);
    }

    [Fact]
    public void WithCreateTable_WhenCreatingUnnamedDefaultConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                Value1      DATETIME NOT NULL █AJ5039░script_0.sql░MyDb.dbo.Table1███UNIQUE█
                            )

                            """;

        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingNamedDefaultConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE Table1
                            ADD Value1 INT NOT NULL
                            CONSTRAINT DF_Table1_Value1 DEFAULT 1;
                            """;
        Verify(code);
    }

    // TODO: Check if this issue has been solved
    [Fact(Skip = "Doesn't work at the moment because the default constraints are not parsed during ALTER TABLE statements. Issue: https://github.com/microsoft/SqlScriptDOM/issues/107")]
    public void WithAlterTable_WhenCreatingUnnamedDefaultConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE Table1
                            ADD Value1 INT █AJ5039░script_0.sql░MyDb.dbo.Table1███DEFAULT 1█

                            """;

        Verify(code);
    }

    #endregion Default Constraints

    #region Check Constraints

    [Fact]
    public void WithCreateTable_WhenCreatingNamedCheckConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                Value1      INT NOT NULL,
                                CONSTRAINT  CHK_Table1_Value1  CHECK (Value1 > 0)
                            )
                            """;
        Verify(code);
    }

    [Fact]
    public void WithCreateTable_WhenCreatingUnnamedCheckConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE Table1
                            (
                                Id          INT NOT NULL,
                                Value1      INT NOT NULL █AJ5039░script_0.sql░MyDb.dbo.Table1███CHECK (Value1 > 0)█
                            )

                            """;

        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingNamedCheckConstraint_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE     Table1
                            ADD             CONSTRAINT CHK_Table1_Value1 CHECK (Value1 > 0);
                            """;
        Verify(code);
    }

    [Fact]
    public void WithAlterTable_WhenCreatingUnnamedCheckConstraint_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            ALTER TABLE     Table1
                            ADD             Value1 INT █AJ5039░script_0.sql░MyDb.dbo.Table1███CHECK (Value1 > 0)█
                            """;

        Verify(code);
    }

    #endregion Check Constraints
}
