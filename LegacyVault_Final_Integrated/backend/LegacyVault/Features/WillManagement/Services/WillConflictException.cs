namespace LegacyVault.Features.WillManagement.Services
{
    // Business conflict (HTTP 409): asset locked or invalid state.
    public class WillConflictException : Exception
    {
        public WillConflictException(string message) : base(message) { }
    }
}
