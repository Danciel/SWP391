using MockIdpApi.Data;
using MockIdpApi.Features.FaceRecognition.Contracts;
using MockIdpApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MockIdpApi.Features.FaceRecognition.Repositories;

public class FaceRepository : IFaceRepository
{
    private readonly MockIdpDbContext _context;

    public FaceRepository(MockIdpDbContext context)
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