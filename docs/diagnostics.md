<!--
    The contents of this file are generated using the tests
    in the "DatabaseAnalyzers.DefaultAnalyzers.InfrastructureTasks" project.
    Do not change this file manually!
-->

# Overview of all diagnostics

| Diagnostic Id                   | Title                                                                   | Type         |
|---------------------------------|-------------------------------------------------------------------------|--------------|
| [AJ5000](diagnostics/AJ5000.md) | Dynamic SQL                                                             | Warning      |
| [AJ5001](diagnostics/AJ5001.md) | Excessive string concatenations                                         | Warning      |
| [AJ5002](diagnostics/AJ5002.md) | Unicode/ASCII string mix                                                | Warning      |
| [AJ5003](diagnostics/AJ5003.md) | Wrong database name in 'USE' statement                                  | Warning      |
| [AJ5004](diagnostics/AJ5004.md) | Open Item                                                               | Information  |
| [AJ5006](diagnostics/AJ5006.md) | Usage of banned data type                                               | Warning      |
| [AJ5007](diagnostics/AJ5007.md) | Multiple empty lines                                                    | Formatting   |
| [AJ5008](diagnostics/AJ5008.md) | Tab character                                                           | Formatting   |
| [AJ5009](diagnostics/AJ5009.md) | Object creation without `OR ALTER` clause                               | Warning      |
| [AJ5010](diagnostics/AJ5010.md) | Missing blank-space                                                     | Formatting   |
| [AJ5011](diagnostics/AJ5011.md) | Unreferenced parameter                                                  | Warning      |
| [AJ5012](diagnostics/AJ5012.md) | Unreferenced variable                                                   | Warning      |
| [AJ5013](diagnostics/AJ5013.md) | Parameter reference with different casing                               | Warning      |
| [AJ5014](diagnostics/AJ5014.md) | Variable reference with different casing                                | Warning      |
| [AJ5015](diagnostics/AJ5015.md) | Missing Index                                                           | MissingIndex |
| [AJ5016](diagnostics/AJ5016.md) | Missing table alias when more than one table is involved in a statement | Warning      |
| [AJ5017](diagnostics/AJ5017.md) | Missing Index on foreign key column                                     | MissingIndex |
| [AJ5018](diagnostics/AJ5018.md) | Null comparison                                                         | Warning      |
| [AJ5020](diagnostics/AJ5020.md) | Usage of weak hashing algorithm                                         | Warning      |
| [AJ5021](diagnostics/AJ5021.md) | Specific options should not be turned off                               | Warning      |
| [AJ5022](diagnostics/AJ5022.md) | Missing BEGIN/END blocks                                                | Formatting   |
| [AJ5023](diagnostics/AJ5023.md) | Statements should begin on a new line                                   | Formatting   |
| [AJ5024](diagnostics/AJ5024.md) | Multiple variable declaration on same line                              | Formatting   |
| [AJ5026](diagnostics/AJ5026.md) | Table has no primary key                                                | Warning      |
| [AJ5027](diagnostics/AJ5027.md) | Table has no clustered index                                            | Warning      |
| [AJ5028](diagnostics/AJ5028.md) | Semicolon is not necessary                                              | Formatting   |
| [AJ5029](diagnostics/AJ5029.md) | The first statement in a procedure should be 'SET NOCOUNT ON'           | Warning      |
| [AJ5030](diagnostics/AJ5030.md) | Object name violates naming convention                                  | Warning      |
| [AJ5031](diagnostics/AJ5031.md) | Redundant pair of parentheses                                           | Warning      |
| [AJ5032](diagnostics/AJ5032.md) | Non-standard comparison operator                                        | Warning      |
| [AJ5033](diagnostics/AJ5033.md) | Ternary operators should not be nested                                  | Warning      |
| [AJ5034](diagnostics/AJ5034.md) | Set options don't need to be separated by GO                            | Warning      |
| [AJ5035](diagnostics/AJ5035.md) | Dead Code                                                               | Warning      |
| [AJ5036](diagnostics/AJ5036.md) | Unreferenced Label                                                      | Warning      |
| [AJ5037](diagnostics/AJ5037.md) | Object creation without schema name                                     | Formatting   |
| [AJ5038](diagnostics/AJ5038.md) | Object name quoting                                                     | Formatting   |
| [AJ5039](diagnostics/AJ5039.md) | Nameless constraints                                                    | Formatting   |
| [AJ5040](diagnostics/AJ5040.md) | Usage of banned function                                                | Warning      |
| [AJ5041](diagnostics/AJ5041.md) | Usage of 'SELECT *'                                                     | Warning      |
| [AJ5042](diagnostics/AJ5042.md) | Usage of RAISERROR                                                      | Warning      |
| [AJ5043](diagnostics/AJ5043.md) | Missing ORDER BY clause when using TOP                                  | Warning      |
| [AJ5044](diagnostics/AJ5044.md) | Missing Object                                                          | Warning      |
| [AJ5045](diagnostics/AJ5045.md) | Missing empty line before/after GO batch separators                     | Formatting   |
| [AJ5046](diagnostics/AJ5046.md) | Consecutive GO statements                                               | Formatting   |
| [AJ5047](diagnostics/AJ5047.md) | Default Object Creation Comments                                        | Warning      |
| [AJ5049](diagnostics/AJ5049.md) | Object Invocation without explicitly specified schema name              | Warning      |
| [AJ5050](diagnostics/AJ5050.md) | Missing empty line after END block                                      | Formatting   |
| [AJ5051](diagnostics/AJ5051.md) | Unused Index                                                            | Warning      |
| [AJ5052](diagnostics/AJ5052.md) | Index Naming                                                            | Warning      |
| [AJ5053](diagnostics/AJ5053.md) | Usage of 'SELECT *' in existence check                                  | Warning      |
| [AJ5054](diagnostics/AJ5054.md) | Inconsistent Column Data Type                                           | Warning      |
| [AJ5055](diagnostics/AJ5055.md) | Inconsistent Column Name Casing                                         | Warning      |
| [AJ5056](diagnostics/AJ5056.md) | Keyword uses wrong casing                                               | Formatting   |
| [AJ5057](diagnostics/AJ5057.md) | Identifier uses wrong casing                                            | Formatting   |
| [AJ9000](diagnostics/AJ9000.md) | The first statement in a script must be 'USE <DATABASE>'                | Warning      |
| [AJ9001](diagnostics/AJ9001.md) | Missing table alias                                                     | Warning      |
| [AJ9002](diagnostics/AJ9002.md) | Duplicate object creation statement                                     | Error        |
| [AJ9004](diagnostics/AJ9004.md) | Error in script                                                         | Error        |
| [AJ9999](diagnostics/AJ9999.md) | Analyzer error                                                          | Error        |

