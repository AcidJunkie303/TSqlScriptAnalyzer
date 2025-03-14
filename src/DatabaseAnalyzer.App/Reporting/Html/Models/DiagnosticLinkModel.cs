using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.App.Reporting.Html.Models;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Used in razor file which requires that it is public")]
public sealed record DiagnosticLinkModel(
    string DiagnosticId,
    IDiagnosticDefinition? DiagnosticDefinition
);
