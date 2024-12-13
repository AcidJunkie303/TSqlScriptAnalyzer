using System.Text;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Testing;

internal static class TokenVisualizer
{
    public static string Visualize(SqlCodeObject codeObject) => Visualize(codeObject.Tokens);

    public static string Visualize(IEnumerable<Token> tokens)
    {
        var buffer = new StringBuilder();

        foreach (var token in tokens)
        {
            buffer.AppendLine(token.ToString());
        }

        return buffer.ToString();
    }
}
