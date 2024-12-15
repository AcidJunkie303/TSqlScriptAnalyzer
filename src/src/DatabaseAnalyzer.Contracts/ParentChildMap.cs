using System.Collections.Frozen;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts;

public sealed record ParentChildMap(
    FrozenDictionary<TSqlFragment, IReadOnlyList<TSqlFragment>> ChildrenByParent,
    FrozenDictionary<TSqlFragment, TSqlFragment?> ParentByChild,
    TSqlFragment? Root
);
