using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

#pragma warning disable CS8321 // Local function is declared but never used
static void GitHubIssue()
#pragma warning restore CS8321 // Local function is declared but never used
{
    const string sqlCode = """
                           ALTER TABLE Table1
                           ADD Value1 INT DEFAULT 1
                           """;
    using var reader = new StringReader(sqlCode);

    var parser = TSqlParser.CreateParser(SqlVersion.Sql170, initialQuotedIdentifiers: true);
    var script = (TSqlScript) parser.Parse(reader, out var errors)!;
    if (errors.Count != 0)
    {
        throw new InvalidOperationException($"Error parsing sql code: {errors[0].Message}");
    }

    var alterTableStatement = (AlterTableAddTableElementStatement) script.Batches[0].Statements[0];
    var defaultConstraint = alterTableStatement.Definition.ColumnDefinitions
        .SelectMany(static a => a.Constraints)
        .OfType<DefaultConstraintDefinition>()
        .FirstOrDefault();

    // here, defaultConstraint is null
    Console.WriteLine(defaultConstraint);
}

#pragma warning disable S125
//var regex = new Regex("▶️(?<header>([^\uD83D][^\uDD3C])+)\uD83D\uDD3C(?<code>[^◀️]+)◀️", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
var regex = new Regex("▶️\uD83D\uDD3C◀️", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
Console.WriteLine(regex.IsMatch("▶️AJ5r03💛script_0.sql💛dbo.bla🔼SELECT r3◀️"));
Console.WriteLine(regex.IsMatch("▶️🔼◀️"));
Console.WriteLine(regex.IsMatch("▶️🔼◀️"));

Console.WriteLine("----------------");
/*
▶
◀
⏯✅
*/
const string input = "▶️AJ503💛script_0.sql💛dbo.bla\u2705SELECT r3◀️";
//const string pattern = "\u25C0\uFE0F";
const string pattern = "✅";

var match = Regex.Match(input, pattern, RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));
Console.WriteLine(match.Index);
