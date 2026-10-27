using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Contracts
{
    public interface ILegalService
    {
        Task<LegalCaseSummaryResponse> SubmitAsync(
            Guid executorAccountId,
            Guid willId,
            IFormFile file);

        Task<List<LegalCaseSummaryResponse>> ListAsync(
            Guid accountId,
            bool isLegalVerifier,
            string? status);

        Task<LegalCaseDetailResponse> GetAsync(
            Guid caseId,
            Guid accountId,
            bool isLegalVerifier);

        Task<(string Path, string ContentType, string FileName)>
            GetCertificateAsync(
                Guid caseId,
                Guid accountId,
                bool isLegalVerifier);

        Task<LegalCaseDetailResponse> DecideAsync(
            Guid caseId,
            Guid reviewerAccountId,
            LegalDecisionRequest request);
    }
}
