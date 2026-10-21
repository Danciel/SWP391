using LegacyVault.Models;

namespace LegacyVault.Features.FaceRecognition.Contracts;

public interface IFaceRepository
{
    Task<Face?> GetByUserIdAsync(Guid idpUserId, CancellationToken cancellationToken = default);

    void Add(Face face);
    void Update(Face face);
    void Remove(Face face);

    // Saves all tracked changes on the shared DbContext (Face + IdpUser.FaceId)
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}