using System.Diagnostics.CodeAnalysis;
using BetterConsoleTables;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public static class SyntaxTreeVisualizer
{
    public static string Visualize(TSqlScript script)
    {
        var traverser = new Traverser();
        script.Accept(traverser);

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

    private sealed class Traverser : TSqlFragmentVisitor
    {
        private readonly HashSet<TSqlFragment> _visited = [];
        private int _level;
        public List<Node> Nodes { get; } = [];

        public override void Visit(TSqlFragment fragment)
        {
            if (!_visited.Add(fragment))
            {
                return;
            }

            var codeRegion = fragment is { StartLine: >= 0, StartColumn: >= 0 }
                ? fragment.GetCodeRegion().ToString()
                : CodeRegion.Create(1, 1, 1, 2).ToString();

            var code = fragment.GetSql()
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);

            var node = new Node(_level, codeRegion, fragment.GetType().Name, code);
            Nodes.Add(node);

            _level++;

            fragment.AcceptChildren(this);

            _level--;
        }
    }

    private sealed record Node(int Level, string CodeRegion, string TypeName, string Code);
}
