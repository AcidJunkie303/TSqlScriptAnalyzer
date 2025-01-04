# About

An extendable analyzer for T-SQL scripts.

# Creating analyzers

Before we dive into the analyzers itself, there are some data structures you need to know first.

### IDiagnosticDefinition

```csharp
public interface IDiagnosticDefinition : IEquatable<IDiagnosticDefinition>
{
    string DiagnosticId { get; } // e.g. AJ1234
    IssueType IssueType { get; } // Warning, Error, Information
    string Title { get; } 
    string MessageTemplate { get; }
    int RequiredInsertionStringCount { get; }
}
```

| Property                     | Description                                                                                                                                | 
|:-----------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------|
| MessageTemplate              | The message template used to report the issue. It can contain insertions strings indices enclosed in curly brackets. E.g. `{0}`, `{1}` etc |
| RequiredInsertionStringCount | The insertion string count required to for this diagnostic to be reported.                                                                 |

### IAnalysisContext

```csharp
public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsProvider DiagnosticSettingsProvider { get; }
    IIssueReporter IssueReporter { get; }
}
```

| Property                    | Description                                                          | 
|:----------------------------|:---------------------------------------------------------------------|
| DefaultSchemaName           | The default schema name. This is provided through the configuration. |
| Scripts                     | All parsed scripts.                                                  |
| ScriptsByDatabaseName       | Scripts grouped by database names.                                   |
| IDiagnosticSettingsProvider | Settings provider. Every diagnostic has it's own configuration.      |
| IIssueReporter              | Use to report diagnostics/issues.                                    |

### IScriptModel

```csharp
public interface IScriptModel
{
    string DatabaseName { get; }
    string RelativeScriptFilePath { get; }
    string Contents { get; }
    TSqlScript ParsedScript { get; }
    IParentFragmentProvider ParentFragmentProvider { get; }
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions { get; }
}
```

| Property               | Description                                                                                                                                                                                      | 
|:-----------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| DatabaseName           | The database name this script is meant to be for. The framework can handle multiple databases. Depending on the path of the script, the framework knows for which database this script used for. |
| RelativeScriptFilePath | The script path relative to the database script root path.                                                                                                                                       |
| Contents               | The bare script contents as string.                                                                                                                                                              |
| ParsedScript           | The parsed script as AST (abstract syntax tree) represented through `Microsoft.SqlServer.TransactSql.ScriptDom.ParsedScript`                                                                     |
| ParentFragmentProvider | Use to get the parent AST node.                                                                                                                                                                  |
| DiagnosticSuppressions | The diagnostic suppressions regions defined in this script through `#pragma diagnostic disable X` and restored through `#pragma diagnostic restore X`                                            |

### IObjectAnalyzer

Base interface for analyzers is `IObjectAnalyzer` provides information about which diagnostic types this analyzer will
report:

```csharp
public interface IObjectAnalyzer
{
    IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; }
}
```

### IScriptAnalyzer

```csharp
public interface IScriptAnalyzer : IObjectAnalyzer
{
    void AnalyzeScript(IAnalysisContext context, IScriptModel script);
}
```

### IGlobalAnalyzer

Global analyzers are not bound to a single script. They can be used to perform analysis in a wider scope which involves
multiple script files.

```csharp

public interface IGlobalAnalyzer : IObjectAnalyzer
{
    void Analyze(IAnalysisContext context);
}
```

# Testing

Unit testing analyzers is pretty simple thanks to the markup extensions.

Let's start with an example. We have an analyzer which checks if there are more than 2 string concatenations.
The test code `SET @MyVar = N'a' + N'b' + N'c' + N'd'` violates this rule because it performs 3 concatenations:

- a + b
- result of (a + b) + c
- result of ((a + b) + c) + d

To test whether the analyzer should report an issue, the following markup can be used:
`SET @MyVar = ▶️AJ5001💛Procedure1.sql💛MyDatabase.dbo.Procedure1💛2✅N'a' + N'b' + N'c' + N'd'◀️`

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

Example 1:
Analyzer which checks for banned data types for variables:

Analyzer which checks if there are more than
`SET @MyVar = ▶️AJ5001💛Procedure1.sql💛MyDatabase.dbo.Procedure1💛2✅N'a' + N'b' + N'c' + N'd'◀️`

| Token                       | Meaning                                                                                                                                                                      |
|:----------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AJ5001                      | Diagnostic ID                                                                                                                                                                |
| Procedure1.sql              | Relative script file path                                                                                                                                                    |
| MyDatabase.dbo.Procedure1   | The full name of the enclosing object name (if any). Pattern: DB.schema.name. If this code is not within in a procedure, table, view, function etc., this value is undefined |
| 2                           | 1st insertion string                                                                                                                                                         |
| `N'a' + N'b' + N'c' + N'd'` | The code which caused the issue                                                                                                                                              |

Example 1:
`SET @MyVar = ▶️AJ5001💛Procedure1.sql💛💛2✅N'a' + N'b' + N'c' + N'd'◀️`

| Token                       | Meaning                                                                                                                                                                      |
|:----------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AJ5001                      | Diagnostic ID                                                                                                                                                                |
| Procedure1.sql              | Relative script file path                                                                                                                                                    |
| MyDatabase.dbo.Procedure1   | The full name of the enclosing object name (if any). Pattern: DB.schema.name. If this code is not within in a procedure, table, view, function etc., this value is undefined |
| 2                           | 1st insertion string                                                                                                                                                         |
| `N'a' + N'b' + N'c' + N'd'` | The code which caused the issue                                                                                                                                              |
