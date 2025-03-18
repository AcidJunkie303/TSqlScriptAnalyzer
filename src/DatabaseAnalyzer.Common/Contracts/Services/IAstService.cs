using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Contracts.Services;

public interface IAstService : IService
{
    bool IsChildOfFunctionEnumParameter(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider);
}
