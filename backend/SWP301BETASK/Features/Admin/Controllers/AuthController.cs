using SWP301BETASK.Data;
using SWP301BETASK.Models;
using SWP301BETASK.Features.Admin.DTOs;
using SWP301BETASK.Features.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace SWP301BETASK.Features.Admin.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(
    AppDbContext db,
    IPasswordService passwords,
    IJwtService jwt,
    IOtpService otp,
    ICurrentUser currentUser) : ControllerBase
{
    // Used to burn equivalent CPU time when the account doesn't exist (limits user enumeration by timing).
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("dummy-password", 12);

    [HttpPost("register")]
    public async Task<ActionResult<AccountResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        var role = req.Role!.Value;
        if (role == UserRole.ADMIN)
            return Problem(detail: "ADMIN accounts cannot be self-registered.", statusCode: StatusCodes.Status403Forbidden);

        var username = Normalize.Username(req.Username);
        var email = Normalize.Email(req.Email);
        var phone = Normalize.Phone(req.Phone);

        var taken = await db.Accounts.AnyAsync(
            a => a.Username == username || a.Email == email || (phone != null && a.Phone == phone), ct);
        if (taken)
            return Problem(detail: "Username, email or phone is already in use.", statusCode: StatusCodes.Status409Conflict);

        var account = new Account
        {
            Username = username,
            Email = email,
            Phone = phone,
            FullName = req.FullName?.Trim(),
            Role = role,
            PasswordHash = passwords.Hash(req.Password),
            SecretOtp = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };

        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) // lost a race against the unique indexes
        {
            return Problem(detail: "Username, email or phone is already in use.", statusCode: StatusCodes.Status409Conflict);
        }

        return Created("/api/accounts/me", account.ToResponse());
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var id = req.Identifier.Trim();
        var lower = id.ToLowerInvariant();
        var phone = Normalize.Phone(id);

        var account = await db.Accounts.FirstOrDefaultAsync(
            a => a.Username == lower || a.Email == lower || (phone != null && a.Phone == phone), ct);

        var hash = account?.PasswordHash ?? DummyHash;
        var ok = passwords.Verify(req.Password, hash);

        if (account is null || !ok || !account.IsActive)
            return Problem(detail: "Invalid credentials.", statusCode: StatusCodes.Status401Unauthorized);

        var (token, expires) = jwt.Create(account);
        return Ok(new AuthResponse(token, "Bearer", expires, account.ToResponse()));
    }

    [Authorize]
    [HttpPost("send-email-otp")]
    public async Task<ActionResult<SendOtpResponse>> SendEmailOtp(CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        if (account is null) return Unauthorized();
        if (account.EmailVerified)
            return Problem(detail: "Email is already verified.", statusCode: StatusCodes.Status409Conflict);

        var result = await otp.SendAsync(account, OtpPurpose.Email, ct);
        return result.Success
            ? Ok(new SendOtpResponse("Verification code sent to your email.", result.DevCode))
            : Problem(detail: result.Error, statusCode: StatusCodes.Status429TooManyRequests);
    }

    [Authorize]
    [HttpPost("verify-email-otp")]
    public async Task<ActionResult<AccountResponse>> VerifyEmailOtp(OtpCodeRequest req, CancellationToken ct)
    {
        var account = await currentUser.GetAccountAsync(db, ct);
        if (account is null) return Unauthorized();

        var result = await otp.VerifyAsync(account, OtpPurpose.Email, req.Code, ct);
        if (!result.Success)
            return Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest);

        account.EmailVerified = true;
        await db.SaveChangesAsync(ct);
        return Ok(account.ToResponse());
    }
}
