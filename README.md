# TSqlScriptAnalyzer

A framework to analyze multiple T-SQL script files

# TODO

### Analyzers not created yet

- IIssueCollector should be on the IScriptModel. This way, we don't need to specify the relative script file path
- sp_executeSql can be used with parameters, so check for it -> improvement
- ToDo, not yet finished, open point finder
- raiseerror finder
- select *
- table creation where constraints are not separated by comma
- invoked stored procedure or function not found
- missing quotes. configurable for: (Required, NotAllowed, Ignored)
    - column references
    - schema name
    - object name
- Output parameters should be assigned (hard because we need to check all possible execution paths)
- Queries that use "TOP" should have an "ORDER BY"
- All branches in a conditional structure should not have exactly the same implementation
- Do not create nameless constraints (unique, primary key) which will have a random name when executing. Otherwise,
  schema comparison would yield lots of unnecessary deltas.
- indices and trigger names should contain the table name and the table schema name
- create smart settings implementation so IDiagnosticSettingsProvider is not used anymore. Instead rely on
  IRawSettings<out TSettings> and the type constraints to make it dynamic.

### Resiliency / Robustness

- A faulty analyzer must not cause the app to crash
- remove IssueReporter.Report() extension methods. Instead, every script should provide the database name
- Every analyzer must handle situations where the database name is not known -> maybe through analyzer base class to
  provide such handling!?

# Done

### Analyzers not created yet

- Dead code after return, break, continue, throw and goto statements
- Unused label -> dead code
- Object creation without schema name
-

### Resiliency / Robustness

- *none*