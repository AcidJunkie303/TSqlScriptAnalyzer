# Unit Testing Analyzers

Unit testing analyzers is pretty simple thanks to the markup extensions.
Based on the example in [Creating-Analyzers](Creating-Analyzers.md), let's create unit tests for this analyzer.

## Markup

The framework allows writing unit tests in a declarative way. For that, it supports some markup extensions. Example:
`▶️AJ5022💛script_0.sql💛💛IF✅PRINT 'tb'◀️`

Markup explanation:
The markup is enclosed in ▶️ and ◀️ and split by ✅ into two sections:

These emoji characters look nice in the Rider IDE as well as Visual Studio Code. Unfortunately, Visual Studio doesn't do
a good job in drawing colored emojis :(

**Left Section**

This part is split by 💛 where the tokens have the following meaning:

| Token # | Meaning                                                                      | Mandatory             |
|:--------|:-----------------------------------------------------------------------------|:----------------------|
| 1       | Diagnostic ID                                                                | Yes                   |
| 2       | Relative script file path                                                    | Yes                   |
| 3       | The full name of the enclosing object name (if any). Pattern: DB.schema.name | Yes, but can be empty |
| 4-n     | The insertion strings                                                        | No                    |

**Right Section**
The right part between ✅ and ◀️ is the actual code region (T-SQL code) which caused the diagnostic issue.

Each token of `▶️AJ5022💛Procedure1.sql💛MyDatabase.dbo.Procedure💛IF✅PRINT 'tb'◀️` explained:

| Token                     | Meaning                                                                                                                                          |
|:--------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------|
| ️AJ5022                   | Diagnostic ID                                                                                                                                    |
| Procedure1.sql            | Relative script file path                                                                                                                        |
| MyDatabase.dbo.Procedure1 | The full name of the enclosing object name (if any). If this code is not within in a procedure, table, view, function etc., this value is empty. |
| IF                        | 1st insertion string                                                                                                                             |
| PRINT 'tb'                | The code which caused the issue                                                                                                                  |

## Base Classes

The framework also provides base classes to unit test script analyzers as well as global analyzers. Those are
`ScriptAnalyzerTestsBase` and `GloablAnalyzerTestsBase`.

## Unit Test Class

```csharp
public sealed class MissingBeginEndAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<MissingBeginEndAnalyzer>(testOutputHelper)
{
    private static readonly Aj5022Settings NoBeginEndRequiredSettings = new(IfRequiresBeginEndBlock: false, WhileRequiresBeginEndBlock: false);
    private static readonly Aj5022Settings BeginEndRequiredSettings = new(IfRequiresBeginEndBlock: true, WhileRequiresBeginEndBlock: true);

    [Fact]
    public void WithIfElse_WithNoBeginEndRequired_WhenNotUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                                PRINT 'tb'
                            ELSE
                                PRINT '303'
                            """;

        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithIfElse_WithNoBeginEndRequired_WhenUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 'tb'
                            END
                            ELSE
                            BEGIN
                                PRINT '303'
                            END
                            """;

        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithNoBeginEndRequired_WhenUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                            BEGIN
                                PRINT 'tb'
                            END
                            """;
        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithNoBeginEndRequired_WhenNotUsingBeginEndThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                                PRINT 'tb-303'
                            """;
        Verify(NoBeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithIfElse_WithBeginEndRequired_WhenNotUsingBeginEnd_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                                ▶️AJ5022💛script_0.sql💛💛IF✅PRINT 'tb'◀️
                            ELSE
                                ▶️AJ5022💛script_0.sql💛💛ELSE✅PRINT '303'◀️
                            """;
        Verify(BeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithBeginEndRequired_WhenUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            IF (1=1)
                            BEGIN
                                PRINT 'tb'
                            END
                            ELSE
                            BEGIN
                                PRINT '303'
                            END
                            """;
        Verify(BeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithWhile_WithBeginEndRequired_WhenNotUsingBeginEnd_ThenDiagnose()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                                ▶️AJ5022💛script_0.sql💛💛WHILE✅PRINT 'tb-303'◀️
                            """;
        Verify(BeginEndRequiredSettings, code);
    }

    [Fact]
    public void WithIfElse_WithBeginEndRequired_WhenUsingBeginEnd_ThenOk()
    {
        const string code = """
                            USE MyDb
                            GO

                            WHILE (1=1)
                            BEGIN
                                PRINT 'tb-303'
                            END
                            """;
        Verify(BeginEndRequiredSettings, code);
    }
}
```

When executing any unit test, the test output window will provide you with the abstract syntax tree as well as the
parsed tokens:

```
==========================================
= Syntax Tree of script script_0.sql
==========================================
+-------------------------------------+-----------------+---------------------------------------------------------+
| Type                                | Region          | Contents                                                |
+-------------------------------------+-----------------+---------------------------------------------------------+
| TSqlScript                          | (1,1) - (5,19)  | USE MyDb\r\nGO\r\n\r\nWHILE (1=1)\r\n    PRINT 'tb-303' |
|   TSqlBatch                         | (1,1) - (1,9)   | USE MyDb                                                |
|     UseStatement                    | (1,1) - (1,9)   | USE MyDb                                                |
|       Identifier                    | (1,5) - (1,9)   | MyDb                                                    |
|   TSqlBatch                         | (4,1) - (5,19)  | WHILE (1=1)\r\n    PRINT 'tb-303'                       |
|     WhileStatement                  | (4,1) - (5,19)  | WHILE (1=1)\r\n    PRINT 'tb-303'                       |
|       BooleanParenthesisExpression  | (4,7) - (4,12)  | (1=1)                                                   |
|         BooleanComparisonExpression | (4,8) - (4,11)  | 1=1                                                     |
|           IntegerLiteral            | (4,8) - (4,9)   | 1                                                       |
|           IntegerLiteral            | (4,10) - (4,11) | 1                                                       |
|       PrintStatement                | (5,5) - (5,19)  | PRINT 'tb-303'                                          |
|         StringLiteral               | (5,11) - (5,19) | 'tb-303'                                                |
+-------------------------------------+-----------------+---------------------------------------------------------+




==========================================
= Tokens of script script_0.sql
==========================================
+-------+--------------------+-----------------+----------+
| Index | Type               | Region          | Contents |
+-------+--------------------+-----------------+----------+
|     0 | Use                | (1,1) - (1,4)   | USE      |
|     1 | WhiteSpace         | (1,4) - (1,5)   | ¦ ¦      |
|     2 | Identifier         | (1,5) - (1,9)   | MyDb     |
|     3 | WhiteSpace         | (1,9) - (2,1)   | \r\n     |
|     4 | Go                 | (2,1) - (2,3)   | GO       |
|     5 | WhiteSpace         | (2,3) - (3,1)   | \r\n     |
|     6 | WhiteSpace         | (3,1) - (4,1)   | \r\n     |
|     7 | While              | (4,1) - (4,6)   | WHILE    |
|     8 | WhiteSpace         | (4,6) - (4,7)   | ¦ ¦      |
|     9 | LeftParenthesis    | (4,7) - (4,8)   | (        |
|    10 | Integer            | (4,8) - (4,9)   | 1        |
|    11 | EqualsSign         | (4,9) - (4,10)  | =        |
|    12 | Integer            | (4,10) - (4,11) | 1        |
|    13 | RightParenthesis   | (4,11) - (4,12) | )        |
|    14 | WhiteSpace         | (4,12) - (5,1)  | \r\n     |
|    15 | WhiteSpace         | (5,1) - (5,5)   | ¦    ¦   |
|    16 | Print              | (5,5) - (5,10)  | PRINT    |
|    17 | WhiteSpace         | (5,10) - (5,11) | ¦ ¦      |
|    18 | AsciiStringLiteral | (5,11) - (5,19) | 'tb-303' |
|    19 | EndOfFile          | (5,19) - (5,19) | ¦¦       |
+-------+--------------------+-----------------+----------+

1 issue reported:
AJ5022    CodeRegion="(5,5) - (5,19)"    FullObjectName="script_0.sql"   Insertions="WHILE
```

Therefore, before implementing any analyzer logic, it's worth creating the unit tests first and check the AST or token
list.
