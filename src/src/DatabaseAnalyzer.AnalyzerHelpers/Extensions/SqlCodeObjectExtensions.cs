using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Extensions;

public static class SqlCodeObjectExtensions
{
    public static IEnumerable<T> GetDescendantsOfType<T>(this SqlCodeObject codeObject)
        where T : SqlCodeObject
    {
        return Get(codeObject, isTop: true);

        static IEnumerable<T> Get(SqlCodeObject codeObject, bool isTop)
        {
            if (!isTop && codeObject is T t)
            {
                yield return t;
            }

            foreach (var child in codeObject.Children)
            {
                foreach (var descendant in Get(child, isTop: false))
                {
                    yield return descendant;
                }
            }
        }
    }

    public static IEnumerable<SqlCodeObject> GetParents(this SqlCodeObject codeObject)
    {
        var parent = codeObject.Parent;
        while (parent is not null)
        {
            yield return parent;
            parent = parent.Parent;
        }
    }

    public static (string? SchemaName, string? ObjectName) TryGetSchemaAndObjectName(this SqlCodeObject codeObject, string defaultSchemaName)
    {
        foreach (var parent in codeObject.GetParents())
        {
            var objectNameParts = TryFindObjectName(parent, defaultSchemaName);
            if (objectNameParts.Length > 0)
            {
                return objectNameParts.Length == 0
                    ? (null, objectNameParts[0])
                    : (objectNameParts[0], objectNameParts[1]);
            }
        }

        return default;

        static string[] TryFindObjectName(SqlCodeObject codeObject, string defaultSchemaName)
        {
            return codeObject switch
            {
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
                _ => []
            };
        }

        static string[] GetSimpleObjectName(SqlIdentifier schema, SqlIdentifier name, string defaultSchemaName)
        {
            var schemaName = schema.Value.NullIfEmptyOrWhiteSpace() ?? defaultSchemaName;
            return [schemaName, name.Value];
        }

        static string[] GetObjectName(SqlObjectIdentifier objectIdentifier, bool supportsSchema, string defaultSchemaName)
        {
            if (supportsSchema)
            {
                var schemaName = objectIdentifier.SchemaName.Value.NullIfEmptyOrWhiteSpace() ?? defaultSchemaName;
                return [schemaName, objectIdentifier.ObjectName.Value];
            }

            return [objectIdentifier.ObjectName.Value];
        }
    }
}
