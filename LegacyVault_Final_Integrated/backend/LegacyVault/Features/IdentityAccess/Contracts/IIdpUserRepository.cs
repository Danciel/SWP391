using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Contracts
{
    public interface IIdpUserRepository
    {
        Task<IdpUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IdpUser?> SearchByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<IdpUser?> SearchByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

        Task<bool> EmailExistsAsync(
            string email,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);

        Task<bool> PhoneExistsAsync(
            string phoneNumber,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<IdpUser> Items, int Total)> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? identityStatus,
            CancellationToken cancellationToken = default);

        Task AddAsync(IdpUser user, CancellationToken cancellationToken = default);

        Task UpdateAsync(IdpUser user, CancellationToken cancellationToken = default);

        Task DeleteAsync(IdpUser user, CancellationToken cancellationToken = default);
    }
}