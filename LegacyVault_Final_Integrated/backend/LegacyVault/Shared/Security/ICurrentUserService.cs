namespace LegacyVault.Shared.Security
{
    public interface ICurrentUserService
    {
        Guid GetRequiredAccountId();
    }
}
