using System.Text.Json;
using MockIdpApi.Features.FaceRecognition.Contracts;
using MockIdpApi.Features.FaceRecognition.DTOs;
using MockIdpApi.Features.IdentityAccess.Contracts;
using MockIdpApi.Models;

namespace MockIdpApi.Features.FaceRecognition.Services;

public class FaceService : IFaceService
{
    // Tune per environment.
    private const double MatchThreshold = 0.45;

    private readonly IFaceRepository _faceRepository;
    private readonly IIdpUserRepository _idpUserRepository;
    private readonly IFaceRecognitionService _faceRecognition;

    public FaceService(
        IFaceRepository faceRepository,
        IIdpUserRepository idpUserRepository,
        IFaceRecognitionService faceRecognition)
    {
        _faceRepository = faceRepository;
        _idpUserRepository = idpUserRepository;
        _faceRecognition = faceRecognition;
    }

    public async Task<ServiceResult<FaceEnrollResponse>> EnrollAsync(
        Guid idpUserId,
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        var user = await _idpUserRepository.GetByIdAsync(idpUserId, cancellationToken);
        if (user == null)
        {
            return ServiceResult<FaceEnrollResponse>.Fail(
                ServiceStatus.NotFound, "IdpUser not found.");
        }

        // 1. Extract the new face embedding
        var newEmbedding = _faceRecognition.ExtractEmbedding(imageStream);

        // 2. Check for duplicate faces across OTHER accounts
        var otherFaces = await _faceRepository.GetAllExceptUserAsync(idpUserId, cancellationToken);

        foreach (var otherFace in otherFaces)
        {
            var storedEmbedding = JsonSerializer.Deserialize<float[]>(otherFace.EmbeddingJson);
            if (storedEmbedding != null)
            {
                var score = _faceRecognition.Compare(newEmbedding, storedEmbedding);

                // If it matches an existing face on another account
                if (score >= MatchThreshold)
                {
                    return ServiceResult<FaceEnrollResponse>.Fail(
                        ServiceStatus.Conflict,
                        "This face is already registered to another account. Please use a different face.");
                }
            }
        }

        // 3. If no duplicates, proceed with enrollment
        var json = JsonSerializer.Serialize(newEmbedding);
        var existing = await _faceRepository.GetByUserIdAsync(idpUserId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        Face face;
        if (existing != null)
        {
            existing.EmbeddingJson = json;
            existing.UpdatedAt = now;
            face = existing;
        }
        else
        {
            face = new Face
            {
                Id = Guid.NewGuid(),
                IdpUserId = user.Id,
                EmbeddingJson = json,
                CreatedAt = now,
                UpdatedAt = now
            };
            _faceRepository.Add(face);
        }

        // Keep the denormalized pointer in sync
        user.FaceId = face.Id;
        user.UpdatedAt = now;

        await _faceRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<FaceEnrollResponse>.Ok(new FaceEnrollResponse
        {
            UserId = user.Id,
            FaceId = face.Id,
            FaceEnrolled = true
        });
    }

    public async Task<ServiceResult<FaceVerifyResponse>> VerifyAsync(
        Guid idpUserId,
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        var user = await _idpUserRepository.GetByIdAsync(idpUserId, cancellationToken);
        if (user == null)
        {
            return ServiceResult<FaceVerifyResponse>.Fail(
                ServiceStatus.NotFound, "IdpUser not found.");
        }

        var face = await _faceRepository.GetByUserIdAsync(idpUserId, cancellationToken);
        if (face == null)
        {
            return ServiceResult<FaceVerifyResponse>.Fail(
                ServiceStatus.Conflict, "This user has no enrolled face.");
        }

        var submitted = _faceRecognition.ExtractEmbedding(imageStream);

        var stored = JsonSerializer.Deserialize<float[]>(face.EmbeddingJson);
        if (stored == null)
        {
            return ServiceResult<FaceVerifyResponse>.Fail(
                ServiceStatus.Invalid, "Stored face enrollment is invalid.");
        }

        var score = _faceRecognition.Compare(submitted, stored);

        return ServiceResult<FaceVerifyResponse>.Ok(new FaceVerifyResponse
        {
            UserId = user.Id,
            Matched = score >= MatchThreshold,
            Score = Math.Round(score, 4)
        });
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid idpUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _idpUserRepository.GetByIdAsync(idpUserId, cancellationToken);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(
                ServiceStatus.NotFound, "IdpUser not found.");
        }

        var face = await _faceRepository.GetByUserIdAsync(idpUserId, cancellationToken);
        if (face == null)
        {
            // Idempotent: nothing to delete
            return ServiceResult<bool>.Ok(false);
        }

        _faceRepository.Remove(face);
        user.FaceId = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _faceRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }
}