using System.Text.RegularExpressions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static partial class ClrStoredProcedureParser
{
    private const string Template = """
                                    CREATE PROCEDURE dbo.MyProcedure
                                    {{Parameters}}
                                    AS
                                    BEGIN
                                    	SELECT 1
                                    END
                                    """;

    public static SqlCreateClrStoredProcedureStatement? TryParse(SqlNullStatement codeObject, string defaultSchemaName)
    {
        // Inner can be something like:
        //      @input1 NVARCHAR(100),
        //      @input2 INT,
        //      @output NVARCHAR(100) OUTPUT
        //      WITH EXECUTE AS OWNER, SCHEMABINDING, NATIVE_COMPILATION
        //      AS EXTERNAL NAME MyStoredProcAssembly.[StoredProcedures].MyClrStoredProcedure
        var match = CreateStatementFinder().Match(codeObject.Sql);
        if (!match.Success)
        {
            return null;
        }

        var name = match.Groups["name"].Value.Trim();
        var schemaName = match.Groups["schema"].Success
            ? match.Groups["schema"].Value.Trim()
            : defaultSchemaName;
        var isCreateOrAlter = match.Groups["alter"].Success;
        var inner = match.Groups["inner"].Value.Trim();

        // we are not interested in the WITH options.
        inner = WithFinder().Replace(inner, string.Empty);

        var parameters = inner.IsNullOrWhiteSpace()
            ? []
            : ParseParameters(inner).ToList();

        return new SqlCreateClrStoredProcedureStatement(schemaName, name, isCreateOrAlter, parameters, CodeRegion.From(codeObject), codeObject);
    }

    private static IEnumerable<ParameterInformation> ParseParameters(string parameters)
    {
        // We use the template and insert the parameters SQL
        // and let the parser do the work
        var sql = Template.Replace("{{Parameters}}", parameters, StringComparison.Ordinal);
        var script = sql.ParseSqlScript();
        var createProcedureStatement = script.GetDescendantsOfType<SqlCreateProcedureStatement>().Single();

        return createProcedureStatement.Definition.Parameters
            .Select(a => new ParameterInformation(a.Name, a.GetDataType(), a.IsOutput));
    }

    [GeneratedRegex(@"\ACREATE\s+((?<alter>OR\s+ALTER\s+))?PROCEDURE\s+(\[?(?<schema>[^\.\]]+)\]?)?\.\[?(?<name>[^\s\]]+)\]?(?<inner>.*?)AS\s+EXTERNAL\s+NAME", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex CreateStatementFinder();

    [GeneratedRegex(@"\sWITH\s.*", RegexOptions.IgnoreCase | RegexOptions.Singleline, 100)]
    private static partial Regex WithFinder();
}
