using System.Text;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

// TODO: Remove
// ReSharper disable InconsistentNaming
public static class TSqlFragmentExtensions
{
    public static CodeRegion GetCodeRegion(this TSqlFragment fragment)
    {
        var firstTokenRegion = fragment.ScriptTokenStream[fragment.FirstTokenIndex].GetCodeRegion();
        var lastTokenRegion = fragment.ScriptTokenStream[fragment.LastTokenIndex].GetCodeRegion();

        return CodeRegion.Create(firstTokenRegion.StartLineNumber, firstTokenRegion.StartColumnNumber, lastTokenRegion.EndLineNumber, lastTokenRegion.EndColumnNumber);
    }

    public static TSqlFragment? GetParent(this TSqlFragment fragment) => ParentChildMapBuilder2.GetParent(fragment);

    public static IEnumerable<TSqlFragment> GetParents(this TSqlFragment fragment)
    {
        var parent = fragment.GetParent();
        while (parent is not null)
        {
            yield return parent;
            parent = parent.GetParent();
        }
    }

    public static IReadOnlyList<TSqlFragment> GetChildren(this TSqlFragment fragment) => ParentChildMapBuilder2.GetChildren(fragment);

    public static IEnumerable<T> GetDescendantsOfType<T>(this TSqlFragment fragment)
        where T : TSqlFragment
    {
        return GetChildrenCore(fragment, true);

        static IEnumerable<T> GetChildrenCore(TSqlFragment node, bool isStartingNode)
        {
            if (!isStartingNode && node is T t)
            {
                yield return t;
            }

            foreach (var child in node.GetChildren())
            {
                foreach (var descendant in GetChildrenCore(child, false))
                {
                    yield return descendant;
                }
            }
        }
    }

    public static IEnumerable<TSqlFragment> GetDescendants(this TSqlFragment fragment)
    {
        return GetChildrenCore(fragment, true);

        static IEnumerable<TSqlFragment> GetChildrenCore(TSqlFragment node, bool isStartingNode)
        {
            if (!isStartingNode)
            {
                yield return node;
            }

            foreach (var child in node.GetChildren())
            {
                foreach (var descendant in GetChildrenCore(child, false))
                {
                    yield return descendant;
                }
            }
        }
    }

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

    public static string? TryGetFirstClassObjectName(this TSqlFragment fragment, string defaultSchemaName)
    {
        foreach (var parent in fragment.GetParents())
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
    /*
SqlCreateFunctionStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlCreateIndexStatement o => [o.Name.Value],
            SqlCreateLoginFromAsymKeyStatement o => [o.Name.Value],
            SqlCreateLoginFromCertificateStatement o => [o.Name.Value],
            SqlCreateLoginFromWindowsStatement o => [o.Name.Value],
            SqlCreateLoginFromExternalProviderStatement o => [o.Name.Value],
            SqlCreateLoginWithPasswordStatement o => [o.Name.Value],
            SqlCreateProcedureStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlCreateRoleStatement o => [o.Name.Value],
            SqlCreateSchemaStatement o => [o.Name.Value],
            SqlCreateSynonymStatement o => GetObjectName(o.Name, true, defaultSchemaName),
            SqlCreateTableStatement o => GetObjectName(o.Name, true, defaultSchemaName),
            SqlCreateTriggerStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlCreateUserDefinedDataTypeStatement o => GetObjectName(o.Name, true, defaultSchemaName),
            SqlCreateUserDefinedTableTypeStatement o => GetObjectName(o.Name, true, defaultSchemaName),
            SqlCreateUserDefinedTypeStatement o => GetObjectName(o.Name, true, defaultSchemaName),
            SqlCreateUserFromAsymKeyStatement o => [o.Name.Value],
            SqlCreateUserFromCertificateStatement o => [o.Name.Value],
            SqlCreateUserWithImplicitAuthenticationStatement o => [o.Name.Value],
            SqlCreateUserFromLoginStatement o => [o.Name.Value],
            SqlCreateUserFromExternalProviderStatement o => [o.Name.Value],
            SqlCreateUserWithoutLoginStatement o => [o.Name.Value],
            SqlCreateViewStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlCreateLoginStatement o => [o.Name.Value],
            SqlCreateTypeStatement o => GetObjectName(o.Name, true, defaultSchemaName),
            SqlCreateUserStatement o => [o.Name.Value],
            SqlAlterFunctionStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlAlterLoginStatement o => [o.Name.Value],
            SqlAlterProcedureStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlAlterTriggerStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlAlterViewStatement o => GetObjectName(o.Definition.Name, true, defaultSchemaName),
            SqlDropAggregateStatement => ["Unknown"],
            SqlDropDatabaseStatement o => [o.DatabaseNames.First().Value],
            SqlDropDefaultStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropFunctionStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropLoginStatement o => [o.LoginName.Value],
            SqlDropProcedureStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropRuleStatement o => GetObjectName(o.Objects.First(), false, defaultSchemaName),
            SqlDropSchemaStatement o => [o.SchemaName.Value],
            SqlDropSecurityPolicyStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropSequenceStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropSynonymStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropTableStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropTriggerStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropTypeStatement o => GetSimpleObjectName(o.TypeName.SchemaName, o.TypeName.ObjectName, defaultSchemaName),
            SqlDropUserStatement o => [o.UserName.Value],
            SqlDropViewStatement o => GetObjectName(o.Objects.First(), true, defaultSchemaName),
            SqlDropStatement => ["Unknown"],
         */
}
