using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.Services;

public interface IAstService : IService
{
    bool IsChildOfFunctionEnumParameter(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider);
}
