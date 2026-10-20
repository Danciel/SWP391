using LegacyVault.Features.IdentityAccess.DTOs;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IAccountService
    {
        Task<ServiceResult<PagedResponse<AccountResponse>>> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? role,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<AccountResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<AccountResponse>> CreateAsync(
            AdminCreateAccountRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<AccountResponse>> UpdateAsync(
            Guid id,
            AdminUpdateAccountRequest request,
            Guid currentAdminId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> DeleteAsync(
            Guid id,
            Guid currentAdminId,
            CancellationToken cancellationToken = default);
    }
}
