using Microsoft.SqlServer.TransactSql.ScriptDom;

const string sqlCode = """
                       CREATE VIEW [dbo].[View1]
                       AS
                           SELECT Id, Name FROM Table1
                       GO
                       """;
using var reader = new StringReader(sqlCode);

var parser = TSqlParser.CreateParser(SqlVersion.Sql170, initialQuotedIdentifiers: true);
var script = (TSqlScript) parser.Parse(reader, out var errors)!;
if (errors.Count != 0)
{
    throw new InvalidOperationException($"Error parsing sql code: {errors[0].Message}");
}

var createViewStatement = script.Batches[0].Statements[0];
Console.WriteLine(createViewStatement);
