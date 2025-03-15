using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultServices;

public interface IAstService : IService
{
    bool IsChildOfFunctionEnumParameter(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider);
}
