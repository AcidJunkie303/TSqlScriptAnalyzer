using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public abstract class ScopedSqlConcreteFragmentVisitor : DatabaseAwareConcreteFragmentVisitor
{
    protected ScopeCollection Scopes { get; } = new();

    protected ScopedSqlConcreteFragmentVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected sealed class Scope
    {
        private readonly Dictionary<string, TableAndAlias> _tableReferencesByFullNameOrAlias = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, TableAndAlias> TableReferencesByFullNameOrAlias => _tableReferencesByFullNameOrAlias;

        public Scope RegisterTableAlias(string? alias, SchemaObjectName schemaObjectName, string currentDatabaseName, string defaultSchemaName)
        {
            var databaseName = schemaObjectName.DatabaseIdentifier?.Value ?? currentDatabaseName;
            var schemaName = schemaObjectName.SchemaIdentifier?.Value ?? defaultSchemaName;
            var tableName = schemaObjectName.BaseIdentifier.Value;

            return RegisterTableAlias(alias, databaseName, schemaName, tableName);
        }

        public Scope RegisterTableAlias(string? alias, string databaseName, string schemaName, string tableName)
        {
            var tableAndAlias = new TableAndAlias(alias, databaseName, schemaName, tableName);
            _tableReferencesByFullNameOrAlias.Add(tableAndAlias.AliasOrFullTableName, tableAndAlias);
            return this;
        }
    }

    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
    protected sealed class ScopeCollection
    {
        private readonly Stack<Scope> _scopes = new();

        public Scope CurrentScope => _scopes.Peek();

        public IDisposable BeginNewScope()
        {
            _scopes.Push(new Scope());
            return new ScopeTerminator(_scopes);
        }

        public TableAndAlias? FindTableByAlias(string alias)
            => _scopes
                .Select(scope => scope.TableReferencesByFullNameOrAlias.GetValueOrDefault(alias))
                .FirstOrDefault();

        private sealed class ScopeTerminator : IDisposable
        {
            private readonly Stack<Scope> _scopes;
            private bool _isDisposed;

            public ScopeTerminator(Stack<Scope> scopes)
            {
                _scopes = scopes;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _scopes.Pop();
                _isDisposed = true;
            }
        }
    }

    protected sealed record QuerySource(
        string DatabaseName,
        string SchemaName,
        string TableName,
        string? Alias
    )
    {
        public string AliasOrFullTableName { get; } = Alias.IsNullOrWhiteSpace()
            ? $"[{DatabaseName}].[{SchemaName}].[{TableName}]"
            : Alias;

        public static QuerySource Create(string databaseName, string schemaName, string tableName, string? alias) => new(databaseName, schemaName, tableName, alias);
    }

    protected sealed record TableAndAlias(string? Alias, string DatabaseName, string SchemaName, string TableName)
    {
        public string AliasOrFullTableName { get; } = Alias.IsNullOrWhiteSpace()
            ? $"{DatabaseName}.{SchemaName}.{TableName}"
            : Alias;
    }
}
