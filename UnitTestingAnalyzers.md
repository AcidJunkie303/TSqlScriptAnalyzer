# Unit Testing Analyzers

Unit testing analyzers is pretty simple thanks to the markup extensions.
Based on the example in [Creating Analyzers](CreatingAnalyzers.md), let's create unit tests for this analyzer.

## Markup

The framework allows writing unit tests in a declarative way. For that, it supports some markup extensions. Example:
`▶️AJ5022💛script_0.sql💛💛IF✅PRINT 'tb'◀️`

Markup explanation:
The markup is enclosed in ▶️ and ◀️ and split by ✅ into two sections:

**Left Part**

This part is split by 💛 where the tokens have the following meaning:

| Token # | Meaning                                                                      | Mandatory             |
|:--------|:-----------------------------------------------------------------------------|:----------------------|
| 1       | Diagnostic ID                                                                | Yes                   |
| 2       | Relative script file path                                                    | Yes                   |
| 3       | The full name of the enclosing object name (if any). Pattern: DB.schema.name | Yes, but can be empty |
| 4-n     | The insertion strings                                                        | No                    |

**Right Part**
The right part between ✅ and ◀️ is the actual code region (T-SQL code) which caused the diagnostic issue.

Each token of `▶️AJ5022💛Procedure1.sql💛MyDatabase.dbo.Procedure💛IF✅PRINT 'tb'◀️` explained:

| Token                     | Meaning                                                                                                                                          |
|:--------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------|
| ️AJ5022                   | Diagnostic ID                                                                                                                                    |
| Procedure1.sql            | Relative script file path                                                                                                                        |
| MyDatabase.dbo.Procedure1 | The full name of the enclosing object name (if any). If this code is not within in a procedure, table, view, function etc., this value is empty. |
| IF                        | 1st insertion string                                                                                                                             |
| `PRINT 'tb'`              | The code which caused the issue                                                                                                                  |

TODO:
continue here with actual code