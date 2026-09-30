using System.Security.Claims;
using AuthService.Data;
using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? Id { get; }
    /// <summary>Throws UnauthorizedAccessException when nobody is signed in.</summary>
    Guid RequiredId { get; }
    string? Username { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsInRole(UserRole role);
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public Guid RequiredId => Id ?? throw new UnauthorizedAccessException("No authenticated user.");

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;

    public bool IsInRole(UserRole role) => Role == role;
}

public static class CurrentUserExtensions
{
    /// <summary>Loads the tracked, active Account for the signed-in user (null if missing/disabled).</summary>
    public static Task<Account?> GetAccountAsync(this ICurrentUser user, AppDbContext db, CancellationToken ct = default)
    {
        var id = user.RequiredId;
        return db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.IsActive, ct);
    }
}
