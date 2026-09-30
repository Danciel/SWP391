namespace SWP301BETASK.Features.Admin.Configuration;

public class JwtOptions
{
    public string Issuer { get; set; } = "SWP301BETASK";
    public string Audience { get; set; } = "SWP301BETASK.Clients";
    public string Secret { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 60;
}

public class OtpOptions
{
    /// <summary>DEV ONLY: echo the OTP in the API response so you can test without email/SMS.</summary>
    public bool ExposeCodeInResponse { get; set; }
}
