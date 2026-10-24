using System.Linq;
using FaceAiSharp;
using FaceAiSharp.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MockIdpApi.Features.FaceRecognition.Services;

public class FaceRecognitionService : IFaceRecognitionService
{
    // Load the models once, rather than once per HTTP request.
    private readonly IFaceDetectorWithLandmarks  _detector;
    private readonly IFaceEmbeddingsGenerator  _recognizer;

    private readonly object _sync = new();

    public FaceRecognitionService()
    {
        _detector =
            FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();

        _recognizer =
            FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
    }

    public float[] ExtractEmbedding(Stream imageStream)
    {
        using var image = Image.Load<Rgb24>(imageStream);

        lock (_sync)
        {
            var faces = _detector.DetectFaces(image);

            // Do not enroll an ambiguous image.
            if (faces.Count != 1)
            {
                throw new InvalidFaceImageException(
                    faces.Count == 0
                        ? "No face was detected."
                        : "Multiple faces were detected.");
            }

            var face = faces.First();

            if (face.Landmarks is null)
            {
                throw new InvalidFaceImageException(
                    "Could not identify facial landmarks.");
            }

            // Alignment modifies the image, so use a copy.
            using var aligned = image.Clone();

            _recognizer.AlignFaceUsingLandmarks(
                aligned, face.Landmarks);

            var embedding = _recognizer.GenerateEmbedding(aligned);

            // Store ordinary numbers, not the model-specific object.
            return embedding.ToArray();
        }
    }

    public double Compare(float[] first, float[] second)
    {
        if (first.Length == 0 || first.Length != second.Length)
        {
            throw new ArgumentException(
                "Face embeddings are incompatible.");
        }

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < first.Length; i++)
        {
            dot += (double)first[i] * second[i];
            normA += (double)first[i] * first[i];
            normB += (double)second[i] * second[i];
        }

        if (normA == 0 || normB == 0)
        {
            throw new ArgumentException(
                "Cannot compare a zero-length embedding.");
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

public class InvalidFaceImageException : Exception
{
    public InvalidFaceImageException(string message)
        : base(message)
    {
    }
}
