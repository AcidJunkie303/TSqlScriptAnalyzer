using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Testing;

internal static class SupportedDiagnosticRetriever
{
    public static IReadOnlyList<IDiagnosticDefinition> GetSupportedDiagnostics(Type analyzerType)
    {
        var retrieverType = typeof(Retriever<>).MakeGenericType(analyzerType);
        var retriever = (IRetriever) Activator.CreateInstance(retrieverType)!;
        return retriever.RetrieveSupportedDiagnostics();
    }

    private interface IRetriever
    {
        IReadOnlyList<IDiagnosticDefinition> RetrieveSupportedDiagnostics();
    }

    private sealed class Retriever<T> : IRetriever
        where T : IObjectAnalyzer
    {
        public IReadOnlyList<IDiagnosticDefinition> RetrieveSupportedDiagnostics() => T.SupportedDiagnostics;
    }
}
