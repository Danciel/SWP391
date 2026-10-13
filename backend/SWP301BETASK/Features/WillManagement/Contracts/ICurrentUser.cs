namespace SWP301BETASK.Features.WillManagement.Contracts
{
    /// <summary>
    /// The logged-in account. IdentityAccess (Nhiên) owns the real CurrentUser helper; once it exists,
    /// register an implementation of this interface that reads the JWT (or swap usages to hers).
    /// </summary>
    public interface ICurrentUser
    {
        Guid UserId { get; }
    }
}
