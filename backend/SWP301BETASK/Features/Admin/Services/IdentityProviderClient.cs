namespace SWP301BETASK.Features.Admin.Services;

public record IdentityCheckRequest(string FullName, string DocumentNumber, DateOnly DateOfBirth);
public record IdentityCheckResult(bool Verified, string? ExternalIdentityId, string? Reason);

public interface IIdentityProviderClient
{
    Task<IdentityCheckResult> VerifyAsync(IdentityCheckRequest request, CancellationToken ct = default);
}

/// <summary>
/// Mock third-party KYC provider. Rules:
///   - DocumentNumber starting with "FAIL"  -> rejected
///   - under 18 years old                   -> rejected
///   - anything else                        -> verified, returns "EXT-xxxxxxxx" id
/// Replace with an HttpClient-based implementation of the real provider.
/// </summary>
public sealed class MockIdentityProviderClient : IIdentityProviderClient
{
    public async Task<IdentityCheckResult> VerifyAsync(IdentityCheckRequest request, CancellationToken ct = default)
    {
        await Task.Delay(150, ct); // simulate network latency

        if (request.DocumentNumber.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase))
            return new(false, null, "Document could not be validated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth > today.AddYears(-age)) age--;
        if (age < 18)
            return new(false, null, "Subject must be at least 18 years old.");

        return new(true, $"EXT-{Guid.NewGuid():N}"[..12], null);
    }
}
