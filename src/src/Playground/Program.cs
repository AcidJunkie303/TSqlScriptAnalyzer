using Microsoft.SqlServer.TransactSql.ScriptDom;

const string sqlCode = """
                       ALTER TABLE Table1
                       ADD Value1 INT DEFAULT 1
                       """;
using var reader = new StringReader(sqlCode);

var parser = TSqlParser.CreateParser(SqlVersion.Sql170, initialQuotedIdentifiers: true);
var script = (TSqlScript)parser.Parse(reader, out var errors)!;
if (errors.Count != 0)
{
    throw new InvalidOperationException($"Error parsing sql code: {errors[0].Message}");
}

var alterTableStatement = (AlterTableAddTableElementStatement)script.Batches[0].Statements[0];
var defaultConstraint = alterTableStatement.Definition.ColumnDefinitions
    .SelectMany(static a => a.Constraints)
    .OfType<DefaultConstraintDefinition>()
    .FirstOrDefault();

// here, defaultConstraint is null

Console.WriteLine(defaultConstraint);
