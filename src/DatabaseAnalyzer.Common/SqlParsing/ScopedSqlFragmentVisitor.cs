using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public abstract class ScopedSqlFragmentVisitor : DatabaseAwareFragmentVisitor
{
    public enum SourceType
    {
        Other = 0,
        TableOrView = 1,
        Cte = 2,
        TempTable = 3
    }

    protected ScopedSqlFragmentVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected ScopeCollection Scopes { get; } = new();

    protected sealed class Scope
    {
        private readonly HashSet<string> _commonTableExpressionNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TableAndAlias> _tableReferencesByFullNameOrAlias = new(StringComparer.OrdinalIgnoreCase);

        public Scope(TSqlFragment owner)
        {
            Owner = owner;
        }

        public IReadOnlyDictionary<string, TableAndAlias> TableReferencesByFullNameOrAlias => _tableReferencesByFullNameOrAlias;
        public IReadOnlySet<string> CommonTableExpressionNames => _commonTableExpressionNames;
        public TSqlFragment Owner { get; }

        public Scope RegisterCommonTableExpressionName(string expressionName)
        {
            _commonTableExpressionNames.Add(expressionName);
            return this;
        }

        public Scope RegisterTableAlias(string? alias, SchemaObjectName schemaObjectName, string currentDatabaseName, string defaultSchemaName, SourceType sourceType)
        {
            ArgumentNullException.ThrowIfNull(schemaObjectName);

            var databaseName = schemaObjectName.DatabaseIdentifier?.Value ?? currentDatabaseName;
            var schemaName = schemaObjectName.SchemaIdentifier?.Value ?? defaultSchemaName;
            var tableName = schemaObjectName.BaseIdentifier.Value;

            return RegisterTableAlias(alias, databaseName, schemaName, tableName, sourceType);
        }

        public Scope RegisterTableAlias(string? alias, string databaseName, string schemaName, string tableName, SourceType sourceType)
        {
            alias ??= $"{databaseName}.{schemaName}.{tableName}";
            var tableAndAlias = new TableAndAlias(alias, databaseName, schemaName, tableName, sourceType);
            _tableReferencesByFullNameOrAlias.Add(tableAndAlias.AliasOrFullTableName, tableAndAlias);
            return this;
        }
    }

    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
    protected sealed class ScopeCollection
    {
        private readonly Stack<Scope> _scopes = new();

        public Scope CurrentScope => _scopes.Peek();
        public Scope RootScope => _scopes.Last();
        public IEnumerable<TableAndAlias> AllTableAndAliases => _scopes.SelectMany(static scope => scope.TableReferencesByFullNameOrAlias.Values);

#pragma warning disable MA0002 // underlying collection is hashset
        public bool IsCommonTableExpressionName(string expressionName) => _scopes.Any(scope => scope.CommonTableExpressionNames.Contains(expressionName));
#pragma warning restore MA0002
        public IDisposable BeginNewScope(TSqlFragment owner)
        {
            _scopes.Push(new Scope(owner));
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

    protected sealed record TableAndAlias(string Alias, string DatabaseName, string SchemaName, string TableName, SourceType SourceType)
    {
        public string AliasOrFullTableName { get; } = Alias.IsNullOrWhiteSpace()
            ? $"{DatabaseName}.{SchemaName}.{TableName}"
            : Alias;
    }
}
