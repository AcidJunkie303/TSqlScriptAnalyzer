# TSqlScriptAnalyzer

A framework to analyze multiple T-SQL script files

# TODO

### Analyzers not created yet

- ToDo, not yet finished, open point finder. Tags (only opening -> capture until end of line, or open and closing tag
  can be configured)
- Output parameters should be assigned (hard because we need to check all possible execution paths)
- All branches in a conditional structure should not have exactly the same implementation (except 1=1)
- Indices and trigger names should contain the table name and the table schema name
- Stored procedure, functions etc. documentation header analyzer

### Resiliency / Robustness

- A faulty analyzer must not cause the app to crash
- Remove IssueReporter.Report() extension methods. Instead, every script should provide the database name. Passing in
  the IScriptModel is easier but sometime, when the script contains additional USE DATABASE statements, the real
  database name can be a different one
- Every analyzer must handle situations where the database name is not known -> maybe through analyzer base class to
  provide such handling!?
- Script in which the first statement is not USE <Database> should be removed from the scanning by the core itself. Such
  cases should be reported as issues as well. This way we can be sure, that for every statement, it's associated with a
  database.

### Other

- Create HTML report

# Done

### Analyzers created

- Dead code after return, break, continue, throw and goto statements
- Unused label -> dead code
- Object creation without schema name
- missing quotes. configurable for: (Required, NotAllowed, Ignored)
    - column references
    - schema name
    - object name
- Do not create nameless constraints (unique, primary key) which will have a random name when executing. Otherwise,
  schema comparison would yield lots of unnecessary deltas.
- usage banned functions like GETDATE(), use GETUTCDATE() instead etc. make it configurable
- "unsafe" select * finder
- Raiserror finder
- Queries that use "TOP" should have an "ORDER BY"
- sp_executeSql can be used with parameters, so check for it -> improvement
- invoked stored procedure or function not found

### Other

- Create smart settings implementation so IDiagnosticSettingsProvider is not used anymore. Instead, rely on
  IRawSettings<out TSettings> and the type constraints to make it dynamic.
- CodeRegion should only contain two properties: Begin and End. Both of them are of type CodeLocation.
  CodeLocation have the following two properties: Line and Column

### Resiliency / Robustness

- *none*