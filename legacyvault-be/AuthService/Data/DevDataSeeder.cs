using System.Security.Cryptography;
using AuthService.Domain;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

/// <summary>
/// Generates test users straight into whichever database is configured (Postgres / SQL Server / SQLite).
/// Run:  dotnet run -- --seed-users 30
/// Idempotent: usernames testadmin, testuser001.. are skipped if they already exist.
/// All seeded users share the password <see cref="DefaultPassword"/>.
/// </summary>
public static class DevDataSeeder
{
    public const string DefaultPassword = "Test@12345";

    private static readonly UserRole[] Roles =
    {
        UserRole.OWNER, UserRole.EXECUTOR, UserRole.BACKUP_EXECUTOR,
        UserRole.BENEFICIARY, UserRole.FAMILY_CONTACT, UserRole.LEGAL_VERIFIER
    };

    // i % 5 -> 0 nothing verified | 1 email | 2 email+phone | 3 fully identity-verified | 4 identity rejected
    private static readonly string[] StateNames =
    {
        "unverified", "email verified", "email+phone verified", "identity VERIFIED", "identity REJECTED"
    };

    public static async Task<int> SeedAsync(
        AppDbContext db, IPasswordService passwords, int count, CancellationToken ct = default)
    {
        var taken = (await db.Accounts
                .Where(a => a.Username.StartsWith("testuser") || a.Username == "testadmin")
                .Select(a => a.Username)
                .ToListAsync(ct))
            .ToHashSet();

        var hash = passwords.Hash(DefaultPassword); // hash once, reuse (BCrypt is intentionally slow)
        var toAdd = new List<(Account Account, string State)>();

        if (!taken.Contains("testadmin"))
            toAdd.Add((Build("testadmin", UserRole.ADMIN, "+84900000000", hash, 3), "admin, identity VERIFIED"));

        for (var i = 1; i <= count; i++)
        {
            var username = $"testuser{i:D3}";
            if (taken.Contains(username)) continue;

            var state = i % 5;
            var role = Roles[(i - 1) % Roles.Length];
            toAdd.Add((Build(username, role, $"+84900{i:D6}", hash, state), StateNames[state]));
        }

        if (toAdd.Count == 0)
        {
            Console.WriteLine("Nothing to do: all requested test users already exist.");
            return 0;
        }

        db.Accounts.AddRange(toAdd.Select(t => t.Account));
        await db.SaveChangesAsync(ct);

        Console.WriteLine($"{"USERNAME",-14} {"ROLE",-16} {"PHONE",-14} STATE");
        foreach (var (a, state) in toAdd)
            Console.WriteLine($"{a.Username,-14} {a.Role,-16} {a.Phone,-14} {state}");
        Console.WriteLine($"Password for all seeded users: {DefaultPassword}");

        return toAdd.Count;
    }

    private static Account Build(string username, UserRole role, string phone, string hash, int state)
    {
        var a = new Account
        {
            Username = username,
            Email = $"{username}@test.local",
            Phone = phone,
            FullName = $"Test {username}",
            Role = role,
            PasswordHash = hash,
            SecretOtp = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };

        if (state >= 1) a.EmailVerified = true;
        if (state >= 2) a.PhoneVerified = true;
        if (state == 3)
        {
            a.IdentityStatus = IdentityStatus.VERIFIED;
            a.ExternalIdentityId = $"EXT-{username}";
        }
        else if (state == 4)
        {
            a.IdentityStatus = IdentityStatus.REJECTED;
        }

        return a;
    }
}
