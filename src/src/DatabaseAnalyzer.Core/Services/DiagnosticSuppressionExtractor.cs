using System.Text.RegularExpressions;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Extensions;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Core.Services;

internal interface IDiagnosticSuppressionExtractor
{
    IEnumerable<Suppression> ExtractSuppressions(SqlScript script);
}

public sealed partial class DiagnosticSuppressionExtractor : IDiagnosticSuppressionExtractor
{
    private static readonly char[] DiagnosticIdSeparators = [';', ',', ' ', '\t'];

    public IEnumerable<Suppression> ExtractSuppressions(SqlScript script)
        => script.Tokens.SelectMany(Extract);

    [GeneratedRegex(@"#pragma\s+diagnostic\s+((?<disable>(disable))|(?<restore>restore))\s+(?<ids>[A-Za-z0-9, ]+)+(\s+->\s+(?<reason>.*))", RegexOptions.ExplicitCapture, 100)]
    private static partial Regex DiagnosticSuppressionActionFinder();

    private static IEnumerable<Suppression> Extract(Token token)
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

    private static IEnumerable<Suppression> GetSuppressionsFromMatch(Match match, Token token)
    {
        var (lineOffset, columnOffset) = token.Text.GetLineAndColumnIndex(match.Index);
        var lineNumber = token.StartLocation.LineNumber + lineOffset;
        var columnNumber = lineOffset == 0
            ? token.StartLocation.ColumnNumber + match.Index
            : columnOffset + 1;

        var diagnosticIds = match.Groups["ids"].Value.Split(DiagnosticIdSeparators, StringSplitOptions.RemoveEmptyEntries);
        var action = match.Groups["disable"].Success
            ? SuppressionAction.Disable
            : SuppressionAction.Restore;
        var reason = match.Groups["reason"].Value.Trim();

        foreach (var diagnosticId in diagnosticIds)
        {
            yield return new Suppression(diagnosticId, lineNumber, columnNumber, action, reason);
        }
    }
}
