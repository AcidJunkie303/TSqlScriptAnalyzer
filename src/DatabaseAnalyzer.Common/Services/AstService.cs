using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Services;

public sealed class AstService : IAstService
{
    private readonly AstServiceSettings _settings;

    public AstService(AstServiceSettings settings)
    {
        _settings = settings;
    }

    public bool IsChildOfFunctionEnumParameter(TSqlFragment fragment, IParentFragmentProvider parentFragmentProvider)
    {
        if (_settings.EnumerationValueParameterIndicesByFunctionName.Count == 0)
        {
            return false;
        }

        var current = fragment;

        while (true)
        {
            var parent = parentFragmentProvider.GetParent(current);
            if (parent is null)
            {
                return false;
            }

            if (parent is not FunctionCall functionCall)
            {
                current = parent;
                continue;
            }

            if (!_settings.EnumerationValueParameterIndicesByFunctionName.TryGetValue(functionCall.FunctionName.Value, out var enumParameterIndices))
            {
                current = parent;
                continue;
            }

            var indexOfCurrentFragment = GetIndexOfArgument(functionCall, current);
            if (enumParameterIndices.Contains(indexOfCurrentFragment))
            {
                return true;
            }

            current = parent;
        }

        static int GetIndexOfArgument(FunctionCall functionCall, TSqlFragment argument)
        {
            for (var i = 0; i < functionCall.Parameters.Count; i++)
            {
                var parameter = functionCall.Parameters[i];
                if (ReferenceEquals(parameter, argument))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
