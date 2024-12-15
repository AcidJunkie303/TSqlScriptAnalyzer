using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Services;

// TODO: remove
#pragma warning disable

public sealed partial class DiagnosticSuppressionExtractor : IDiagnosticSuppressionExtractor
{
    private static readonly char[] DiagnosticIdSeparators = [';', ',', ' ', '\t'];

    public IEnumerable<DiagnosticSuppression> ExtractSuppressions(TSqlScript script)
        => []; // => script.Tokens.SelectMany(Extract);

    [GeneratedRegex(@"#pragma\s+diagnostic\s+((?<disable>(disable))|(?<restore>restore))\s+(?<ids>[A-Za-z0-9, ]+)(\s*-> \s*(?<reason>.*))?", RegexOptions.ExplicitCapture, 100)]
    private static partial Regex DiagnosticSuppressionActionFinder();

    private static IEnumerable<DiagnosticSuppression> Extract(Token token)
    {
        if (!token.IsComment())
        {
            yield break;
        }

        foreach (Match match in DiagnosticSuppressionActionFinder().Matches(token.Text))
        {
            foreach (var suppression in GetSuppressionsFromMatch(match, token))
            {
                yield return suppression;
            }
        }
    }

    private static IEnumerable<DiagnosticSuppression> GetSuppressionsFromMatch(Match match, Token token)
    {
        var (lineOffset, columnOffset) = token.Text.GetLineAndColumnIndex(match.Index);
        var lineNumber = token.StartLocation.LineNumber + lineOffset;
        var columnNumber = lineOffset == 0
            ? token.StartLocation.ColumnNumber + match.Index
            : columnOffset + 1;

        var diagnosticIds = match.Groups["ids"].Value.Split(DiagnosticIdSeparators, StringSplitOptions.RemoveEmptyEntries);

        SuppressionAction action;
        string reason;
        if (match.Groups["disable"].Success)
        {
            action = SuppressionAction.Disable;
            reason = match.Groups["reason"].Value.Trim();
        }
        else
        {
            action = SuppressionAction.Restore;
            reason = string.Empty;
        }

        var location = new CodeLocation(lineNumber, columnNumber);

        foreach (var diagnosticId in diagnosticIds)
        {
            yield return new DiagnosticSuppression(diagnosticId, location, action, reason);
        }
    }
}
