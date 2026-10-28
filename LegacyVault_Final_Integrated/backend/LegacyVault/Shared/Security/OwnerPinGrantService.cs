
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace LegacyVault.Shared.Security;

public static class OwnerPinScopes
{
    public const string AssetSecret = "ASSET_SECRET";
    public const string WillDetail = "WILL_DETAIL";
    public const string AssetDelete = "ASSET_DELETE";
    public const string ExecutorRequestDelete = "EXECUTOR_REQUEST_DELETE";
}

public sealed class OwnerPinGrantService
{
    // Grant co hieu luc 5 phut
    private static readonly TimeSpan GrantLifetime =
        TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _protector;

    // B1. Khoi tao Data Protection
    public OwnerPinGrantService(
        IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector(
                "LegacyVault.OwnerSecurityPin.Grant.v1"
            )
            .ToTimeLimitedDataProtector();
    }

    // B2. Kiem tra muc dich truy cap
    private static bool ValidScope(string scope)
    {
        return scope == OwnerPinScopes.AssetSecret
            || scope == OwnerPinScopes.WillDetail
            || scope == OwnerPinScopes.AssetDelete
            || scope == OwnerPinScopes.ExecutorRequestDelete;
    }

    // B3. Cap grant sau khi xac minh PIN thanh cong
    public string CreateGrant(
        Guid ownerId,
        string scope,
        Guid resourceId)
    {
        if (ownerId == Guid.Empty ||
            resourceId == Guid.Empty ||
            !ValidScope(scope))
        {
            throw new ArgumentException(
                "Invalid security PIN grant request."
            );
        }

        // Grant gan voi dung Owner, scope va resource
        string payload =
            $"{ownerId:N}|{scope}|{resourceId:N}";

        return _protector.Protect(
            payload,
            GrantLifetime
        );
    }

    // B4. Kiem tra grant truoc khi cho xem du lieu
    public bool IsValidGrant(
        string? grant,
        Guid ownerId,
        string scope,
        Guid resourceId)
    {
        if (string.IsNullOrWhiteSpace(grant) ||
            ownerId == Guid.Empty ||
            resourceId == Guid.Empty ||
            !ValidScope(scope))
        {
            return false;
        }

        try
        {
            // Giai ma va kiem tra thoi han
            string payload =
                _protector.Unprotect(grant);

            string expected =
                $"{ownerId:N}|{scope}|{resourceId:N}";

            byte[] actualBytes =
                Encoding.UTF8.GetBytes(payload);

            byte[] expectedBytes =
                Encoding.UTF8.GetBytes(expected);

            return CryptographicOperations.FixedTimeEquals(
                actualBytes,
                expectedBytes
            );
        }
        catch (CryptographicException)
        {
            // Grant sai, bi sua hoac het han
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
