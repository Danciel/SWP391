namespace LegacyVault.Models
{
    public class Face
    {
        public Guid Id { get; set; }

        // One-to-one owner; unique per IdpUser
        public Guid IdpUserId { get; set; }

        public IdpUser IdpUser { get; set; } = null!;

        // Stored vector (serialized float[]) from FaceAiSharp
        public string EmbeddingJson { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}