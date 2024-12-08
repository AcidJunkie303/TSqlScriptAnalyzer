using System.Text;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Testing;

internal static class SyntaxTreeVisualizer
{
    private const int IndentationCharactersPerLevel = 2;

    public static string Visualize(SqlCodeObject codeObject)
    {
        var nodes = new List<Node>();
        Traverse(codeObject, 0, nodes);

        return Visualize(nodes);
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

    private static void Traverse(SqlCodeObject codeObject, int level, List<Node> nodes)
    {
        var codeWithoutNewlineCharacters = codeObject.Sql
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        var node = new Node(level, codeWithoutNewlineCharacters, codeObject.GetType().Name);
        nodes.Add(node);

        foreach (var child in codeObject.Children)
        {
            Traverse(child, level + 1, nodes);
        }
    }

    private sealed record Node(int Level, string Code, string TypeName);

    private static class Headers
    {
        public const string Type = "Type";
        public const string Code = "Code";
    }
}
