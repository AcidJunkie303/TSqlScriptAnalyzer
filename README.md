# TSqlScriptAnalyzer

A framework to analyze multiple T-SQL script files

# TODO

### Analyzers not created yet

- Output parameters should be assigned (hard because we need to check all possible execution paths)
- All branches in a conditional structure should not have exactly the same implementation (except 1=1)
- Object naming extensions:
    - if the object is tied to a schema, add placeholders to the expression pattern which represent the current schema
    - if the object is tied to a table, add placeholders to the expression pattern which represent the current schema
      and table name
- Stored procedure, functions etc. documentation header analyzer
- index creation on table without specified table schema name
- scripts containing standard headers like:
  `/****** Object: ……. Script Date:`
- unused indices (except FK indices)
- No empty line after GO batch separator
- Only one consecutive GO batch separator
- procedure invocation without schema
- Keywords must be uppercase or lower case (configurable). maybe make the list of keywords configurable -> big though
- table alias with different casing (not done already?)
- undoncitional table or index  creation (not embeded in IF exists check)
- referenced stored procedure not found
- referenced object name casing difference (procedure, table, view, column etc.) also schema name
- table alias naming analyzer (small only etc. -> regex)
- XML or JSON string extraction of banned types -> integrate into banned type analyzer
  
### Resiliency / Robustness

### Other

- write github diagnostic descriptor markdown
- Create HTML report feature

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
- ToDo, not yet finished, open point finder. Tags (only opening -> capture until end of line, or open and closing tag
  can be configured)

### Other

- A faulty analyzer must not cause the app to crash
- Create smart settings implementation so IDiagnosticSettingsProvider is not used anymore. Instead, rely on
  IRawSettings<out TSettings> and the type constraints to make it dynamic.
- CodeRegion should only contain two properties: Begin and End. Both of them are of type CodeLocation.
  CodeLocation have the following two properties: Line and Column
- Scripts which contain errors should not be take part of the analysis
- Remove IssueReporter.Report() extension methods. Instead, every script should provide the database name. Passing in
  the IScriptModel is easier but sometime, when the script contains additional USE DATABASE statements, the real
  database name can be a different one

### Resiliency / Robustness

- *none*
