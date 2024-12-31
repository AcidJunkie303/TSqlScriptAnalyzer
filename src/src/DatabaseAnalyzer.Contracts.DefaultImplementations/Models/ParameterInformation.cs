using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed record ParameterInformation(string Name, DataTypeReference DataType, bool IsOutput);
