namespace AuthService.Services;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed class BCryptPasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}
