using MockIdpApi.Features.IdentityAccess.DTOs;

namespace MockIdpApi.Features.IdentityAccess.Contracts
{
    public interface IIdpUserService
    {
        Task<ServiceResult<PagedResponse<IdpUserResponse>>> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? identityStatus,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<IdpUserResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<IdpUserResponse>> CreateAsync(
            AdminCreateIdpUserRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<IdpUserResponse>> UpdateAsync(
            Guid id,
            AdminUpdateIdpUserRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<IdpUserSearchResponse>> SearchAsync(
            string? email,
            string? phone,
            CancellationToken cancellationToken = default);
    }
}