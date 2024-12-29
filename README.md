# TSqlScriptAnalyzer

A framework to analyze multiple T-SQL script files

# Analyzers not created yet

- IIssueCollector should be on the IScriptModel. This way, we don't need to specify the relative script file path
- Dead code after return statement
- Unused label -> dead code
- sp_executeSql can be used with parameters, so check for it -> improvement
- ToDo, not yet finished, open point finder
- raiseerror finder
- select *
- table creation where constraints are not separated by comma
- invoked stored procedure or function not found
- object creation without schema name
- object creation without quotes
- Output parameters should be assigned (hard because we need to check all possible execution paths)
- Queries that use "TOP" should have an "ORDER BY"
- All branches in a conditional structure should not have exactly the same implementation
-

# Resiliency / Robustness

- A faulty analyzer must not cause the app to crash
- remove IssueReporter.Report() extension methods. Instead, every script should provide the database name
- Every analyzer must handle situations where the database name is not known -> maybe through analyzer base class to
  provide such handling!?
