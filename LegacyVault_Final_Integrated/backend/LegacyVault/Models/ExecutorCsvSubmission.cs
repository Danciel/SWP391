namespace LegacyVault.Models;

public class ExecutorCsvSubmission
{
    public Guid Id { get; set; }
    public Guid ExecutorAccountId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public DateTimeOffset UploadedAt { get; set; }
}
