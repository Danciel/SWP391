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
        private readonly IAccountService _accountService;
        private const string AdminRole = "ADMIN";

        public AccountsController(
            IAccountRepository accountRepository,
            IAccountService accountService)
        {
            _accountRepository = accountRepository;
            _accountService = accountService;
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
        // ADMIN: LIST ACCOUNTS
        // GET api/accounts?page=1&pageSize=20&search=...&role=USER
        // =========================
        [HttpGet]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> List(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _accountService.ListAsync(
                page, pageSize, search, role, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // ADMIN: GET ACCOUNT BY ID
        // =========================
        [HttpGet("{id:guid}")]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _accountService.GetByIdAsync(id, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // ADMIN: CREATE ACCOUNT
        // =========================
        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> Create(
            [FromBody] AdminCreateAccountRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _accountService.CreateAsync(request, cancellationToken);

            if (result.Status == ServiceStatus.Created)
            {
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Data!.Id },
                    result.Data);
            }

            return ToActionResult(result);
        }

        // =========================
        // ADMIN: UPDATE ACCOUNT
        // =========================
        [HttpPut("{id:guid}")]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] AdminUpdateAccountRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentAccountId(out var currentAdminId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _accountService.UpdateAsync(
                id, request, currentAdminId, cancellationToken);

            return ToActionResult(result);
        }

        // =========================
        // ADMIN: DELETE ACCOUNT
        // =========================
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentAccountId(out var currentAdminId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var result = await _accountService.DeleteAsync(
                id, currentAdminId, cancellationToken);

            return result.Status == ServiceStatus.Ok
                ? NoContent()
                : ToActionResult(result);
        }

        // =========================
        // HELPERS
        // =========================
        private bool TryGetCurrentAccountId(out Guid accountId) =>
            Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out accountId);

        private IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            return result.Status switch
            {
                ServiceStatus.Ok => Ok(result.Data),
                ServiceStatus.Created => StatusCode(StatusCodes.Status201Created, result.Data),
                ServiceStatus.NotFound => NotFound(new { message = result.Error }),
                ServiceStatus.Conflict => Conflict(new { message = result.Error }),
                ServiceStatus.Forbidden => StatusCode(
                    StatusCodes.Status403Forbidden, new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
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