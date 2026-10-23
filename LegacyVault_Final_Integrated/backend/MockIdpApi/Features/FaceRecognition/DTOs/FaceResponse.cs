namespace MockIdpApi.Features.FaceRecognition.DTOs;

public class FaceEnrollResponse
{
    public Guid UserId { get; set; }
    public Guid FaceId { get; set; }
    public bool FaceEnrolled { get; set; }
}

public class FaceVerifyResponse
{
    public Guid UserId { get; set; }
    public bool Matched { get; set; }
    public double Score { get; set; }
}