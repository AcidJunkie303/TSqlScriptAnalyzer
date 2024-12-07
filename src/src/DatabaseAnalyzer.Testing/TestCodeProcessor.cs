using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Testing.Extensions;

namespace DatabaseAnalyzer.Testing;

public sealed partial class TestCodeProcessor
{
    private readonly IDiagnosticDefinitionRegistry _diagnosticRegistry;

    public TestCodeProcessor(IDiagnosticDefinitionRegistry diagnosticRegistry)
    {
        _diagnosticRegistry = diagnosticRegistry;
    }

    // examples:
    //  {{AJ5000¦file.sql¦object-name|||Code here}}
    //  {{AJ5000¦file.sql¦object-name¦insertion1||Code here}}
    //  {{AJ5000¦file.sql¦object-name¦insertion1¦insertion2||Code here}}
    //  {{AJ5000¦file.sql¦object-name¦insertion1¦insertion2¦insertion3||Code here}}
    // please note that the object-name is optional and can be empty. E.g. when the code is not within a CREATE PROCEDURE statement for example
    [GeneratedRegex(@"\{\{(?<id>[^¦]+)¦(?<parts>[^\|]+)\|\|\|(?<code>.*?)\}\}", RegexOptions.Compiled | RegexOptions.Singleline, 1000)]
    private static partial Regex MarkupRegex();

    public TestCode ParseTestCode(string code)
    {
        var issues = new List<IIssue>();

        // locate, parse and remove markup code and extract issues to issues list
        var markupFreeCode = MarkupRegex().Replace(code, match =>
        {
            var id = match.Groups["id"].Value;
            var parts = match.Groups["parts"].Value.Split('¦');
            if (parts.Length < 2)
            {
                throw new InvalidMarkupException("The header, separated by '¦', must contain at least two parts.'");
            }

            var inner = match.Groups["code"].Value;
            if (inner.Length == 0)
            {
                throw new InvalidMarkupException("The header does not contain any code which is enclosed in '|' and '}}'");
            }

            var fileName = parts[0];
            var objectName = parts[1];
            var insertions = parts[2..];

            // tricky part: the code can span across multiple lines
            var (startLineNumber, startColumnNumber) = code.GetLineAndColumnNumber(match.Index);
            var (endLineNumberOffset, endColumnOffset) = inner.GetLineAndColumnIndex(inner.Length - 1);

            var endLineNumber = startLineNumber + endLineNumberOffset;

            var endColumnNumber = endLineNumberOffset == 0
                ? startColumnNumber + endColumnOffset + 1
                : endColumnOffset + 1 + 1;

            var location = SourceSpan.Create(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);
            var issue = Issue.Create(_diagnosticRegistry.GetDefinition(id), fileName, objectName, location, insertions);
            issues.Add(issue);

            return inner;
        });

        return new TestCode(markupFreeCode, issues);
    }
}
