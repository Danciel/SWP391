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
        private const int PageWidth = 595;
        private const int PageHeight = 842;
        private const int MarginLeft = 50;
        private const int TopY = 790;
        private const int BottomY = 75;

        private sealed record PdfLine(string Text, bool Bold, int Size);

        public static byte[] RenderPdf(
            HandoverMinutesContent content,
            string contentSha256,
            IReadOnlyList<MinutesSignatureLine> signatures,
            bool sealedCopy)
        {
            var lines = new List<PdfLine>();

            void Add(string text, bool bold = false, int size = 10)
            {
                foreach (var part in Wrap(text, size))
                    lines.Add(new PdfLine(part, bold, size));
            }

            void Gap() => lines.Add(new PdfLine(string.Empty, false, 6));

            Add("HANDOVER MINUTES - BIEN BAN BAN GIAO TAI SAN SO", true, 15);
            Gap();
            Add(sealedCopy
                ? "Status: FULLY SIGNED - SEALED COPY"
                : "Status: WAITING FOR SIGNATURES", true, 10);
            Gap();

            Add("Will: " + content.WillTitle);
            Add("Owner: " + content.OwnerName);
            Add("Executor: " + content.ExecutorName);
            Add("Handover type: " + content.HandoverType);
            Add("Started at (UTC): " + content.StartedAt.UtcDateTime
                .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            Add("Will ID: " + content.WillId.ToString("D"), false, 9);
            Add("Handover case ID: " + content.HandoverCaseId.ToString("D"), false, 9);
            Gap();

            Add("Assets handed over", true, 12);
            var index = 1;
            foreach (var item in content.Items)
            {
                var relation = string.IsNullOrWhiteSpace(item.Relationship)
                    ? string.Empty
                    : " (" + item.Relationship + ")";

                Add(index.ToString(CultureInfo.InvariantCulture) + ". " +
                    item.AssetName + " [" + item.AssetType + "] -> " +
                    item.BeneficiaryName + relation + ", share " +
                    item.AllocationPercentage.ToString("0.##", CultureInfo.InvariantCulture) + "%");
                index++;
            }
            Gap();

            Add("Content SHA-256", true, 12);
            Add(contentSha256, false, 9);
            Add("The hash above is taken over the exact content of these minutes. " +
                "Every signature below includes it, so changing the content breaks every signature.",
                false, 9);
            Gap();

            Add("Signatures", true, 12);
            foreach (var sig in signatures)
            {
                var when = sig.SignedAt == null
                    ? "not signed yet"
                    : "signed " + sig.SignedAt.Value.UtcDateTime
                        .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

                Add(sig.Role + " - " + sig.SignerName + ": " + sig.Status + ", " + when);

                if (!string.IsNullOrEmpty(sig.SignatureHash))
                    Add("Signature hash: " + sig.SignatureHash, false, 8);
            }

            return WritePdf(Paginate(lines), contentSha256, content.StartedAt);
        }

        private static List<List<PdfLine>> Paginate(List<PdfLine> lines)
        {
            var pages = new List<List<PdfLine>> { new() };
            var y = TopY;

            foreach (var line in lines)
            {
                var step = line.Size + 4;

                if (y - step < BottomY)
                {
                    pages.Add(new List<PdfLine>());
                    y = TopY;
                }

                pages[^1].Add(line);
                y -= step;
            }

            return pages;
        }

        private static IEnumerable<string> Wrap(string text, int size)
        {
            // Helvetica is about 0.55 em wide on average: stay on the safe side.
            var maxChars = Math.Max(20,
                (int)((PageWidth - 2 * MarginLeft) / (size * 0.56)));

            var clean = ToAscii(text);

            if (clean.Length == 0)
            {
                yield return string.Empty;
                yield break;
            }

            var current = new StringBuilder();

            foreach (var rawWord in clean.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var word = rawWord;

                while (word.Length > maxChars)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }

                    yield return word.Substring(0, maxChars);
                    word = word.Substring(maxChars);
                }

                if (current.Length == 0)
                {
                    current.Append(word);
                }
                else if (current.Length + 1 + word.Length <= maxChars)
                {
                    current.Append(' ').Append(word);
                }
                else
                {
                    yield return current.ToString();
                    current.Clear();
                    current.Append(word);
                }
            }

            if (current.Length > 0)
                yield return current.ToString();
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

        private static string PdfEscape(string text)
            => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        private static byte[] WritePdf(
            List<List<PdfLine>> pages,
            string contentSha256,
            DateTimeOffset createdAt)
        {
            using var ms = new MemoryStream();
            var offsets = new List<long>();

            void Write(string s)
            {
                var bytes = Encoding.Latin1.GetBytes(s);
                ms.Write(bytes, 0, bytes.Length);
            }

            void BeginObject(int number)
            {
                offsets.Add(ms.Position);
                Write(number.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
            }

            Write("%PDF-1.4\n");

            // 1 catalog, 2 pages, 3 regular font, 4 bold font, 5 info,
            // then for page i: page object 6+2i and content stream 7+2i.
            BeginObject(1);
            Write("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            var kids = new StringBuilder();
            for (var i = 0; i < pages.Count; i++)
                kids.Append(6 + 2 * i).Append(" 0 R ");

            BeginObject(2);
            Write("<< /Type /Pages /Kids [ " + kids + "] /Count " +
                  pages.Count.ToString(CultureInfo.InvariantCulture) + " >>\nendobj\n");

            BeginObject(3);
            Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");

            BeginObject(4);
            Write("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

            BeginObject(5);
            Write("<< /Title (Handover minutes) /Producer (LegacyVault) /CreationDate (D:" +
                  createdAt.UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
                  "Z) >>\nendobj\n");

            for (var p = 0; p < pages.Count; p++)
            {
                var content = new StringBuilder();
                var y = TopY;

                foreach (var line in pages[p])
                {
                    if (line.Text.Length > 0)
                    {
                        content.Append("BT /")
                            .Append(line.Bold ? "F2" : "F1").Append(' ')
                            .Append(line.Size.ToString(CultureInfo.InvariantCulture))
                            .Append(" Tf ")
                            .Append(MarginLeft.ToString(CultureInfo.InvariantCulture)).Append(' ')
                            .Append(y.ToString(CultureInfo.InvariantCulture))
                            .Append(" Td (").Append(PdfEscape(line.Text)).Append(") Tj ET\n");
                    }

                    y -= line.Size + 4;
                }

                // Footer on every page: the seal.
                content.Append("BT /F1 7 Tf ")
                    .Append(MarginLeft.ToString(CultureInfo.InvariantCulture)).Append(" 40 Td (")
                    .Append(PdfEscape("Content SHA-256: " + contentSha256))
                    .Append(") Tj ET\n");
                content.Append("BT /F1 7 Tf ")
                    .Append(MarginLeft.ToString(CultureInfo.InvariantCulture)).Append(" 30 Td (")
                    .Append(PdfEscape("LegacyVault handover minutes - page " +
                        (p + 1).ToString(CultureInfo.InvariantCulture) + " of " +
                        pages.Count.ToString(CultureInfo.InvariantCulture)))
                    .Append(") Tj ET\n");

                var streamText = content.ToString();
                var streamLength = Encoding.Latin1.GetByteCount(streamText);

                BeginObject(6 + 2 * p);
                Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " +
                      PageWidth.ToString(CultureInfo.InvariantCulture) + " " +
                      PageHeight.ToString(CultureInfo.InvariantCulture) +
                      "] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " +
                      (7 + 2 * p).ToString(CultureInfo.InvariantCulture) + " 0 R >>\nendobj\n");

                BeginObject(7 + 2 * p);
                Write("<< /Length " + streamLength.ToString(CultureInfo.InvariantCulture) +
                      " >>\nstream\n" + streamText + "endstream\nendobj\n");
            }

            var xrefPosition = ms.Position;
            var total = offsets.Count + 1;

            Write("xref\n0 " + total.ToString(CultureInfo.InvariantCulture) + "\n");
            Write("0000000000 65535 f \n");
            foreach (var offset in offsets)
                Write(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");

            Write("trailer\n<< /Size " + total.ToString(CultureInfo.InvariantCulture) +
                  " /Root 1 0 R /Info 5 0 R >>\nstartxref\n" +
                  xrefPosition.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
