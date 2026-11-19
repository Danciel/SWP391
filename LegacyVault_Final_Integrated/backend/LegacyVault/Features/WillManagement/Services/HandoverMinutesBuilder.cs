using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LegacyVault.Features.WillManagement.DTOs;

namespace LegacyVault.Features.WillManagement.Services
{
    // What the PDF prints for one required signature.
    public sealed record MinutesSignatureLine(
        string Role,
        string SignerName,
        string Status,
        DateTimeOffset? SignedAt,
        string? SignatureHash);

    // C2: content, SHA-256 and PDF of the handover minutes.
    // Pure code (no database, no NuGet package): the PDF is written by hand
    // with the built-in Helvetica font, so Vietnamese letters are printed
    // without diacritics (Nguyen Van A). The exact names stay in ContentJson.
    public static class HandoverMinutesBuilder
    {
        public static string SerializeContent(HandoverMinutesContent content)
            => JsonSerializer.Serialize(content);

        public static HandoverMinutesContent? DeserializeContent(string json)
            => JsonSerializer.Deserialize<HandoverMinutesContent>(json);

        public static string Sha256Hex(string text)
            => Sha256Hex(Encoding.UTF8.GetBytes(text));

        public static string Sha256Hex(byte[] data)
            => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

        // PostgreSQL keeps microseconds, .NET keeps 100 ns ticks: cut the
        // time now so the value hashed is the value read back later.
        public static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        {
            var utc = value.UtcDateTime;
            return new DateTimeOffset(
                utc.Ticks - (utc.Ticks % 10), TimeSpan.Zero);
        }

        public static string SignatureHash(
            string contentSha256,
            string role,
            Guid signerAccountId,
            Guid? beneficiaryDesignationId,
            DateTimeOffset signedAt)
        {
            var raw = string.Join("|",
                contentSha256,
                role,
                signerAccountId.ToString("D"),
                beneficiaryDesignationId?.ToString("D") ?? "-",
                signedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));

            return Sha256Hex(raw);
        }

        // ------------------------------------------------------------
        // PDF
        // ------------------------------------------------------------

        public static byte[] RenderPdf(
            HandoverMinutesContent content,
            string contentSha256,
            IReadOnlyList<MinutesSignatureLine> signatures,
            bool sealedCopy)
        {
            return HandoverMinutesPdf.Render(content, contentSha256, signatures, sealedCopy);
        }


        // Helvetica (WinAnsi) has no Vietnamese letters: drop the marks.
        public static string ToAscii(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var decomposed = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) ==
                    UnicodeCategory.NonSpacingMark)
                    continue;

                if (ch == '\u0111') sb.Append('d');
                else if (ch == '\u0110') sb.Append('D');
                else if (ch >= ' ' && ch <= '~') sb.Append(ch);
                else sb.Append('?');
            }

            return sb.ToString();
        }
   
    }
}
