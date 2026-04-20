using BlazorDevTools.CompatibilityFixture.Models;

namespace BlazorDevTools.CompatibilityFixture.Services;

public sealed class CaseWorkspaceService
{
    public async Task<CaseWorkspaceModel> GetWorkspaceAsync(int caseId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        return new CaseWorkspaceModel(
            caseId,
            $"LS-{caseId:0000}",
            caseId % 2 == 0,
            [
                new CaseDocument("Claim overview", "Summary", 3),
                new CaseDocument("Evidence bundle", "Attachment", 7)
            ],
            [
                new CaseActivity("Hearing scheduled", "Case clerk", true),
                new CaseActivity("Client note updated", "Attorney", false)
            ]);
    }
}
