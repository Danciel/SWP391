namespace SWP301BETASK.Features.Admin.Services;

public static class Normalize
{
    public static string Email(string email) => email.Trim().ToLowerInvariant();

    public static string Username(string username) => username.Trim().ToLowerInvariant();

    /// <summary>Keeps digits and a leading '+', e.g. "+84 912-345-678" -> "+84912345678".</summary>
    public static string? Phone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var trimmed = phone.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return trimmed.StartsWith('+') ? "+" + digits : digits;
    }
}
