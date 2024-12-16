using System.Diagnostics.CodeAnalysis;
using System.Text;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlFragmentExtensions
{
    public static CodeRegion GetCodeRegion(this TSqlFragment fragment)
    {
        var firstTokenRegion = fragment.ScriptTokenStream[fragment.FirstTokenIndex].GetCodeRegion();
        var lastTokenRegion = fragment.ScriptTokenStream[fragment.LastTokenIndex].GetCodeRegion();

        return CodeRegion.Create(firstTokenRegion.StartLineNumber, firstTokenRegion.StartColumnNumber, lastTokenRegion.EndLineNumber, lastTokenRegion.EndColumnNumber);
    }

    public static IReadOnlyList<TSqlFragment> GetChildren(this TSqlFragment fragment, bool recursive = false)
        => SqlFragmentChildProvider.GetChildren<TSqlFragment>(fragment, recursive);

    public static IReadOnlyList<T> GetChildren<T>(this TSqlFragment fragment, bool recursive = false)
        where T : TSqlFragment
        => SqlFragmentChildProvider.GetChildren<T>(fragment, recursive);

    public static string GetSql(this TSqlFragment fragment)
    {
        var tokens = fragment.ScriptTokenStream;
        var startIndex = fragment.FirstTokenIndex;
        var endIndex = fragment.LastTokenIndex;
        var sb = new StringBuilder();

        startIndex = Math.Max(startIndex, 0);
        endIndex = Math.Min(endIndex, tokens.Count - 1);

        for (var i = startIndex; i <= endIndex; i++)
        {
            sb.Append(tokens[i].Text);
        }

        return sb.ToString();
    }

    public static string? TryGetFirstClassObjectName(this TSqlFragment fragment, IAnalysisContext context, IScriptModel script)
        => fragment.TryGetFirstClassObjectName(context.DefaultSchemaName, script.ParentFragmentProvider);

    public static string? TryGetFirstClassObjectName(this TSqlFragment fragment, string defaultSchemaName, IParentFragmentProvider parentFragmentProvider)
    {
        foreach (var parent in parentFragmentProvider.GetParents(fragment).Prepend(fragment))
        {
            var name = TryGetFirstClassObjectNameCore(parent, defaultSchemaName);
            if (!name.IsNullOrWhiteSpace())
            {
                return name;
            }
        }

        return null;
    }

    private static string? TryGetFirstClassObjectNameCore(TSqlFragment fragment, string defaultSchemaName)
        => fragment switch
        {
            CreateProcedureStatement s => s.ProcedureReference.Name.GetConcatenatedTwoPartObjectName(defaultSchemaName),
            CreateFunctionStatement s => s.Name.GetConcatenatedTwoPartObjectName(defaultSchemaName),
            CreateTableStatement s => s.SchemaObjectName.GetConcatenatedTwoPartObjectName(defaultSchemaName),
            CreateIndexStatement s => s.Name.Value,
            CreateViewStatement s => s.SchemaObjectName.GetConcatenatedTwoPartObjectName(defaultSchemaName),
            _ => null
        };

    public static IEnumerable<TSqlFragment> GetParents(this TSqlFragment fragment, IScriptModel script)
        => fragment.GetParents(script.ParentFragmentProvider);

    public static IEnumerable<TSqlFragment> GetParents(this TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
        => parentFragmentProvider.GetParents(fragment);

    public static TSqlFragment? GetParent(this TSqlFragment fragment, IScriptModel script)
        => fragment.GetParent(script.ParentFragmentProvider);

    public static TSqlFragment? GetParent(this TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
        => parentFragmentProvider.GetParent(fragment);

    public static TSqlFragment? TryGetSqlFragmentAtPosition(this TSqlScript script, TSqlParserToken token)
        => script.TryGetSqlFragmentAtPosition(token.Line, token.Column);

    public static TSqlFragment? TryGetSqlFragmentAtPosition(this TSqlScript script, int index)
    {
        TSqlFragment? match = null;

        foreach (var child in script.GetChildren(recursive: true))
        {
            if (IsIndexInsideFragment(index, child))
            {
                // get the last possible match
                match = child;
            }
        }

        return match;

        static bool IsIndexInsideFragment(int index, TSqlFragment fragment)
        {
            var fragmentEndOffset = fragment.StartOffset + fragment.FragmentLength;
            return (index >= fragment.StartOffset)
                   && (index <= fragmentEndOffset);
        }
    }

    [SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions")]
    public static TSqlFragment? TryGetSqlFragmentAtPosition(this TSqlScript script, int lineNumber, int columnNumber)
    {
        TSqlFragment? match = null;

        foreach (var child in script.GetChildren(recursive: true))
        {
            if (IsInsideElement(child, lineNumber, columnNumber))
            {
                // get the last possible match
                match = child;
            }
        }

        return match;

        static bool IsInsideElement(TSqlFragment fragment, int lineNumber, int columnNumber)
        {
            var codeRegion = fragment.GetCodeRegion();
            return codeRegion.IsAround(lineNumber, columnNumber);
        }
    }
}
