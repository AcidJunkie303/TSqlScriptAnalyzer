# About

An extendable analyzer for T-SQL scripts.

The main executable is `DatabaseAnalyzer.App.exe`, which is a command-line tool that can be used to analyze T-SQL
scripts. The analyzer can be extended by creating custom analyzers, which are implemented as .NET assemblies.

# Command Line

To get help on the command line options, run the following command: `DatabaseAnalyzer.App.exe analyze --help`. This will
show you the following output:

```
C:\DatabaseAnalyzer>DatabaseAnalyzer.App.exe analyze --help
Description:
  Analyze a project

Usage:
  DatabaseAnalyzer.App analyze [options]

Options:
  -f, --file <file> (REQUIRED)                         The settings file.
  -crt, --console-report-type <console-report-type>    The console report types to render. Following values are valid:
                                                       'Json','JsonSummary', 'Html' or 'Text'. [default: Text]
  -h, --html-report-file-path <html-report-file-path>  The path of file to render the html report to.
  -hrt, --html-report-theme <Dark|Light>               The theme for the HTML report. Default is `Dark`
  -j, --json-report-file-path <json-report-file-path>  The path of file to render the json report to.
  -s, --json-summary-report-file-path                  The path of file to render the json summary report to.
  <json-summary-report-file-path>
  -t, --text-report-file-path <text-report-file-path>  The path of file to render the text report to.
  -l, --log-file-path <log-file-path>                  The path of the log file.
  -m, --minimum-log-level                              The minimum log level. Default is 'Information'.
  <Debug|Error|Fatal|Information|Verbose|Warning>
  -?, -h, --help                                       Show help and usage information
```

# Project File

The project file is a JSON file that contains the settings for the analyzer like file locations, custom analyzer
settings etc.:

```json
{
  "Analyzers": [
    {
      "Name": "MyAnalyzer",
      "AssemblyPath": "MyAnalyzer.dll"
    }
  ]
}
```

# Creating Custom Analyzers

- [1. Creating Analyzers.md](1.Creating-Analyzers.md)
- [2. Unit-Testing Analyzers.md](2.Unit-Testing-Analyzers.md)
- [3. Integrating Analyzers.md](3.Integrating-Analyzers.md)
- [4.Project-File-Reference.md](4.Project-File-Reference.md)
- [5.Issue-Suppression.md](5.Issue-Suppression.md)
- 