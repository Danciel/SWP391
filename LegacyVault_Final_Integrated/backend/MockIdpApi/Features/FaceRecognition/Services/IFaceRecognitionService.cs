namespace MockIdpApi.Features.FaceRecognition.Services;

public interface IFaceRecognitionService
{
    float[] ExtractEmbedding(Stream imageStream);

    double Compare(float[] first, float[] second);
}
