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

| Property                     | Description                                                                                                                                 | 
|:-----------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------|
| MessageTemplate              | The message template used to report the issue. It can contain insertions strings indices enclosed in curly brackets. E.g. `{0}`, `{1}` etc. |
| RequiredInsertionStringCount | The insertion string count required to for this diagnostic to be reported.                                                                  |

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
| ParsedScript           | The parsed script as AST (abstract syntax tree) represented through `Microsoft.SqlServer.TransactSql.ScriptDom.ParsedScript`.                                                                    |
| ParentFragmentProvider | Use to get the parent AST node.                                                                                                                                                                  |
| DiagnosticSuppressions | The diagnostic suppressions regions defined in this script through `#pragma diagnostic disable X` and restored through `#pragma diagnostic restore X`.                                           |

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

### Settings

Analyzers can have settings. They are defined in a common JSON project analyzer configuration file.
The framework enforces you to have two settings classes:

- A raw settings class which gets deserialized from JSON.
- Final settings class which is created from the raw settings class.

This distinction is useful for the following cases:

- Settings validation.
- Expensive and/or complicated transformation (e.g. regular expression string to `Regex` object).

```csharp
public interface ISettings
{
    static abstract string DiagnosticId { get; }
}

public interface ISettings<out TSettings> : ISettings
    where TSettings : class
{
    static abstract TSettings Default { get; }
}
```

```csharp
public interface IRawSettings<out TSettings>
    where TSettings : class
{
    TSettings ToSettings();
}
```

Settings are not defined per analyzer though. They are defined per diagnostic ID.
Example is for a settings implementation can be found in the next section

# Walkthrough

We want to create an analyzer which ensures that the branches of `IF` and `WHILE` are enclosed in `BEGIN` and `END`.
We also want to make it configurable. The settings contain the information whether `BEGIN/END` is required for `IF` and
`WHILE` statements.

## Settings

All we need to do is to create the following two classes:

```csharp
internal sealed class Aj5022SettingsRaw : IRawSettings<Aj5022Settings>
{
    public bool IfRequiresBeginEndBlock { get; set; }
    public bool WhileRequiresBeginEndBlock { get; set; }

    public Aj5022Settings ToSettings() => new
    (
        IfRequiresBeginEndBlock,
        WhileRequiresBeginEndBlock
    );
}

internal sealed record Aj5022Settings(
    bool IfRequiresBeginEndBlock,
    bool WhileRequiresBeginEndBlock
) : ISettings<Aj5022Settings>
{
    public static Aj5022Settings Default { get; } = new(IfRequiresBeginEndBlock: true, WhileRequiresBeginEndBlock: true);
    public static string DiagnosticId => "AJ5022";
}
```

The framework will extract this information and provides it to the analyzer through
`IAnalysisContext.DiagnosticSettingsProvider`.

Important:
Even if the project configuration doesn't contain settings for this diagnostics, it will instantiate the raw class with
default property values.

## Analyzer Code

Barebone implementation:

```csharp
public sealed class MissingBeginEndAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5022Settings>();
        // analysis code will come here
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5022",
            IssueType.Formatting,
            "Missing BEGIN/END blocks",
            "The children of '{0}' should be enclosed in BEGIN/END blocks.",
            ["Statement name"], // describes the insertion strings used above
            new Uri("https://link.to.the.issue")
        );
    }
}
```

Check out the class `DiagnosticDefinition`. The message template (4th property) contains one insertion string. This will
be the name of the statement (`IF` or `WHILE`).

Let's have a look at the analyzer core implementation:

```csharp
public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
{
    var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5022Settings>();

    if (settings.WhileRequiresBeginEndBlock)
    {
        foreach (var statement in script.ParsedScript.GetChildren<WhileStatement>(recursive: true))
        {
            AnalyzeWhileStatement(context, script, statement);
        }
    }

    if (settings.IfRequiresBeginEndBlock)
    {
        foreach (var statement in script.ParsedScript.GetChildren<IfStatement>(recursive: true))
        {
            AnalyzeIfStatement(context, script, statement.ThenStatement, "IF");
            if (statement.ElseStatement is not null)
            {
                AnalyzeIfStatement(context, script, statement.ElseStatement, "ELSE");
            }
        }
    }
}

private static void AnalyzeWhileStatement(IAnalysisContext context, IScriptModel script, WhileStatement statement)
{
    if (statement.Statement is BeginEndBlockStatement)
    {
        return;
    }

    Report(context, script, statement.Statement, "WHILE");
}

private static void AnalyzeIfStatement(IAnalysisContext context, IScriptModel script, TSqlStatement statement, string statementName)
{
    if (statement is BeginEndBlockStatement)
    {
        return;
    }

    Report(context, script, statement, statementName);
}

private static void Report(IAnalysisContext context, IScriptModel script, TSqlFragment fragmentToReport, string statementName)
{
    var fullObjectName = fragmentToReport.TryGetFirstClassObjectName(context, script);
    var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragmentToReport) ?? DatabaseNames.Unknown;
    context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, fragmentToReport.GetCodeRegion(), statementName);
}
```

Pretty easy isn't it? Well, you need to know the AST classes of course...

Let's have a look at the Report method:

```csharp
private static void Report(IAnalysisContext context, IScriptModel script, TSqlFragment fragmentToReport, string statementName)
{
    var fullObjectName = fragmentToReport.TryGetFirstClassObjectName(context, script);
    var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(fragmentToReport) ?? DatabaseNames.Unknown;
    context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, fragmentToReport.GetCodeRegion(), statementName);
}
```

```csharp
var fullObjectName = statement.TryGetFirstClassObjectName(context, script);
```

The code above uses an extension method `TryGetFirstClassObjectName` to find the first parent in the AST which is a
function, stored procedure, table etc. If such an element is found, the method returns the full object name like
`MyDatabase.dbo.Procedure1`. Otherwise, it will return null.

```csharp
var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(statement) ?? DatabaseNames.Unknown;
```

T-SQL scripts can have `USE` statements to bind to a specific database. The extension method
`TryFindCurrentDatabaseNameAtFragment` tries to find the current database name at the location of the statement. If
there's no preceding `USE` statement, this method will return `null`

```csharp
context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, statement.Statement.GetCodeRegion(), "WHILE");
```

`WHILE` is the one and only insertion string we provide.

Let's continue with [Unit Testing Analyzers](UnitTestingAnalyzers.md)
