namespace LegacyVault.Features.WillManagement.Contracts
{
    // Links a beneficiary designation (name + email typed by the owner) to the
    // real account without the owner: the account must have a VERIFIED email
    // equal to the designation email AND the same name. The owner may no
    // longer be alive after a death handover, so this cannot wait for them.
    public interface IBeneficiaryAutoLinkService
    {
        // An account just verified its email / signed in: link the matching
        // designations of wills that have a running handover.
        Task<int> TryLinkAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

        // A handover just started: link designations whose account already exists.
        Task<int> TryLinkWillAsync(Guid willId, CancellationToken cancellationToken = default);

        // C7: the account email matches but the NAME differs. The Executor of the
        // will checks the person and links them by hand (a written reason is kept).
        Task ConfirmByExecutorAsync(
            Guid willId,
            Guid designationId,
            Guid executorAccountId,
            string note,
            CancellationToken cancellationToken = default);
    }
}
