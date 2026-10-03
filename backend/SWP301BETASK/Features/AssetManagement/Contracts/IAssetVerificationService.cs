namespace SWP301BETASK.Features.AssetManagement.Contracts
{
    public interface IAssetVerificationService
    {
        Task<bool> VerifyAsync(
            string provider,
            string username,
            string password);
    }
}