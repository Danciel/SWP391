using LegacyVault.Data;
using LegacyVault.Features.FaceRecognition.Contracts;
using LegacyVault.Models;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.FaceRecognition.Repositories;

public class FaceRepository : IFaceRepository
{
    private readonly LegacyVaultDbContext _context;

    public FaceRepository(LegacyVaultDbContext context)
    {
        _context = context;
    }

    public Task<Face?> GetByUserIdAsync(
        Guid idpUserId,
        CancellationToken cancellationToken = default)
        => _context.Faces.FirstOrDefaultAsync(
            x => x.IdpUserId == idpUserId, cancellationToken);

    public void Add(Face face) => _context.Faces.Add(face);

    public void Update(Face face) => _context.Faces.Update(face);

    public void Remove(Face face) => _context.Faces.Remove(face);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}