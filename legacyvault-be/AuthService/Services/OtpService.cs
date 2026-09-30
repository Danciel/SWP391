using System.Security.Cryptography;
using System.Text;
using AuthService.Configuration;
using AuthService.Data;
using AuthService.Domain;
using Microsoft.Extensions.Options;

namespace AuthService.Services;

public enum OtpPurpose { Email, Phone }

public record OtpSendResult(bool Success, string? Error, string? DevCode);
public record OtpVerifyResult(bool Success, string? Error);

public interface IOtpService
{
    Task<OtpSendResult> SendAsync(Account account, OtpPurpose purpose, CancellationToken ct = default);
    Task<OtpVerifyResult> VerifyAsync(Account account, OtpPurpose purpose, string code, CancellationToken ct = default);
}

/// <summary>
/// Stateless time-based OTP: HMAC-SHA256(account.SecretOtp, purpose|contact|timeStep) -> 6 digits.
/// Step = 5 minutes; the current and previous step are accepted (≈ 5–10 min validity).
/// The code is bound to the email/phone it was sent to, so changing the contact invalidates it.
/// </summary>
public sealed class OtpService(AppDbContext db, IOtpSender sender, IOptions<OtpOptions> options) : IOtpService
{
    private const int StepSeconds = 300;
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<OtpSendResult> SendAsync(Account account, OtpPurpose purpose, CancellationToken ct = default)
    {
        if (IsLocked(account)) return new(false, "Too many failed attempts. Try again later.", null);

        var contact = ContactFor(account, purpose);
        if (contact is null) return new(false, $"No {purpose.ToString().ToLower()} on this account.", null);

        var code = Compute(account.SecretOtp, purpose, contact, CurrentStep());

        if (purpose == OtpPurpose.Email) await sender.SendEmailAsync(contact, code, ct);
        else await sender.SendSmsAsync(contact, code, ct);

        return new(true, null, options.Value.ExposeCodeInResponse ? code : null);
    }

    public async Task<OtpVerifyResult> VerifyAsync(Account account, OtpPurpose purpose, string code, CancellationToken ct = default)
    {
        if (IsLocked(account)) return new(false, "Too many failed attempts. Try again later.");

        var contact = ContactFor(account, purpose);
        if (contact is null) return new(false, $"No {purpose.ToString().ToLower()} on this account.");

        var step = CurrentStep();
        var supplied = Encoding.UTF8.GetBytes(code);
        var valid = false;

        for (var s = step; s >= step - 1; s--)
        {
            var expected = Encoding.UTF8.GetBytes(Compute(account.SecretOtp, purpose, contact, s));
            valid |= CryptographicOperations.FixedTimeEquals(expected, supplied);
        }

        if (valid)
        {
            account.OtpFailedAttempts = 0;
            account.OtpLockoutEnd = null;
            await db.SaveChangesAsync(ct);
            return new(true, null);
        }

        account.OtpFailedAttempts++;
        if (account.OtpFailedAttempts >= MaxFailures)
        {
            account.OtpLockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            account.OtpFailedAttempts = 0;
        }
        await db.SaveChangesAsync(ct);
        return new(false, "Invalid or expired code.");
    }

    private static bool IsLocked(Account a) => a.OtpLockoutEnd is { } end && end > DateTime.UtcNow;

    private static string? ContactFor(Account a, OtpPurpose p) =>
        p == OtpPurpose.Email ? a.Email : a.Phone;

    private static long CurrentStep() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() / StepSeconds;

    private static string Compute(string secretBase64, OtpPurpose purpose, string contact, long step)
    {
        var key = Convert.FromBase64String(secretBase64);
        var data = Encoding.UTF8.GetBytes($"{purpose}|{contact}|{step}");
        var hash = HMACSHA256.HashData(key, data);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | (hash[offset + 1] << 16)
                   | (hash[offset + 2] << 8)
                   | hash[offset + 3];

        return (binary % 1_000_000).ToString("D6");
    }
}
