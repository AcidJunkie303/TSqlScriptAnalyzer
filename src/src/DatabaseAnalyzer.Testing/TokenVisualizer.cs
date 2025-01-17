using System.Diagnostics.CodeAnalysis;
using BetterConsoleTables;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public static class TokenVisualizer
{
    public static string Visualize(TSqlFragment fragment) => Visualize(fragment.ScriptTokenStream);

    public static string Visualize(IEnumerable<TSqlParserToken> tokens)
    {
        var table = new Table(Alignment.Left)
        {
            Config = TableConfiguration.MySql()
        };

        table.Config.hasInnerRows = false;

        table
            .AddColumn("Type")
            .AddColumn("Region")
            .AddColumn("Contents");

        foreach (var token in tokens)
        {
            var text = token.Text.EmptyIfNull()
                .Replace("\r\n", "\\r\\n", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);
            text = text.IsNullOrWhiteSpace() ? $"¦{text}¦" : text;
            table.AddRow(token.TokenType, token.GetCodeRegion(), text);
        }

        return table.ToString();
    }
}
