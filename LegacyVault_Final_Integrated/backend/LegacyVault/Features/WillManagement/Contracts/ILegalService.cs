using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface ILegalService
    {
        Task<LegalCaseSummaryResponse> SubmitAsync(
            Guid executorAccountId,
            Guid willId,
            IFormFile file,
            string? certificateData = null);

        Task<List<LegalCaseSummaryResponse>> ListAsync(
            Guid accountId,
            bool isLegalVerifier,
            string? status);

        Task<LegalCaseDetailResponse> GetAsync(
            Guid caseId,
            Guid accountId,
            bool isLegalVerifier);

        // C8: the decrypted file (checked against its stored SHA-256).
        Task<(byte[] Content, string ContentType, string FileName)>
            GetCertificateAsync(
                Guid caseId,
                Guid accountId,
                bool isLegalVerifier);

        // C9: the Executor withdraws a PENDING case (wrong file, etc.).
        Task<LegalCaseDetailResponse> WithdrawAsync(
            Guid caseId,
            Guid executorAccountId);

        Task<LegalCaseDetailResponse> DecideAsync(
            Guid caseId,
            Guid reviewerAccountId,
            LegalDecisionRequest request);
    }
}
