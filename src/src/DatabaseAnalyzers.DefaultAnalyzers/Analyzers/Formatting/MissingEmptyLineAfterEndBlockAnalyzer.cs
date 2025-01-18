using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class MissingEmptyLineAfterEndBlockAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        for (var i = 0; i < script.ParsedScript.ScriptTokenStream.Count; i++)
        {
            var token = script.ParsedScript.ScriptTokenStream[i];
            if (token.TokenType != TSqlTokenType.End)
            {
                continue;
            }


            AnalyzeEndToken(context, script, token, i);
        }
    }

    private int FindNextImmediateCatchOrFinallyToken(IList<TSqlParserToken> tokens, int tokenIndex)
    {
        var tokenCount = 0;
        var immediateCatchOrFinallyTokenAfter = tokens
            .Skip(tokenIndex + 1)
            .TakeWhile(a =>
            {
                var result = a.TokenType == TSqlTokenType.WhiteSpace || a.TokenType == TSqlTokenType.SingleLineComment || a.TokenType == TSqlTokenType.MultilineComment;
                if (result)
                {
                    tokenCount++;
                }

                return result;
            })
            .FirstOrDefault();
    }

    private static void AnalyzeEndToken(IAnalysisContext context, IScriptModel script, TSqlParserToken endToken, int tokenIndex)
    {
        if (IsNextOrNextAfterNextTokenEndOfFileToken())
        {
            return;
        }

        var missing = IsMissingEmptyLineAfter();
        if (!missing)
        {
            return;
        }

        var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(endToken.Line, endToken.Column) ?? DatabaseNames.Unknown;
        var codeRegion = endToken.GetCodeRegion();
        var fullObjectName = script.ParsedScript
            .TryGetSqlFragmentAtPosition(endToken)
            ?.TryGetFirstClassObjectName(context, script);

        context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion);

        bool IsMissingEmptyLineAfter() => GetNewLineCountAfterToken() < 2;

        bool IsNextOrNextAfterNextTokenEndOfFileToken()
        {
            var tokensAfterEndToken = script.ParsedScript.ScriptTokenStream.Skip(tokenIndex + 1).Take(2).ToList();
            if (tokensAfterEndToken.Count == 1)
            {
                if (tokensAfterEndToken[0].TokenType == TSqlTokenType.EndOfFile)
                {
                    return true;
                }
            }

            if (tokensAfterEndToken.Count == 2)
            {
                if (tokensAfterEndToken[0].TokenType != TSqlTokenType.WhiteSpace)
                {
                    return false;
                }

                if (tokensAfterEndToken[1].TokenType == TSqlTokenType.EndOfFile)
                {
                    return true;
                }
            }

            return false;
        }

        int GetNewLineCountAfterToken()
            => script.ParsedScript.ScriptTokenStream
                .Skip(tokenIndex + 1)
                .TakeWhile(t => t.TokenType == TSqlTokenType.WhiteSpace)
                .Sum(a => a.Text.Count(c => c == '\n'));
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5050",
            IssueType.Formatting,
            "Missing empty line after END block",
            "Missing empty line after END block",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
