
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LegacyVault.Data;
using LegacyVault.Features.IdentityAccess.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using NpgsqlTypes;

namespace LegacyVault.Shared.Security;

// B1. DTO
public sealed class SetupOwnerPinRequest
{
    public string Pin { get; set; } = "";
    public string ConfirmPin { get; set; } = "";
}

public sealed class VerifyOwnerPinRequest
{
    public string Pin { get; set; } = "";
    public string Scope { get; set; } = "";
    public Guid ResourceId { get; set; }
}

public sealed class ChangeOwnerPinRequest
{
    public string CurrentPin { get; set; } = "";
    public string NewPin { get; set; } = "";
    public string ConfirmPin { get; set; } = "";
}

public sealed class ResetOwnerPinRequest
{
    public string Otp { get; set; } = "";
    public string NewPin { get; set; } = "";
    public string ConfirmPin { get; set; } = "";
}

// B2. CONTROLLER
[ApiController]
[Authorize]
[Route("api/owner-pin")]
public sealed class OwnerSecurityPinController : ControllerBase
{
    private const int MaxAttempts = 5;
    private const int Iterations = 210_000;

    private readonly LegacyVaultDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _config;
    private readonly OwnerPinGrantService _grantService;
    private readonly IAccountRepository _accounts;
    private readonly IEmailOtpService _emailOtpService;
    private readonly IEmailOtpRepository _emailOtpRepository;
    private readonly IEmailSender _emailSender;
    private readonly IMemoryCache _cache;

    private const int MaxOtpAttempts = 5;

    public OwnerSecurityPinController(
        LegacyVaultDbContext db,
        ICurrentUserService currentUser,
        IConfiguration config,
        OwnerPinGrantService grantService,
        IAccountRepository accounts,
        IEmailOtpService emailOtpService,
        IEmailOtpRepository emailOtpRepository,
        IEmailSender emailSender,
        IMemoryCache cache)
    {
        _db = db;
        _currentUser = currentUser;
        _config = config;
        _grantService = grantService;
        _accounts = accounts;
        _emailOtpService = emailOtpService;
        _emailOtpRepository = emailOtpRepository;
        _emailSender = emailSender;
        _cache = cache;
    }

    // B3. VALIDATE PIN
    private static bool IsValidPin(string? pin)
    {
        return pin != null &&
               Regex.IsMatch(pin, @"^[0-9]{6}$");
    }

    // B4. HASH PIN
    private byte[] HashPin(string pin, byte[] salt)
    {
        string pepper = _config["OwnerPin:Pepper"]
            ?? throw new InvalidOperationException(
                "OwnerPin:Pepper is missing.");

        if (pepper.Length < 32)
        {
            throw new InvalidOperationException(
                "Owner PIN pepper is too short.");
        }

        byte[] key = Encoding.UTF8.GetBytes(pepper);

        byte[] keyedPin = HMACSHA256.HashData(
            key,
            Encoding.UTF8.GetBytes(pin)
        );

        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                keyedPin,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                32
            );
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyedPin);
        }
    }

    // B5. CONNECTION
    private NpgsqlConnection CreateConnection()
    {
        string connectionString =
            _db.Database.GetConnectionString()
            ?? throw new InvalidOperationException(
                "Database connection string is missing.");

        return new NpgsqlConnection(connectionString);
    }

    // =====================================
    // GET /api/owner-pin/status
    // =====================================
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        Guid ownerId =
            _currentUser.GetRequiredAccountId();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM public."OwnerSecurityPins"
                WHERE "OwnerAccountId" = @ownerId
            )
            """,
            connection
        );

        command.Parameters.AddWithValue(
            "ownerId", ownerId);

        bool configured = (bool)(
            await command.ExecuteScalarAsync() ?? false
        );

        return Ok(new { configured });
    }

    // =====================================
    // POST /api/owner-pin/setup
    // =====================================
    [HttpPost("setup")]
    public async Task<IActionResult> Setup(
        [FromBody] SetupOwnerPinRequest request)
    {
        if (!IsValidPin(request.Pin) ||
            request.Pin != request.ConfirmPin)
        {
            return BadRequest(new
            {
                message =
                    "PIN must contain 6 matching digits."
            });
        }

        Guid ownerId =
            _currentUser.GetRequiredAccountId();

        byte[] salt =
            RandomNumberGenerator.GetBytes(16);

        byte[] pinHash =
            HashPin(request.Pin, salt);

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO public."OwnerSecurityPins"
            (
                "OwnerAccountId",
                "Salt",
                "PinHash"
            )
            VALUES
            (
                @ownerId,
                @salt,
                @pinHash
            )
            ON CONFLICT ("OwnerAccountId")
            DO NOTHING
            """,
            connection
        );

        command.Parameters.AddWithValue(
            "ownerId", ownerId);

        command.Parameters.AddWithValue(
            "salt", salt);

        command.Parameters.AddWithValue(
            "pinHash", pinHash);

        int rows = await command.ExecuteNonQueryAsync();

        if (rows == 0)
        {
            return Conflict(new
            {
                message = "PIN already configured."
            });
        }

        return StatusCode(201, new
        {
            message = "PIN created successfully."
        });
    }

    // =====================================
    // POST /api/owner-pin/verify
    // =====================================
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyOwnerPinRequest request)
    {
        if (!IsValidPin(request.Pin))
        {
            return BadRequest(new
            {
                message = "PIN must contain 6 digits."
            });
        }

        if (request.ResourceId == Guid.Empty ||
            request.Scope is not
                ("ASSET_SECRET" or "WILL_DETAIL"))
        {
            return BadRequest(new
            {
                message = "Invalid resource or scope."
            });
        }

        Guid ownerId =
            _currentUser.GetRequiredAccountId();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        // Transaction de cap nhat so lan sai an toan
        await using var transaction =
            (NpgsqlTransaction)
            await connection.BeginTransactionAsync();

        byte[] storedSalt;
        byte[] storedHash;
        int failedAttempts;
        DateTimeOffset? lockedUntil;

        // B6. Lay hash va khoa dong
        await using (var select = new NpgsqlCommand(
            """
            SELECT
                "Salt",
                "PinHash",
                "FailedAttempts",
                "LockedUntil"
            FROM public."OwnerSecurityPins"
            WHERE "OwnerAccountId" = @ownerId
            FOR UPDATE
            """,
            connection,
            transaction
        ))
        {
            select.Parameters.AddWithValue(
                "ownerId", ownerId);

            await using var reader =
                await select.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return NotFound(new
                {
                    message = "PIN is not configured."
                });
            }

            storedSalt = reader.GetFieldValue<byte[]>(0);
            storedHash = reader.GetFieldValue<byte[]>(1);
            failedAttempts = reader.GetInt32(2);

            lockedUntil = reader.IsDBNull(3)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(3);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // B7. Kiem tra lockout
        if (lockedUntil.HasValue &&
            lockedUntil.Value > now)
        {
            return StatusCode(429, new
            {
                message = "PIN is temporarily locked.",
                lockedUntil
            });
        }

        // Sau khi het khoa, bat dau dem lai
        if (lockedUntil.HasValue &&
            lockedUntil.Value <= now)
        {
            failedAttempts = 0;
        }

        // B8. So sanh hash
        byte[] candidate = HashPin(
            request.Pin,
            storedSalt
        );

        bool isCorrect =
            CryptographicOperations.FixedTimeEquals(
                candidate,
                storedHash
            );

        CryptographicOperations.ZeroMemory(candidate);

        if (!isCorrect)
        {
            failedAttempts = Math.Min(
                MaxAttempts,
                failedAttempts + 1
            );

            DateTimeOffset? newLock =
                failedAttempts >= MaxAttempts
                    ? now.AddMinutes(15)
                    : null;

            await using var update = new NpgsqlCommand(
                """
                UPDATE public."OwnerSecurityPins"
                SET
                    "FailedAttempts" = @attempts,
                    "LockedUntil" = @lockedUntil,
                    "UpdatedAt" = NOW()
                WHERE "OwnerAccountId" = @ownerId
                """,
                connection,
                transaction
            );

            update.Parameters.AddWithValue(
                "ownerId", ownerId);

            update.Parameters.AddWithValue(
                "attempts", failedAttempts);

            update.Parameters
                .Add("lockedUntil", NpgsqlDbType.TimestampTz)
                .Value =
                    (object?)newLock ?? DBNull.Value;

            await update.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return StatusCode(
                newLock.HasValue ? 429 : 401,
                new
                {
                    message = newLock.HasValue
                        ? "PIN locked for 15 minutes."
                        : "Incorrect PIN.",
                    remainingAttempts =
                        MaxAttempts - failedAttempts
                }
            );
        }

        // B9. Reset so lan sai
        await using (var reset = new NpgsqlCommand(
            """
            UPDATE public."OwnerSecurityPins"
            SET
                "FailedAttempts" = 0,
                "LockedUntil" = NULL,
                "UpdatedAt" = NOW()
            WHERE "OwnerAccountId" = @ownerId
            """,
            connection,
            transaction
        ))
        {
            reset.Parameters.AddWithValue(
                "ownerId", ownerId);

            await reset.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        // B10. Cap grant 5 phut
        string grant = _grantService.CreateGrant(
            ownerId,
            request.Scope,
            request.ResourceId
        );

        Response.Headers.CacheControl = "no-store";

        return Ok(new
        {
            grant,
            expiresInSeconds = 300
        });
    }

    // =====================================
    // POST /api/owner-pin/change
    // Doi PIN khi con nho PIN hien tai
    // =====================================
    [HttpPost("change")]
    public async Task<IActionResult> Change(
        [FromBody] ChangeOwnerPinRequest request)
    {
        if (!IsValidPin(request.CurrentPin) ||
            !IsValidPin(request.NewPin) ||
            request.NewPin != request.ConfirmPin)
        {
            return BadRequest(new
            {
                message = "PINs must contain 6 digits and the new PINs must match."
            });
        }

        if (request.NewPin == request.CurrentPin)
        {
            return BadRequest(new
            {
                message = "New PIN must be different from the current PIN."
            });
        }

        Guid ownerId = _currentUser.GetRequiredAccountId();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await using var transaction =
            (NpgsqlTransaction)await connection.BeginTransactionAsync();

        byte[] storedSalt;
        byte[] storedHash;
        int failedAttempts;
        DateTimeOffset? lockedUntil;

        await using (var select = new NpgsqlCommand(
            """
            SELECT "Salt", "PinHash", "FailedAttempts", "LockedUntil"
            FROM public."OwnerSecurityPins"
            WHERE "OwnerAccountId" = @ownerId
            FOR UPDATE
            """,
            connection,
            transaction))
        {
            select.Parameters.AddWithValue("ownerId", ownerId);

            await using var reader = await select.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return NotFound(new { message = "PIN is not configured." });
            }

            storedSalt = reader.GetFieldValue<byte[]>(0);
            storedHash = reader.GetFieldValue<byte[]>(1);
            failedAttempts = reader.GetInt32(2);
            lockedUntil = reader.IsDBNull(3)
                ? null
                : reader.GetFieldValue<DateTimeOffset>(3);
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (lockedUntil.HasValue && lockedUntil.Value > now)
        {
            return StatusCode(429, new
            {
                message = "PIN is temporarily locked. Use Forgot PIN to reset it with an email code.",
                lockedUntil
            });
        }

        if (lockedUntil.HasValue && lockedUntil.Value <= now)
        {
            failedAttempts = 0;
        }

        byte[] candidate = HashPin(request.CurrentPin, storedSalt);

        bool isCorrect = CryptographicOperations.FixedTimeEquals(
            candidate, storedHash);

        CryptographicOperations.ZeroMemory(candidate);

        if (!isCorrect)
        {
            failedAttempts = Math.Min(MaxAttempts, failedAttempts + 1);

            DateTimeOffset? newLock =
                failedAttempts >= MaxAttempts ? now.AddMinutes(15) : null;

            await using var update = new NpgsqlCommand(
                """
                UPDATE public."OwnerSecurityPins"
                SET "FailedAttempts" = @attempts,
                    "LockedUntil" = @lockedUntil,
                    "UpdatedAt" = NOW()
                WHERE "OwnerAccountId" = @ownerId
                """,
                connection,
                transaction);

            update.Parameters.AddWithValue("ownerId", ownerId);
            update.Parameters.AddWithValue("attempts", failedAttempts);
            update.Parameters
                .Add("lockedUntil", NpgsqlDbType.TimestampTz)
                .Value = (object?)newLock ?? DBNull.Value;

            await update.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return StatusCode(
                newLock.HasValue ? 429 : 401,
                new
                {
                    message = newLock.HasValue
                        ? "PIN locked for 15 minutes."
                        : "Current PIN is incorrect.",
                    remainingAttempts = MaxAttempts - failedAttempts
                });
        }

        byte[] newSalt = RandomNumberGenerator.GetBytes(16);
        byte[] newHash = HashPin(request.NewPin, newSalt);

        await using (var save = new NpgsqlCommand(
            """
            UPDATE public."OwnerSecurityPins"
            SET "Salt" = @salt,
                "PinHash" = @pinHash,
                "FailedAttempts" = 0,
                "LockedUntil" = NULL,
                "UpdatedAt" = NOW()
            WHERE "OwnerAccountId" = @ownerId
            """,
            connection,
            transaction))
        {
            save.Parameters.AddWithValue("ownerId", ownerId);
            save.Parameters.AddWithValue("salt", newSalt);
            save.Parameters.AddWithValue("pinHash", newHash);
            await save.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        return Ok(new { message = "PIN changed successfully." });
    }

    // =====================================
    // POST /api/owner-pin/forgot
    // Gui ma OTP qua email cua tai khoan dang dang nhap
    // =====================================
    [HttpPost("forgot")]
    public async Task<IActionResult> Forgot()
    {
        Guid ownerId = _currentUser.GetRequiredAccountId();

        // Moi 60 giay chi gui 1 ma de tranh spam email
        string cooldownKey = $"pin-forgot-cooldown:{ownerId}";
        if (_cache.TryGetValue(cooldownKey, out _))
        {
            return StatusCode(429, new
            {
                message = "A code was sent recently. Please wait a minute before requesting another."
            });
        }

        await using (var connection = CreateConnection())
        {
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                """
                SELECT EXISTS (
                    SELECT 1 FROM public."OwnerSecurityPins"
                    WHERE "OwnerAccountId" = @ownerId
                )
                """,
                connection);

            command.Parameters.AddWithValue("ownerId", ownerId);

            bool configured = (bool)(await command.ExecuteScalarAsync() ?? false);

            if (!configured)
            {
                return NotFound(new { message = "PIN is not configured. Create one first." });
            }
        }

        var account = await _accounts.GetByIdAsync(ownerId);
        if (account == null)
        {
            return NotFound(new { message = "Account not found." });
        }

        string otp = await _emailOtpService.GenerateAndSaveOtpAsync(account.Id);
        await _emailSender.SendOtpAsync(account.Email, otp);

        _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(60));
        _cache.Remove($"pin-reset-attempts:{ownerId}");

        return Ok(new { message = "A verification code has been sent to your email." });
    }

    // =====================================
    // POST /api/owner-pin/reset
    // Dat lai PIN bang OTP email (khong can PIN cu)
    // =====================================
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(
        [FromBody] ResetOwnerPinRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Otp) ||
            !Regex.IsMatch(request.Otp, @"^[0-9]{6}$"))
        {
            return BadRequest(new { message = "Verification code must be 6 digits." });
        }

        if (!IsValidPin(request.NewPin) ||
            request.NewPin != request.ConfirmPin)
        {
            return BadRequest(new
            {
                message = "PIN must contain 6 matching digits."
            });
        }

        Guid ownerId = _currentUser.GetRequiredAccountId();

        string attemptsKey = $"pin-reset-attempts:{ownerId}";
        int attempts = _cache.TryGetValue(attemptsKey, out int cached) ? cached : 0;

        if (attempts >= MaxOtpAttempts)
        {
            return StatusCode(429, new
            {
                message = "Too many incorrect codes. Request a new code and try again."
            });
        }

        var emailOtp = await _emailOtpRepository.GetLatestUnusedAsync(ownerId);

        if (emailOtp == null)
        {
            return BadRequest(new
            {
                message = "No active code found. Request a new one."
            });
        }

        if (emailOtp.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest(new
            {
                message = "The code has expired. Request a new one."
            });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Otp, emailOtp.OtpHash))
        {
            _cache.Set(attemptsKey, attempts + 1, TimeSpan.FromMinutes(15));

            return BadRequest(new { message = "Invalid verification code." });
        }

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] pinHash = HashPin(request.NewPin, salt);

        await using (var connection = CreateConnection())
        {
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                """
                UPDATE public."OwnerSecurityPins"
                SET "Salt" = @salt,
                    "PinHash" = @pinHash,
                    "FailedAttempts" = 0,
                    "LockedUntil" = NULL,
                    "UpdatedAt" = NOW()
                WHERE "OwnerAccountId" = @ownerId
                """,
                connection);

            command.Parameters.AddWithValue("ownerId", ownerId);
            command.Parameters.AddWithValue("salt", salt);
            command.Parameters.AddWithValue("pinHash", pinHash);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 0)
            {
                return NotFound(new { message = "PIN is not configured. Create one first." });
            }
        }

        emailOtp.UsedAt = DateTimeOffset.UtcNow;
        await _emailOtpRepository.SaveChangesAsync();

        _cache.Remove(attemptsKey);

        return Ok(new { message = "PIN reset successfully." });
    }
}
