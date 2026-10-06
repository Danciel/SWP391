namespace SWP301BETASK.Models;

public enum UserRole
{
    OWNER,
    EXECUTOR,
    BACKUP_EXECUTOR,
    BENEFICIARY,
    FAMILY_CONTACT,
    LEGAL_VERIFIER,
    ADMIN
}

public enum IdentityStatus
{
    NONE,
    PENDING,
    VERIFIED,
    REJECTED
}

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    /// <summary>Per-user random secret (base64) used to derive email/phone OTP codes.</summary>
    public string SecretOtp { get; set; } = "";

    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public UserRole Role { get; set; }

    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }

    public IdentityStatus IdentityStatus { get; set; } = IdentityStatus.NONE;

    /// <summary>Id returned by the third-party identity provider after a successful check.</summary>
    public string? ExternalIdentityId { get; set; }

    public bool IsActive { get; set; } = true;

    // OTP brute-force protection
    public int OtpFailedAttempts { get; set; }
    public DateTime? OtpLockoutEnd { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
