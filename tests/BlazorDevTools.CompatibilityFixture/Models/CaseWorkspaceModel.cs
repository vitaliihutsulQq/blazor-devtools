namespace BlazorDevTools.CompatibilityFixture.Models;

public sealed record CaseWorkspaceModel(
    int CaseId,
    string CaseNumber,
    bool IsPinned,
    IReadOnlyList<CaseDocument> Documents,
    IReadOnlyList<CaseActivity> Activities);
