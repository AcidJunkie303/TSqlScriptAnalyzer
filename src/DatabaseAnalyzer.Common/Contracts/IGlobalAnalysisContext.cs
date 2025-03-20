using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Common.Contracts;

[SuppressMessage("Minor Code Smell", "S4023:Interfaces should not be empty", Justification = "Marker interface")]
public interface IGlobalAnalysisContext : IAnalysisContext;
