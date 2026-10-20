using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using LegacyVault.Models;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class AccountsService : IAccountService
    {
        private const int MaxPageSize = 100;

        private static readonly HashSet<string> AllowedRoles =
            new(StringComparer.OrdinalIgnoreCase)
            { "USER", "ADMIN", "OWNER", "BENEFICIARY", "LEGAL_VERIFIER" };

        private readonly IAccountRepository _accountRepository;

        public AccountsService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        // =========================
        // LIST
        // =========================
        public async Task<ServiceResult<PagedResponse<AccountResponse>>> ListAsync(
            int page,
            int pageSize,
            string? search,
            string? role,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            string? normalizedRole = null;
            if (!string.IsNullOrWhiteSpace(role))
            {
                normalizedRole = role.Trim().ToUpperInvariant();
                if (!AllowedRoles.Contains(normalizedRole))
                {
                    return ServiceResult<PagedResponse<AccountResponse>>.Fail(
                        ServiceStatus.Invalid, "Invalid role filter.");
                }
            }

            var (items, total) = await _accountRepository.ListAsync(
                page, pageSize, search?.Trim(), normalizedRole, cancellationToken);

            var response = new PagedResponse<AccountResponse>
            {
                Items = items.Select(ToResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            return ServiceResult<PagedResponse<AccountResponse>>.Ok(response);
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<ServiceResult<AccountResponse>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByIdAsync(id);

            return account == null
                ? ServiceResult<AccountResponse>.Fail(ServiceStatus.NotFound, "Account not found.")
                : ServiceResult<AccountResponse>.Ok(ToResponse(account));
        }

        // =========================
        // CREATE
        // =========================
        public async Task<ServiceResult<AccountResponse>> CreateAsync(
            AdminCreateAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            var role = request.Role.Trim().ToUpperInvariant();
            if (!AllowedRoles.Contains(role))
            {
                return ServiceResult<AccountResponse>.Fail(
                    ServiceStatus.Invalid, "Invalid role.");
            }

            var email = NormalizeEmail(request.Email);
            var phone = NormalizePhone(request.PhoneNumber);

            if (await _accountRepository.EmailExistsAsync(email, null, cancellationToken))
            {
                return ServiceResult<AccountResponse>.Fail(
                    ServiceStatus.Conflict, "Email is already in use.");
            }

            if (phone != null &&
                await _accountRepository.PhoneExistsAsync(phone, null, cancellationToken))
            {
                return ServiceResult<AccountResponse>.Fail(
                    ServiceStatus.Conflict, "Phone number is already in use.");
            }

            var now = DateTimeOffset.UtcNow;

            var account = new Account
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = phone,
                Role = role,
                EmailVerified = request.EmailVerified,
                PhoneVerified = request.PhoneVerified && phone != null,
                CreatedAt = now,
                UpdatedAt = now
            };

            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

            await _accountRepository.AddAsync(account, cancellationToken);

            return ServiceResult<AccountResponse>.Created(ToResponse(account));
        }

        // =========================
        // UPDATE (partial)
        // =========================
        public async Task<ServiceResult<AccountResponse>> UpdateAsync(
            Guid id,
            AdminUpdateAccountRequest request,
            Guid currentAdminId,
            CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return ServiceResult<AccountResponse>.Fail(
                    ServiceStatus.NotFound, "Account not found.");
            }

            if (request.Role != null)
            {
                var role = request.Role.Trim().ToUpperInvariant();

                if (!AllowedRoles.Contains(role))
                {
                    return ServiceResult<AccountResponse>.Fail(
                        ServiceStatus.Invalid, "Invalid role.");
                }

                // Prevent an admin from locking themselves out.
                if (id == currentAdminId && role != "ADMIN")
                {
                    return ServiceResult<AccountResponse>.Fail(
                        ServiceStatus.Forbidden, "You cannot remove your own admin role.");
                }

                account.Role = role;
            }

            if (request.Email != null)
            {
                var email = NormalizeEmail(request.Email);

                if (!string.Equals(email, account.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _accountRepository.EmailExistsAsync(email, id, cancellationToken))
                    {
                        return ServiceResult<AccountResponse>.Fail(
                            ServiceStatus.Conflict, "Email is already in use.");
                    }

                    account.Email = email;
                    // A changed email is no longer verified unless the admin says so.
                    account.EmailVerified = request.EmailVerified ?? false;
                }
            }

            if (request.PhoneNumber != null)
            {
                // Empty string clears the phone number.
                var phone = NormalizePhone(request.PhoneNumber);

                if (phone != account.Phone)
                {
                    if (phone != null &&
                        await _accountRepository.PhoneExistsAsync(phone, id, cancellationToken))
                    {
                        return ServiceResult<AccountResponse>.Fail(
                            ServiceStatus.Conflict, "Phone number is already in use.");
                    }

                    account.Phone = phone;
                    account.PhoneVerified = phone != null && (request.PhoneVerified ?? false);
                }
            }

            if (request.FullName != null)
            {
                if (string.IsNullOrWhiteSpace(request.FullName))
                {
                    return ServiceResult<AccountResponse>.Fail(
                        ServiceStatus.Invalid, "Full name cannot be empty.");
                }

                account.FullName = request.FullName.Trim();
            }

            if (request.Password != null)
            {
                account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
            }

            if (request.EmailVerified.HasValue && request.Email == null)
            {
                account.EmailVerified = request.EmailVerified.Value;
            }

            if (request.PhoneVerified.HasValue && request.PhoneNumber == null)
            {
                account.PhoneVerified = request.PhoneVerified.Value && account.Phone != null;
            }

            if (!string.IsNullOrWhiteSpace(request.IdentityStatus))
            {
                account.IdentityStatus = request.IdentityStatus.Trim().ToUpperInvariant();
            }

            account.UpdatedAt = DateTimeOffset.UtcNow;

            await _accountRepository.UpdateAsync(account, cancellationToken);

            return ServiceResult<AccountResponse>.Ok(ToResponse(account));
        }

        // =========================
        // DELETE
        // =========================
        public async Task<ServiceResult<bool>> DeleteAsync(
            Guid id,
            Guid currentAdminId,
            CancellationToken cancellationToken = default)
        {
            if (id == currentAdminId)
            {
                return ServiceResult<bool>.Fail(
                    ServiceStatus.Forbidden, "You cannot delete your own account.");
            }

            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
            {
                return ServiceResult<bool>.Fail(
                    ServiceStatus.NotFound, "Account not found.");
            }

            await _accountRepository.DeleteAsync(account, cancellationToken);

            return ServiceResult<bool>.Ok(true);
        }

        // =========================
        // HELPERS
        // =========================
        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        private static string? NormalizePhone(string? phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        private static AccountResponse ToResponse(Account account) =>
            new()
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                PhoneNumber = account.Phone,
                Role = account.Role,
                EmailVerified = account.EmailVerified,
                PhoneVerified = account.PhoneVerified,
                IdentityStatus = account.IdentityStatus,
                CreatedAt = account.CreatedAt
            };
    }
}