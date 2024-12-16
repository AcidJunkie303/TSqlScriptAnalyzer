using BetterConsoleTables;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Testing;

internal static class TokenVisualizer
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
