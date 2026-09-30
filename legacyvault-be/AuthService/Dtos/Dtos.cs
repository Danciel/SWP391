using System.ComponentModel.DataAnnotations;
using AuthService.Domain;

namespace AuthService.Dtos;

public record RegisterRequest(
    [param: Required, StringLength(50, MinimumLength = 3), RegularExpression(@"^[A-Za-z0-9_.-]+$")] string Username,
    [param: Required, EmailAddress, StringLength(254)] string Email,
    [param: RegularExpression(@"^\+?[0-9\s\-]{8,20}$")] string? Phone,
    [param: Required, RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8-100 chars and contain a letter and a digit.")] string Password,
    [param: Required] UserRole? Role,
    [param: StringLength(100)] string? FullName
    );

public record LoginRequest(
    [param: Required] string Identifier,   // username, email or phone
    [param: Required] string Password
    );

public record OtpCodeRequest(
    [param: Required, RegularExpression(@"^\d{6}$")] string Code
    );

public record UpdateProfileRequest(
    [param: StringLength(100)] string? FullName,
    [param: EmailAddress, StringLength(254)] string? Email,
    [param: RegularExpression(@"^\+?[0-9\s\-]{8,20}$")] string? Phone
    );

public record ChangeRoleRequest([param: Required] UserRole? Role
);

public record IdentityVerifyRequest(
    [param: Required, StringLength(100)] string FullName,
    [param: Required, StringLength(50, MinimumLength = 6)] string DocumentNumber,
    [param: Required] DateOnly? DateOfBirth
    );

public record AccountResponse(
    Guid Id, string Username, string Email, string? Phone, string? FullName, UserRole Role,
    bool EmailVerified, bool PhoneVerified, IdentityStatus IdentityStatus, string? ExternalIdentityId,
    DateTime DateCreated, DateTime DateUpdated
    );

/// <summary>Limited view returned to non-admin callers of the search endpoint.</summary>
public record AccountSummary(Guid Id, string Username, UserRole Role, IdentityStatus IdentityStatus);

public record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAt, AccountResponse Account);

public record SendOtpResponse(string Message, string? DevCode);

public record IdentityStatusResponse(IdentityStatus Status, string? ExternalIdentityId, string? Reason);

public static class AccountMappings
{
    public static AccountResponse ToResponse(this Account a) => new(
        a.Id, a.Username, a.Email, a.Phone, a.FullName, a.Role,
        a.EmailVerified, a.PhoneVerified, a.IdentityStatus, a.ExternalIdentityId,
        a.DateCreated, a.DateUpdated
        );

    public static AccountSummary ToSummary(this Account a) => new(a.Id, a.Username, a.Role, a.IdentityStatus);
}
