using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

// TODO Remove
#pragma warning disable
public static class SqlCodeObjectExtensions
{
    public static IEnumerable<T> GetTopLevelDescendantsOfType<T>(this SqlCodeObject codeObject)
        where T : SqlCodeObject
    {
        return Get(codeObject, true);

        static IEnumerable<T> Get(SqlCodeObject codeObject, bool isStartingNode)
        {
            if (!isStartingNode && codeObject is T t)
            {
                yield return t;
            }
            else
            {
                foreach (var child in codeObject.Children)
                {
                    foreach (var descendant in Get(child, false))
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }

    public static IEnumerable<T> GetDescendantsOfType<T>(this SqlCodeObject codeObject)
        where T : SqlCodeObject
    {
        return GetChildren(codeObject, true);

        static IEnumerable<T> GetChildren(SqlCodeObject codeObject, bool isStartingNode)
        {
            if (!isStartingNode && codeObject is T t)
            {
                yield return t;
            }

            foreach (var child in codeObject.Children)
            {
                foreach (var descendant in GetChildren(child, false))
                {
                    yield return descendant;
                }
            }
        }
    }

    public static IEnumerable<SqlCodeObject> GetDescendants(this SqlCodeObject codeObject)
    {
        return GetChildren(codeObject, true);

        static IEnumerable<SqlCodeObject> GetChildren(SqlCodeObject codeObject, bool isStartingNode)
        {
            if (!isStartingNode)
            {
                yield return codeObject;
            }

            foreach (var child in codeObject.Children)
            {
                foreach (var descendant in GetChildren(child, false))
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

    public static IEnumerable<SqlCodeObject> GetPrecedingSiblings(this SqlCodeObject codeObject)
    {
        var parent = codeObject.Parent;
        if (parent is null)
        {
            yield break;
        }

        var hasPassedCurrentNode = false;

        foreach (var sibling in parent.Children.Reverse())
        {
            if (hasPassedCurrentNode)
            {
                yield return sibling;
            }
            else if (ReferenceEquals(codeObject, sibling))
            {
                hasPassedCurrentNode = true;
            }
        }
    }

    public static IEnumerable<SqlCodeObject> GetSucceedingSiblings(this SqlCodeObject codeObject)
    {
        var parent = codeObject.Parent;
        if (parent is null)
        {
            yield break;
        }

        var hasPassedCurrentNode = false;

        foreach (var sibling in parent.Children)
        {
            if (hasPassedCurrentNode)
            {
                yield return sibling;
            }
            else if (ReferenceEquals(codeObject, sibling))
            {
                hasPassedCurrentNode = true;
            }
        }
    }

    public static SqlCodeObject? TryGetCodeObjectAtPosition(this SqlCodeObject codeObject, int characterIndex)
    {
        var (lineNumber, columnNumber) = codeObject.Sql.GetLineAndColumnNumber(characterIndex);
        return codeObject.TryGetCodeObjectAtPosition(lineNumber, columnNumber);
    }

    public static SqlCodeObject? TryGetCodeObjectAtPosition(this SqlCodeObject codeObject, Location location)
        => codeObject.TryGetCodeObjectAtPosition(location.LineNumber, location.ColumnNumber);

    public static SqlCodeObject? TryGetCodeObjectAtPosition(this SqlCodeObject codeObject, CodeLocation location)
        => codeObject.TryGetCodeObjectAtPosition(location.LineNumber, location.ColumnNumber);

    [SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions")]
    public static SqlCodeObject? TryGetCodeObjectAtPosition(this SqlCodeObject codeObject, int lineNumber, int columnNumber)
    {
        SqlCodeObject? match = null;

        foreach (var child in codeObject.GetDescendantsOfType<SqlCodeObject>())
        {
            if (IsInsideElement(child, lineNumber, columnNumber))
            {
                match = child;
            }
        }

        return match;

        static bool IsInsideElement(SqlCodeObject codeObject, int lineNumber, int columnNumber)
        {
            return (lineNumber >= codeObject.StartLocation.LineNumber)
                   && (columnNumber >= codeObject.StartLocation.ColumnNumber)
                   && (lineNumber <= codeObject.EndLocation.LineNumber)
                   && (columnNumber <= codeObject.EndLocation.ColumnNumber);
        }
    }

    public static string? TryGetFullObjectNameAtPosition(this SqlCodeObject codeObject, string defaultSchemaName, Location location)
        => codeObject.TryGetFullObjectNameAtPosition(defaultSchemaName, location.LineNumber, location.ColumnNumber);

    public static string? TryGetFullObjectNameAtPosition(this SqlCodeObject codeObject, string defaultSchemaName, int lineNumber, int columnNumber)
    {
        var codeObjectFound = codeObject.TryGetCodeObjectAtPosition(lineNumber, columnNumber);
        if (codeObjectFound is null)
        {
            return null;
        }

        var (schemaName, objectName) = codeObjectFound.TryGetSchemaAndObjectName(defaultSchemaName);
        if (schemaName is null && objectName is null)
        {
            return null;
        }

        return schemaName is null
            ? objectName
            : $"{schemaName}.{objectName}";
    }

    public static string? TryGetFullObjectName(this SqlCodeObject codeObject, string defaultSchemaName)
    {
        var (schemaName, objectName) = codeObject.TryGetSchemaAndObjectName(defaultSchemaName);

        if (schemaName is null && objectName is null)
        {
            return null;
        }

        return schemaName is null
            ? objectName
            : $"{schemaName}.{objectName}";
    }

    public static (string? SchemaName, string? ObjectName) TryGetSchemaAndObjectName(this SqlCodeObject codeObject, string defaultSchemaName)
    {
        foreach (var parent in (SqlCodeObject[]) [codeObject, .. codeObject.GetParents()])
        {
            var objectNameParts = TryFindObjectName(parent, defaultSchemaName);
            switch (objectNameParts.Length)
            {
                case 0:
                    continue;
                case 1:
                    return (null, objectNameParts[0]);
                default:
                    return (objectNameParts[0], objectNameParts[1]);
            }
        }

        return default;
    }

    private static string[] TryFindObjectName(SqlCodeObject codeObject, string defaultSchemaName)
    {
        return [""];
        /*
        if (codeObject is SqlNullStatement nullStatement)
        {
            var clrProcedure = nullStatement.TryParseCreateClrStoredProcedureStatement(defaultSchemaName);
            if (clrProcedure is not null)
            {
                return [clrProcedure.SchemaName, clrProcedure.Name];
            }
        }
*/
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

    private static string[] GetSimpleObjectName(SqlIdentifier schema, SqlIdentifier name, string defaultSchemaName)
    {
        var schemaName = schema.Value.NullIfEmptyOrWhiteSpace() ?? defaultSchemaName;
        return [schemaName, name.Value];
    }

    private static string[] GetObjectName(SqlObjectIdentifier objectIdentifier, bool supportsSchema, string defaultSchemaName)
    {
        if (supportsSchema)
        {
            var schemaName = objectIdentifier.SchemaName.Value.NullIfEmptyOrWhiteSpace() ?? defaultSchemaName;
            return [schemaName, objectIdentifier.ObjectName.Value];
        }

        return [objectIdentifier.ObjectName.Value];
    }
}
