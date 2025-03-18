using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class SqlScriptExtensions
{
    public static IParentFragmentProvider CreateParentFragmentProvider(this TSqlFragment fragment)
        => ParentFragmentProviderFactory.Build(fragment);

    public static IEnumerable<T> GetTopLevelDescendantsOfType<T>(this TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
        where T : TSqlFragment
    {
        ArgumentNullException.ThrowIfNull(fragment);

        var visitor = new GetTopLevelDescendantVisitor<T>(parentFragmentProvider);
        fragment.Accept(visitor);
        return visitor.Nodes;
    }

    public static bool HasStatements(this TSqlScript script)
        => script
            .GetChildren(recursive: false)
            .Count > 0;

    public static bool IsUseDatabaseCalledBeforeAnyOtherStatement(this TSqlScript script)
        => ScriptComplianceChecker.IsUseDatabaseCalledBeforeAnyOtherStatement(script);

    public static string FindCurrentDatabaseNameAt(this TSqlScript script, TSqlParserToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(script);

        return CurrentDatabaseNameFinder.FindCurrentDatabaseNameAtToken(script, token);
    }

    public static string? TryFindCurrentDatabaseNameAtToken(this TSqlScript script, TSqlParserToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(script);

        return CurrentDatabaseNameFinder.TryFindCurrentDatabaseNameAtToken(script, token);
    }

    public static string? TryFindCurrentDatabaseNameAtFragment(this TSqlScript script, TSqlFragment fragment)
        => CurrentDatabaseNameFinder.TryFindCurrentDatabaseNameAtFragment(script, fragment);

    public static string FindCurrentDatabaseNameAtFragment(this TSqlScript script, TSqlFragment fragment)
        => CurrentDatabaseNameFinder.FindCurrentDatabaseNameAtFragment(script, fragment);

    public static string? TryFindCurrentDatabaseNameAtLocation(this TSqlScript script, CodeLocation location)
        => CurrentDatabaseNameFinder.TryFindCurrentDatabaseNameAtLocation(script, location);

    public static string? TryFindCurrentDatabaseNameAtLocation(this TSqlScript script, int line, int column)
        => CurrentDatabaseNameFinder.TryFindCurrentDatabaseNameAtLocation(script, line, column);

    public static string FindCurrentDatabaseNameAtLocation(this TSqlScript script, CodeLocation location)
        => CurrentDatabaseNameFinder.FindCurrentDatabaseNameAtLocation(script, location);

    private sealed class GetTopLevelDescendantVisitor<T> : TSqlFragmentVisitor
        where T : TSqlFragment
    {
        private readonly IParentFragmentProvider _parentFragmentProvider;

        public List<T> Nodes { get; } = [];

        public GetTopLevelDescendantVisitor(IParentFragmentProvider parentFragmentProvider)
        {
            _parentFragmentProvider = parentFragmentProvider;
        }

        public override void Visit(TSqlFragment fragment)
        {
            if (fragment is T node)
            {
                var anyParentIsOfSameType = _parentFragmentProvider.GetParents(fragment).Any(a => a is T);
                if (!anyParentIsOfSameType)
                {
                    Nodes.Add(node);
                    return;
                }

                base.Visit(fragment);
            }
        }
    }
}
