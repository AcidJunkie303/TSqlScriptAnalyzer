using System.Text;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Testing;

internal static class SyntaxTreeVisualizer2
{
    private const int IndentationCharactersPerLevel = 2;

    public static string Visualize(TSqlScript script)
    {
        var traverser = new Traverser();
        traverser.Traverse(script);

        return Visualize(traverser.Nodes);
    }

    private static string Visualize(List<Node> nodes)
    {
        var maxTypeWidth = nodes
            .Select(a => a.TypeName.Length + (a.Level * IndentationCharactersPerLevel))
            .Append(Headers.Type.Length)
            .Max();

        var buffer = new StringBuilder();

        RenderHeaderFooterDivider();
        RenderLine(Headers.Type, Headers.Code, 0);
        RenderHeaderFooterDivider();

        foreach (var node in nodes)
        {
            RenderLine(node.TypeName, node.Code, node.Level);
        }

        RenderHeaderFooterDivider();

        return buffer.ToString();

        void RenderHeaderFooterDivider()
        {
            buffer
                .Append('+')
                .Append('-', maxTypeWidth + 2)
                .Append('+')
                .Append('-', 100)
                .AppendLine();
        }

        void RenderLine(string type, string code, int level)
        {
            var indentationCharCount = level * IndentationCharactersPerLevel;
            buffer
                .Append("| ")
                .Append(' ', indentationCharCount)
                .Append(type)
                .Append(' ', maxTypeWidth - type.Length - indentationCharCount)
                .Append(" | ")
                .AppendLine(code);
        }
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

            var code = fragment.GetSql()
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);

            var node = new Node(currentLevel, code, fragment.GetType().Name);
            Nodes.Add(node);

            foreach (var child in fragment.GetChildren())
            {
                Traverse(child);
            }

            _level--;
        }
    }

    private sealed record Node(int Level, string Code, string TypeName);

    private static class Headers
    {
        public const string Type = "Type";
        public const string Code = "Code";
    }
}
