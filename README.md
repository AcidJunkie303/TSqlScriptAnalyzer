# TSqlScriptAnalyzer

An framework to analyze multiple T-SQL script files

# Type Recognition

## Types not handled by the SMO parser

| Object Type                 | How to get information                                                              |
|:----------------------------|:------------------------------------------------------------------------------------|
| Create CLR stored procedure | Use extension method `SqlNullStatement.TryParseCreateClrStoredProcedureStatement()` |

## Types handled by the Parser

- Inline CLR table-valued function