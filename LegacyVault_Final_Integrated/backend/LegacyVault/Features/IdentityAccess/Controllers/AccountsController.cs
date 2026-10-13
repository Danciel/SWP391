using System.Security.Claims;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Features.IdentityAccess.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LegacyVault.Features.IdentityAccess.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;

        public AccountsController(
            IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        // =========================
        // GET CURRENT ACCOUNT
        // =========================
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var accountIdText =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(
                accountIdText,
                out var accountId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            var account =
                await _accountRepository
                    .GetByIdAsync(accountId);

            if (account == null)
            {
                return NotFound(new
                {
                    message = "Account not found."
                });
            }

            var response = new AccountResponse
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

            return Ok(response);
        }

        // =========================
        // SEARCH BENEFICIARY ACCOUNT
        // =========================
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? email,
            [FromQuery] string? phone)
        {
            var hasEmail =
                !string.IsNullOrWhiteSpace(email);

            var hasPhone =
                !string.IsNullOrWhiteSpace(phone);

            // Chỉ được search bằng email HOẶC phone
            if (hasEmail == hasPhone)
            {
                return BadRequest(new
                {
                    message =
                        "Provide exactly one email or phone number."
                });
            }

            var account = hasEmail
                ? await _accountRepository
                    .SearchByEmailAsync(email!)
                : await _accountRepository
                    .SearchByPhoneAsync(phone!);

            if (account == null)
            {
                return NotFound(new
                {
                    message = "Account not found."
                });
            }

            var response =
                new AccountSearchResponse
                {
                    Id = account.Id,

                    FullName = account.FullName,

                    MaskedEmail =
                        MaskEmail(account.Email),

                    MaskedPhoneNumber =
                        MaskPhone(account.Phone),

                    IdentityStatus =
                        account.IdentityStatus
                };

            return Ok(response);
        }

        // =========================
        // MASK EMAIL
        // =========================
        private static string? MaskEmail(
            string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var parts = email.Split('@');

            if (parts.Length != 2)
            {
                return "***";
            }

            var name = parts[0];
            var domain = parts[1];

            if (name.Length <= 3)
            {
                return $"{name[0]}***@{domain}";
            }

            return $"{name[..3]}***@{domain}";
        }

        // =========================
        // MASK PHONE
        // =========================
        private static string? MaskPhone(
            string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            if (phone.Length <= 6)
            {
                return "***";
            }

            var firstThree =
                phone[..3];

            var lastThree =
                phone[^3..];

            return $"{firstThree}****{lastThree}";
        }
    }
}