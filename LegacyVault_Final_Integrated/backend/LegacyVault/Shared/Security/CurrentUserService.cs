using System.Security.Claims;

namespace LegacyVault.Shared.Security
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetRequiredAccountId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var accountIdText =
                user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(accountIdText, out var accountId))
            {
                throw new UnauthorizedAccessException(
                    "Invalid or missing authenticated account.");
            }

            return accountId;
        }
    }
}
