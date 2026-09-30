namespace AuthService.Services;

public interface IOtpSender
{
    Task SendEmailAsync(string to, string code, CancellationToken ct = default);
    Task SendSmsAsync(string to, string code, CancellationToken ct = default);
}

/// <summary>Mock sender: writes the code to the log. Swap for SendGrid/SES/Twilio etc.</summary>
public sealed class ConsoleOtpSender(ILogger<ConsoleOtpSender> logger) : IOtpSender
{
    public Task SendEmailAsync(string to, string code, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK EMAIL] To: {To} | OTP: {Code}", to, code);
        return Task.CompletedTask;
    }

    public Task SendSmsAsync(string to, string code, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK SMS] To: {To} | OTP: {Code}", to, code);
        return Task.CompletedTask;
    }
}
