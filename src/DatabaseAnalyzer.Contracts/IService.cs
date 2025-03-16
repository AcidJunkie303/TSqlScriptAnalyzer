using System.Diagnostics.CodeAnalysis;

namespace DatabaseAnalyzer.Contracts;

[SuppressMessage("Design", "CA1040: Avoid empty interfaces", Justification = "Marker interface")]
[SuppressMessage("Minor Code Smell", "S4023: Interfaces should not be empty", Justification = "Marker interface")]
public interface IService;
