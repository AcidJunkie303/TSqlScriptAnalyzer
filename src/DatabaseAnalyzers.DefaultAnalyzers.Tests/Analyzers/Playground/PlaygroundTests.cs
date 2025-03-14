using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Playground;

public sealed class PlaygroundTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectCreationWithoutOrAlterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void PlaygroundTests1()
    {
        const string code = """
                            USE MyDB
                            GO

                            DECLARE @Id1 INT = 0

                            UPDATE  m   -- here, the error is reported
                            SET     m.RowStateId = 0
                            FROM    MyTable m
                            WHERE   m.RowStateId = 1 AND m.MId = @Id1

                            """;

        var script = code.ParseSqlScript();

        var visitor = new Visitor("dbo", script);
        script.Accept(visitor);

        var tester = GetDefaultTesterBuilder(code).Build();
        Verify(tester);
    }

    internal sealed class Visitor : DatabaseAwareFragmentVisitor
    {
        private readonly TSqlScript _script;

        public Visitor(string defaultSchemaName, TSqlScript script) : base(defaultSchemaName)
        {
            _script = script;
        }

        public override void Visit(NamedTableReference node)
        {
            Console.Write(node);
            base.Visit(node);
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            var resolver = new TableColumnResolver(new FakeIssueReporter(), _script, node, "dummy.sql", "dbo");

            var aaa = resolver.Resolve();
            Console.Write(aaa);
            base.Visit(node);
        }
    }

    internal sealed class FakeIssueReporter : IIssueReporter
    {
        public List<IIssue> Issues { get; } = [];

        IReadOnlyList<IIssue> IIssueReporter.Issues => Issues;

        public void Report(IDiagnosticDefinition rule, string databaseName, string relativeScriptFilePath, string? fullObjectName, CodeRegion codeRegion, params object[] insertionStrings)
        {
            var issue = Issue.Create(rule, databaseName, relativeScriptFilePath, fullObjectName, codeRegion, insertionStrings);
            Issues.Add(issue);
        }
    }
}
