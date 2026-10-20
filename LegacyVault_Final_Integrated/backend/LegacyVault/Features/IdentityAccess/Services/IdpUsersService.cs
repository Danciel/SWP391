using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class IdpUsersService : IIdpUserService
    {
        private const int MaxPageSize = 100;

        private readonly IIdpUserRepository _idpUserRepository;

        public IdpUsersService(IIdpUserRepository idpUserRepository)
        {
            _idpUserRepository = idpUserRepository;
        }

        // =========================
        // LIST
        // =========================
        public async Task<ServiceResult<PagedResponse<IdpUserResponse>>> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? identityStatus,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            string? normalizedStatus = null;
            if (!string.IsNullOrWhiteSpace(identityStatus))
            {
                normalizedStatus = identityStatus.Trim().ToUpperInvariant();
            }

            var (items, total) = await _idpUserRepository.ListAsync(
                page, pageSize, search?.Trim(), normalizedStatus, cancellationToken);

            var response = new PagedResponse<IdpUserResponse>
            {
                Items = items.Select(ToResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            return ServiceResult<PagedResponse<IdpUserResponse>>.Ok(response);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<ServiceResult<IdpUserResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var user = await _idpUserRepository.GetByIdAsync(id, cancellationToken);

            return user == null
                ? ServiceResult<IdpUserResponse>.Fail(
                    ServiceStatus.NotFound, "IdpUser not found.")
                : ServiceResult<IdpUserResponse>.Ok(ToResponse(user));
        }

        // =========================
        // CREATE
        // =========================
        public async Task<ServiceResult<IdpUserResponse>> CreateAsync(
            AdminCreateIdpUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var email = NormalizeEmail(request.Email);
            var phone = NormalizePhone(request.PhoneNumber);

            if (await _idpUserRepository.EmailExistsAsync(email, null, cancellationToken))
            {
                return ServiceResult<IdpUserResponse>.Fail(
                    ServiceStatus.Conflict, "Email is already in use.");
            }

            if (phone != null &&
                await _idpUserRepository.PhoneExistsAsync(phone, null, cancellationToken))
            {
                return ServiceResult<IdpUserResponse>.Fail(
                    ServiceStatus.Conflict, "Phone number is already in use.");
            }

            var now = DateTimeOffset.UtcNow;

            var user = new IdpUser
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = phone,
                IdentityStatus = NormalizeStatus(request.IdentityStatus),
                FaceId = null, // managed by the FaceId service
                CreatedAt = now,
                UpdatedAt = now
            };

            await _idpUserRepository.AddAsync(user, cancellationToken);

            return ServiceResult<IdpUserResponse>.Created(ToResponse(user));
        }

        // =========================
        // UPDATE (partial)
        // =========================
        public async Task<ServiceResult<IdpUserResponse>> UpdateAsync(
            Guid id,
            AdminUpdateIdpUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var user = await _idpUserRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return ServiceResult<IdpUserResponse>.Fail(
                    ServiceStatus.NotFound, "IdpUser not found.");
            }

            if (request.Email != null)
            {
                var email = NormalizeEmail(request.Email);

                if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _idpUserRepository.EmailExistsAsync(email, id, cancellationToken))
                    {
                        return ServiceResult<IdpUserResponse>.Fail(
                            ServiceStatus.Conflict, "Email is already in use.");
                    }

                    user.Email = email;
                }
            }

            if (request.PhoneNumber != null)
            {
                // Empty string clears the phone number.
                var phone = NormalizePhone(request.PhoneNumber);

                if (phone != user.Phone)
                {
                    if (phone != null &&
                        await _idpUserRepository.PhoneExistsAsync(phone, id, cancellationToken))
                    {
                        return ServiceResult<IdpUserResponse>.Fail(
                            ServiceStatus.Conflict, "Phone number is already in use.");
                    }

                    user.Phone = phone;
                }
            }

            if (request.FullName != null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return ServiceResult<IdpUserResponse>.Fail(
                        ServiceStatus.Invalid, "Full name cannot be empty.");
                }

                user.FullName = request.FullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.IdentityStatus))
            {
                user.IdentityStatus = NormalizeStatus(request.IdentityStatus);
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _idpUserRepository.UpdateAsync(user, cancellationToken);

            return ServiceResult<IdpUserResponse>.Ok(ToResponse(user));
        }

        // =========================
        // DELETE
        // =========================
        public async Task<ServiceResult<bool>> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var user = await _idpUserRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return ServiceResult<bool>.Fail(
                    ServiceStatus.NotFound, "IdpUser not found.");
            }

            await _idpUserRepository.DeleteAsync(user, cancellationToken);

            return ServiceResult<bool>.Ok(true);
        }

        // =========================
        // SEARCH (by email XOR phone)
        // =========================
        public async Task<ServiceResult<IdpUserSearchResponse>> SearchAsync(
            string? email,
            string? phone,
            CancellationToken cancellationToken = default)
        {
            var hasEmail = !string.IsNullOrWhiteSpace(email);
            var hasPhone = !string.IsNullOrWhiteSpace(phone);

            if (hasEmail == hasPhone)
            {
                return ServiceResult<IdpUserSearchResponse>.Fail(
                    ServiceStatus.Invalid,
                    "Provide exactly one email or phone number.");
            }

            var user = hasEmail
                ? await _idpUserRepository.SearchByEmailAsync(email!, cancellationToken)
                : await _idpUserRepository.SearchByPhoneAsync(phone!, cancellationToken);

            if (user == null)
            {
                return ServiceResult<IdpUserSearchResponse>.Fail(
                    ServiceStatus.NotFound, "IdpUser not found.");
            }

            var response = new IdpUserSearchResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                MaskedEmail = MaskEmail(user.Email),
                MaskedPhoneNumber = MaskPhone(user.Phone),
                IdentityStatus = user.IdentityStatus
            };

            return ServiceResult<IdpUserSearchResponse>.Ok(response);
        }

        // =========================
        // HELPERS
        // =========================
        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        private static string? NormalizePhone(string? phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        private static string NormalizeStatus(string? status) =>
            string.IsNullOrWhiteSpace(status)
                ? "UNVERIFIED"
                : status.Trim().ToUpperInvariant();

        private static IdpUserResponse ToResponse(IdpUser user) =>
            new()
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone,
                IdentityStatus = user.IdentityStatus,
                FaceId = user.FaceId,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

        private static string? MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var parts = email.Split('@');
            if (parts.Length != 2) return "***";

            var name = parts[0];
            var domain = parts[1];

            return name.Length <= 3
                ? $"{name[0]}***@{domain}"
                : $"{name[..3]}***@{domain}";
        }

        private static string? MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            if (phone.Length <= 6) return "***";

            return $"{phone[..3]}****{phone[^3..]}";
        }
    }
}