using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

#pragma warning disable S125

[SuppressMessage("Roslynator", "RCS1262:Unnecessary raw string literal", Justification = "For better test code alignment")]
[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations")]
public sealed class NameQuotingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NameQuotingAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0000 */                 dbo.T1                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0001 */                 [dbo].T1                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0002 */                 dbo.[T1]                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0003 */                 "dbo".T1                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0004 */                 dbo."T1"                                                          """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0010 */                 ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0011 */                 [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0012 */                 "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0013 */                 [dbo].[T1]                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0014 */                 "dbo"."T1"                                                        """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0020 */               T1                                                                """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0021 */               dbo.T1                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0022 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛[dbo]💛dbo💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️.T1      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0023 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛"dbo"💛dbo💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.T1      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0024 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛[T1]💛T1💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0025 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛"T1"💛T1💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️        """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0030 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0031 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛[T1]💛"T1"💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️    """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0032 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0033 */     "dbo"."T1"                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0034 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛dbo💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅dbo◀️."T1"      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0035 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛[dbo]💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️."T1"  """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0040 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0041 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛"T1"💛[T1]💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️    """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0042 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0043 */   [dbo].[T1]                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0044 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛dbo💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅dbo◀️.[T1]      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0045 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛table💛"dbo"💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.[T1]  """)]
    public void WithTableCreation_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string tableNameCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyDuringObjectCreation = aj5038SettingsNameQuotingPolicy };

        // we test the table name creation. We don't want the analyzer to yield also issues for the column naming
        // therefore, we quote them as needed. There's a separate test
        var columnName = aj5038SettingsNameQuotingPolicy switch
        {
            Aj5038SettingsNameQuotingPolicy.Undefined              => "Column1",
            Aj5038SettingsNameQuotingPolicy.Required               => "[Column1]",
            Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired   => "\"Column1\"",
            Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired => "[Column1]",
            Aj5038SettingsNameQuotingPolicy.NotAllowed             => "Column1",
            _                                                      => throw new ArgumentOutOfRangeException(nameof(aj5038SettingsNameQuotingPolicy), aj5038SettingsNameQuotingPolicy, message: null)
        };

        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE {tableNameCode}
                    (
                        {columnName} INT
                    )
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0000 */                 dbo.T1                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0001 */                 [dbo].T1                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0002 */                 dbo.[T1]                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0003 */                 "dbo".T1                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0004 */                 dbo."T1"                                                            """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0010 */                 ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️             """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0011 */                 [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0012 */                 "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0013 */                 [dbo].[T1]                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0014 */                 "dbo"."T1"                                                          """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0020 */               T1                                                                  """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0021 */               dbo.T1                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0022 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛[dbo]💛dbo💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️.T1     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0023 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛"dbo"💛dbo💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.T1     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0024 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛[T1]💛T1💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0025 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛"T1"💛T1💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️       """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0030 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️             """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0031 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛[T1]💛"T1"💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️   """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0032 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0033 */     "dbo"."T1"                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0034 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛dbo💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅dbo◀️."T1"     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0035 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛[dbo]💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️."T1" """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0040 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️             """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0041 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛"T1"💛[T1]💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️   """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0042 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0043 */   [dbo].[T1]                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0044 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛dbo💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅dbo◀️.[T1]     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0045 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛function💛"dbo"💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.[T1] """)]
    public void WithFunctionCreation_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string functionNameCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyDuringObjectCreation = aj5038SettingsNameQuotingPolicy };
        var code = $"""
                    USE MyDb
                    GO

                    CREATE FUNCTION {functionNameCode} ()
                    RETURNS INT
                    AS
                    BEGIN
                      RETURN 1
                    END
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0000 */ dbo.T1                                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0001 */ [dbo].T1                                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0002 */ dbo.[T1]                                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0003 */ "dbo".T1                                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0004 */ dbo."T1"                                                                            """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0010 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0011 */ [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️                      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0012 */ "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️                      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0013 */ [dbo].[T1]                                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0014 */ "dbo"."T1"                                                                          """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0020 */ T1                                                                                """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0021 */ dbo.T1                                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0022 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛[dbo]💛dbo💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️.T1                  """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0023 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛"dbo"💛dbo💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.T1                  """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0024 */ dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛[T1]💛T1💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️                    """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0025 */ dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛"T1"💛T1💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️                    """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0030 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️                """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0031 */ "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛[T1]💛"T1"💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0032 */ "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0033 */ "dbo"."T1"                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0034 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛dbo💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅dbo◀️."T1"        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0035 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛[dbo]💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️."T1"    """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0040 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0041 */ [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛"T1"💛[T1]💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️    """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0042 */ [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0043 */ [dbo].[T1]                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0044 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛dbo💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅dbo◀️.[T1]      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0045 */ ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛procedure💛"dbo"💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.[T1]  """)]
    public void WithProcedureCreation_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string procedureNameCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyDuringObjectCreation = aj5038SettingsNameQuotingPolicy };
        var code = $"""
                    USE MyDb
                    GO

                    CREATE PROCEDURE {procedureNameCode}
                    AS
                    BEGIN
                        SELECT 1 AS Column1
                    END
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0000 */                 dbo.T1                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0001 */                 [dbo].T1                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0002 */                 dbo.[T1]                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0003 */                 "dbo".T1                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0004 */                 dbo."T1"                                                        """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0010 */                 ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️             """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0011 */                 [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0012 */                 "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0013 */                 [dbo].[T1]                                                      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0014 */                 "dbo"."T1"                                                      """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0020 */               T1                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0021 */               dbo.T1                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0022 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛[dbo]💛dbo💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️.T1     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0023 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛"dbo"💛dbo💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.T1     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0024 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛[T1]💛T1💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0025 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛"T1"💛T1💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️       """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0030 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️             """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0031 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛[T1]💛"T1"💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️   """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0032 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0033 */     "dbo"."T1"                                                      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0034 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛dbo💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅dbo◀️."T1"     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0035 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛[dbo]💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️."T1" """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0040 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️             """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0041 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛"T1"💛[T1]💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️   """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0042 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0043 */   [dbo].[T1]                                                      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0044 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛dbo💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅dbo◀️.[T1]     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0045 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛view💛"dbo"💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.[T1] """)]
    public void WithViewCreation_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string viewCodeName)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyDuringObjectCreation = aj5038SettingsNameQuotingPolicy };
        var code = $"""
                    USE MyDb
                    GO

                    CREATE VIEW {viewCodeName}
                    AS
                        SELECT 1 AS Column1
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0000 */                 dbo.T1                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0001 */                 [dbo].T1                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0002 */                 dbo.[T1]                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0003 */                 "dbo".T1                                                            """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0004 */                 dbo."T1"                                                            """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0010 */                 ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0011 */                 [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0012 */                 "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0013 */                 [dbo].[T1]                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """  /* 0014 */                 "dbo"."T1"                                                          """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0020 */               T1                                                                  """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0021 */               dbo.T1                                                              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0022 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛[dbo]💛dbo💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️.T1      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0023 */               ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛"dbo"💛dbo💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.T1      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0024 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛[T1]💛T1💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """  /* 0025 */               dbo.▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛"T1"💛T1💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️        """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0030 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0031 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛[T1]💛"T1"💛NameQuotingPolicyDuringObjectCreation✅[T1]◀️    """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0032 */     "dbo".▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛"T1"💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0033 */     "dbo"."T1"                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0034 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛dbo💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅dbo◀️."T1"      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """  /* 0035 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛[dbo]💛"dbo"💛NameQuotingPolicyDuringObjectCreation✅[dbo]◀️."T1"  """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0040 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️              """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0041 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛"T1"💛[T1]💛NameQuotingPolicyDuringObjectCreation✅"T1"◀️    """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0042 */   [dbo].▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛T1💛[T1]💛NameQuotingPolicyDuringObjectCreation✅T1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0043 */   [dbo].[T1]                                                          """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0044 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛dbo💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅dbo◀️.[T1]      """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """  /* 0045 */   ▶️AJ5038💛script_0.sql💛MyDb.dbo.T1💛trigger💛"dbo"💛[dbo]💛NameQuotingPolicyDuringObjectCreation✅"dbo"◀️.[T1]  """)]
    public void WithTriggerCreation_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string triggerNameCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyDuringObjectCreation = aj5038SettingsNameQuotingPolicy };
        var code = $"""
                    USE MyDb
                    GO

                    CREATE TRIGGER {triggerNameCode}
                        ON dbo.Table1
                        AFTER INSERT
                        AS
                            BEGIN PRINT 303
                        END
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0000 */                 Column1                                                         """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0001 */                 "Column1"                                                       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Undefined, """ /* 0002 */                 [Column1]                                                       """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """ /* 0010 */                  ▶️AJ5038💛script_0.sql💛💛column💛Column1💛[Column1]💛NameQuotingPolicyForColumnReferences✅Column1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """ /* 0011 */                  "Column1"                                                       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.Required, """ /* 0012 */                  [Column1]                                                       """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """ /* 0020 */                Column1                                                         """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """ /* 0021 */                ▶️AJ5038💛script_0.sql💛💛column💛"Column1"💛Column1💛NameQuotingPolicyForColumnReferences✅"Column1"◀️     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.NotAllowed, """ /* 0022 */                ▶️AJ5038💛script_0.sql💛💛column💛[Column1]💛Column1💛NameQuotingPolicyForColumnReferences✅[Column1]◀️     """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """ /* 0030 */      ▶️AJ5038💛script_0.sql💛💛column💛Column1💛"Column1"💛NameQuotingPolicyForColumnReferences✅Column1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """ /* 0031 */      "Column1"                                                       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """ /* 0032 */      ▶️AJ5038💛script_0.sql💛💛column💛[Column1]💛"Column1"💛NameQuotingPolicyForColumnReferences✅[Column1]◀️   """)]
    //
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0040 */    ▶️AJ5038💛script_0.sql💛💛column💛Column1💛[Column1]💛NameQuotingPolicyForColumnReferences✅Column1◀️       """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0041 */    ▶️AJ5038💛script_0.sql💛💛column💛"Column1"💛[Column1]💛NameQuotingPolicyForColumnReferences✅"Column1"◀️   """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0042 */    [Column1]                                                       """)]
    public void WithColumnReference_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string columnCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyForColumnReferences = aj5038SettingsNameQuotingPolicy };
        var code = $"""
                    USE MyDb
                    GO

                    SELECT
                              {columnCode}
                    FROM      Table1

                    SELECT
                              t1.{columnCode}
                    FROM      Table1 AS t1

                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0000 */     [Column1]                                                                                  """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0001 */     ▶️AJ5038💛script_0.sql💛MyDb.dbo.Table1💛column definition💛Column1💛[Column1]💛NameQuotingPolicyForColumnDefinitions✅Column1◀️        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """ /* 0002 */       ▶️AJ5038💛script_0.sql💛MyDb.dbo.Table1💛column definition💛[Column1]💛"Column1"💛NameQuotingPolicyForColumnDefinitions✅[Column1]◀️    """)]
    public void WithColumnDefinition_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string columnNameCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyForColumnDefinitions = aj5038SettingsNameQuotingPolicy };

        var code = $"""
                    USE MyDb
                    GO

                    CREATE TABLE Table1
                    (
                        {columnNameCode} INT NOT NULL
                    )
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0000 */     [Table1]                                                                           """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0001 */     ▶️AJ5038💛script_0.sql💛💛table reference💛Table1💛[Table1]💛NameQuotingPolicyForTableReferences✅Table1◀️     """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """ /* 0002 */       ▶️AJ5038💛script_0.sql💛💛table reference💛[Table1]💛"Table1"💛NameQuotingPolicyForTableReferences✅[Table1]◀️ """)]
    public void WithTableReference_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string tableReferenceCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyForTableReferences = aj5038SettingsNameQuotingPolicy };

        var code = $"""
                    USE MyDb
                    GO

                    SELECT      Column1
                    FROM        {tableReferenceCode}
                    """;

        Verify(settings, code);
    }

    [Theory]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0000 */     [int]                                                                                  """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0001 */     [NVARCHAR](MAX)                                                                        """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.SquareBracketsRequired, """ /* 0002 */     ▶️AJ5038💛script_0.sql💛💛data type💛NVARCHAR💛[NVARCHAR]💛NameQuotingPolicyForDataTypes✅NVARCHAR◀️(MAX)   """)]
    [InlineData(Aj5038SettingsNameQuotingPolicy.DoubleQuotesRequired, """ /* 0003 */       ▶️AJ5038💛script_0.sql💛💛data type💛NVARCHAR💛"NVARCHAR"💛NameQuotingPolicyForDataTypes✅NVARCHAR◀️(MAX) """)]
    public void WithDataTypeReference_Theory(Aj5038SettingsNameQuotingPolicy aj5038SettingsNameQuotingPolicy, string typeCode)
    {
        var settings = Aj5038Settings.Default with { NameQuotingPolicyForDataTypes = aj5038SettingsNameQuotingPolicy };

        var code = $"""
                    USE MyDb
                    GO

                    DECLARE @myVar {typeCode}
                    """;

        Verify(settings, code);
    }

    [Fact]
    public void WhenTempTable_ThenIgnore()
    {
        var settings = Aj5038Settings.Default with
        {
            NameQuotingPolicyForTableReferences = Aj5038SettingsNameQuotingPolicy.Required,
            NameQuotingPolicyDuringObjectCreation = Aj5038SettingsNameQuotingPolicy.Required
        };

        const string code = """
                            USE MyDb
                            GO

                            CREATE TABLE #MyTable
                            (
                                Id            INT NOT NULL
                            )

                            SELECT * FROM #MyTable
                            """;

        Verify(settings, code);
    }
}
