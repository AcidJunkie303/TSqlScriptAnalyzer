using System.Diagnostics.CodeAnalysis;
using System.Text;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

// aa
public static class SqlFragmentExtensions
{
    public static CodeRegion GetCodeRegion(this TSqlFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);

        var firstTokenRegion = fragment.ScriptTokenStream[fragment.FirstTokenIndex].GetCodeRegion();
        var lastTokenRegion = fragment.ScriptTokenStream[fragment.LastTokenIndex].GetCodeRegion();

        return CodeRegion.Create(firstTokenRegion.StartLineNumber, firstTokenRegion.StartColumnNumber, lastTokenRegion.EndLineNumber, lastTokenRegion.EndColumnNumber);
    }

    public static IReadOnlyList<TSqlFragment> GetChildren(this TSqlFragment fragment, bool recursive = false, Func<TSqlFragment, bool>? continueBranchRecursionPredicate = null)
        => SqlFragmentChildrenProvider.GetChildren<TSqlFragment>(fragment, recursive, continueBranchRecursionPredicate);

    public static IReadOnlyList<T> GetChildren<T>(this TSqlFragment fragment, bool recursive = false, Func<TSqlFragment, bool>? continueBranchRecursionPredicate = null)
        where T : TSqlFragment
        => SqlFragmentChildrenProvider.GetChildren<T>(fragment, recursive, continueBranchRecursionPredicate);

    public static string GetSql(this TSqlFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);

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
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(script);

        return fragment.TryGetFirstClassObjectName(context.DefaultSchemaName, script.ParsedScript, script.ParentFragmentProvider);
    }

    public static string? TryGetFirstClassObjectName(this TSqlFragment fragment, string defaultSchemaName, TSqlScript script, IParentFragmentProvider parentFragmentProvider)
    {
        ArgumentNullException.ThrowIfNull(parentFragmentProvider);

        foreach (var parent in parentFragmentProvider.GetParents(fragment).Prepend(fragment))
        {
            if (parent is TSqlScript or TSqlBatch) // there's no point of continuing with those fragment types
            {
                return null;
            }

            var databaseName = parent.FindCurrentDatabaseNameAtFragment(script);
            var name = TryGetFirstClassObjectNameCore(parent, defaultSchemaName);
            if (!name.IsNullOrWhiteSpace())
            {
                return $"{databaseName}.{name}";
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
    {
        ArgumentNullException.ThrowIfNull(script);

        return fragment.GetParents(script.ParentFragmentProvider);
    }

    public static IEnumerable<TSqlFragment> GetParents(this TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
    {
        ArgumentNullException.ThrowIfNull(parentFragmentProvider);

        return parentFragmentProvider.GetParents(fragment);
    }

    public static TSqlFragment? GetParent(this TSqlFragment fragment, IScriptModel script)
    {
        ArgumentNullException.ThrowIfNull(script);

        return fragment.GetParent(script.ParentFragmentProvider);
    }

    public static TSqlFragment? GetParent(this TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
    {
        ArgumentNullException.ThrowIfNull(parentFragmentProvider);

        return parentFragmentProvider.GetParent(fragment);
    }

    /// <summary>
    ///     Retrieves the preceding sibling fragments of the specified T-SQL fragment
    ///     within its parent fragment.
    /// </summary>
    /// <param name="fragment">
    ///     The T-SQL fragment for which the previous sibling fragments are to be retrieved.
    /// </param>
    /// <param name="script">
    ///     The <see cref="TSqlScript" /> the fragment originated from.
    /// </param>
    /// <returns>
    ///     An enumerable collection of TSqlFragment objects that represent the previous
    ///     sibling fragments of the specified fragment.
    ///     The order is as the siblings appear in the AST.
    /// </returns>
    public static IEnumerable<TSqlFragment> GetPrecedingSiblings(this TSqlFragment fragment, TSqlScript script)
        => fragment.GetPrecedingSiblings(script.CreateParentFragmentProvider());

    /// <summary>
    ///     Retrieves the preceding sibling fragments of the specified T-SQL fragment
    ///     within its parent fragment.
    /// </summary>
    /// <param name="fragment">
    ///     The T-SQL fragment for which the previous sibling fragments are to be retrieved.
    /// </param>
    /// <param name="parentFragmentProvider">
    ///     An implementation of IParentFragmentProvider that provides parent fragment information
    ///     for the specified T-SQL fragment.
    /// </param>
    /// <returns>
    ///     An enumerable collection of TSqlFragment objects that represent the previous
    ///     sibling fragments of the specified fragment.
    ///     The order is as the siblings appear in the AST.
    /// </returns>
    public static IEnumerable<TSqlFragment> GetPrecedingSiblings(this TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
    {
        var parent = fragment.GetParent(parentFragmentProvider);
        if (parent is null)
        {
            yield break;
        }

        foreach (var sibling in parent.GetChildren(recursive: false))
        {
            if (ReferenceEquals(sibling, fragment))
            {
                yield break;
            }

            yield return sibling;
        }
    }

    public static TSqlFragment? TryGetSqlFragmentAtPosition(this TSqlScript script, TSqlParserToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        return script.TryGetSqlFragmentAtPosition(token.Line, token.Column);
    }

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
            return index >= fragment.StartOffset
                   && index <= fragmentEndOffset;
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

    public static string FindCurrentDatabaseNameAtFragment(this TSqlFragment fragment, TSqlScript script)
        => CurrentDatabaseNameFinder.FindCurrentDatabaseNameAtFragment(fragment, script);
}
