using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record ParameterInformation(string Name, DataTypeReference DataType, bool IsOutput, bool HasDefaultValue, bool IsNullable);
