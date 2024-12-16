using BetterConsoleTables;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Testing;

internal static class SyntaxTreeVisualizer
{
    public static string Visualize(TSqlScript script)
    {
        var traverser = new Traverser();
        traverser.Traverse(script);

        return Visualize(traverser.Nodes);
    }

    private static string Visualize(List<Node> nodes)
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

        foreach (var node in nodes)
        {
            var typeNameWithIndentation = new string(' ', node.Level * 2) + node.TypeName;
            var code = node.Code.IsNullOrWhiteSpace() ? $"¦{node.Code}¦" : node.Code;
            table.AddRow(typeNameWithIndentation, node.CodeRegion, code);
        }

        return table.ToString();
    }

    private struct Traverser
    {
        private int _level;

        public Traverser()
        {
            _level = 0;
        }

        public List<Node> Nodes { get; } = [];

        public void Traverse(TSqlFragment fragment)
        {
            var currentLevel = _level++;

            var codeRegion = fragment.StartLine >= 0 && fragment.StartColumn >= 0
                ? fragment.GetCodeRegion().ToString()
                : "Unknown";

            var code = fragment.GetSql()
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);

            var node = new Node(currentLevel, codeRegion, fragment.GetType().Name, code);
            Nodes.Add(node);

            foreach (var child in fragment.GetChildren())
            {
                Traverse(child);
            }

            _level--;
        }
    }

    private sealed record Node(int Level, string CodeRegion, string TypeName, string Code);
}
