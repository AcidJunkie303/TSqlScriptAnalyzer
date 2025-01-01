using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzer.Testing;

internal sealed partial class TestCodeProcessor
{
    private readonly IDiagnosticDefinitionRegistry _diagnosticRegistry;

    public TestCodeProcessor(IDiagnosticDefinitionRegistry diagnosticRegistry)
    {
        _diagnosticRegistry = diagnosticRegistry;
    }

    // examples:
    // █AJ5000░file.sql░object.name██PRINT 303█                -> has object name;     no insertions: [],                  region: "PRINT 303"
    // █AJ5000░file.sql░██PRINT 303█                           -> no object name;      no insertions: [],                  region: "PRINT 303"
    // █AJ5000░file.sql░object.name░██PRINT 303█               -> has object name;     1 insertion: [""],                  region: "PRINT 303"
    // █AJ5000░file.sql░schema.name░hello██PRINT 303█          -> has object name;     1 insertion: ["hello"],             region: "PRINT 303"
    // █AJ5000░file.sql░schema.name░hello░██PRINT 303█         -> has object name;     2 insertion: ["hello", ""],         region: "PRINT 303"
    // █AJ5000░file.sql░schema.name░hello░world██PRINT 303█    -> has object name;     2 insertion: ["hello", "world"],    region: "PRINT 303"
    // █AJ5000░file.sql░░hello░world██PRINT 303█               -> no object name;      2 insertion: ["hello", "world"],    region: "PRINT 303"
    // please note that the object-name is optional and can be empty. E.g. when the code is not within a CREATE PROCEDURE statement for example
    [GeneratedRegex(@"█(?<header>[^█]+)███(?<code>[^█]+)█", RegexOptions.Compiled | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex MarkupRegex();

    public TestCode ParseTestCode(string code)
    {
        var issues = new List<IIssue>();

        var currentCode = code;
        while (true)
        {
            // locate, parse and remove markup code and extract issues to issues list
            var processedCode = RemoveMarkupAndAddIssues(currentCode, issues, _diagnosticRegistry);
            if (processedCode.EqualsOrdinal(currentCode))
            {
                return new TestCode(currentCode, issues);
            }

            currentCode = processedCode;
        }
    }

    private static string RemoveMarkupAndAddIssues(string code, List<IIssue> issues, IDiagnosticDefinitionRegistry diagnosticRegistry)
        => MarkupRegex()
            .Replace(
                count: 1,
                input: code,
                evaluator: match =>
                {
                    var parts = match.Groups["header"].Value
                        .Split('░')
                        .Select(x => x.Trim())
                        .ToList();

                    if (parts.Count < 3)
                    {
                        throw new InvalidMarkupException("The header, separated by '░', must contain at least two parts.'");
                    }

                    var id = parts[0];
                    var fileName = parts[1];
                    var fullObjectName = parts[2].NullIfEmptyOrWhiteSpace();
                    var innerCode = match.Groups["code"].Value;
                    var insertions = parts.Count >= 4
                        ? parts[3..]
                        : [];

                    // tricky part: the code can span across multiple lines
                    var (startLineNumber, startColumnNumber) = code.GetLineAndColumnNumber(match.Index);
                    var (endLineNumberOffset, endColumnOffset) = innerCode.GetLineAndColumnIndex(innerCode.Length - 1);

                    var endLineNumber = startLineNumber + endLineNumberOffset;

                    var endColumnNumber = endLineNumberOffset == 0
                        ? startColumnNumber + endColumnOffset
                        : endColumnOffset + 1;

                    endColumnNumber++; // because it's an offset

                    if (innerCode.EndsWith('\n'))
                    {
                        endColumnNumber--;
                    }

                    var location = CodeRegion.Create(startLineNumber, startColumnNumber, endLineNumber, endColumnNumber);
                    var issue = Issue.Create(diagnosticRegistry.GetDefinition(id), "MyDb", fileName, fullObjectName, location, insertions);
                    issues.Add(issue);

                    return innerCode;
                });
}
