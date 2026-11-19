using System.Globalization;
using LegacyVault.Features.WillManagement.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LegacyVault.Features.WillManagement.Services
{
    // English handover minutes drawn with QuestPDF and its built-in font.
    // Every text that comes from the database goes through ToAscii (accents
    // removed), so no custom font file is needed.
    public static class HandoverMinutesPdf
    {
        private const string Brand = "#205E50";
        private const string Line = "#B8C4BF";

        private static bool _licenseSet;

        private static string A(string? value)
            => HandoverMinutesBuilder.ToAscii(value);

        public static byte[] Render(
            HandoverMinutesContent content,
            string contentSha256,
            IReadOnlyList<MinutesSignatureLine> signatures,
            bool sealedCopy)
        {
            if (!_licenseSet)
            {
                // Community license: free for individuals / small companies.
                QuestPDF.Settings.License = LicenseType.Community;
                _licenseSet = true;
            }

            var status = sealedCopy
                ? "FULLY SIGNED - SEALED COPY"
                : "WAITING FOR SIGNATURES";

            var executors = signatures.Where(s => s.Role == "EXECUTOR").ToList();
            var beneficiaries = signatures.Where(s => s.Role != "EXECUTOR").ToList();

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    var logoPath = Path.Combine(AppContext.BaseDirectory, "logo.jpg");

                    page.Header()
                        .PaddingBottom(6)
                        .BorderBottom(1).BorderColor(Brand)
                        .Row(row =>
                        {
                            if (File.Exists(logoPath))
                            {
                                row.AutoItem()
                                    .Height(40)
                                    .Image(logoPath);
                            }
                            else
                            {
                                row.AutoItem()
                                    .Text("LEGACYVAULT").Bold().FontSize(14).FontColor(Brand);
                            }

                            row.RelativeItem()
                                .AlignRight()
                                .AlignBottom()
                                .Text("DIGITAL ASSET HANDOVER MINUTES")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(8);

                        column.Item().AlignCenter()
                            .Text("HANDOVER MINUTES").Bold().FontSize(16);
                        column.Item().AlignCenter()
                            .Text("Digital Asset Handover Record")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);

                        column.Item().Background("#EEF5F2").Padding(8)
                            .Text(t =>
                            {
                                t.Span("Case status: ").Bold();
                                t.Span(status).Bold().FontColor(Brand);
                            });

                        Heading(column, "I. CASE INFORMATION");
                        Info(column, "Will name", A(content.WillTitle));
                        Info(column, "Owner", A(content.OwnerName));
                        Info(column, "Executor", A(content.ExecutorName));
                        Info(column, "Handover type", A(content.HandoverType));
                        Info(column, "Started at (UTC)",
                            content.StartedAt.UtcDateTime.ToString(
                                "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));
                        Info(column, "Will ID", content.WillId.ToString("D"));
                        Info(column, "Handover case ID", content.HandoverCaseId.ToString("D"));

                        Heading(column, "II. ASSETS HANDED OVER");
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(36);
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                                c.ConstantColumn(54);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("No.");
                                h.Cell().Element(HeaderCell).Text("Asset / Type");
                                h.Cell().Element(HeaderCell).Text("Beneficiary");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Share");
                            });

                            var index = 1;
                            foreach (var item in content.Items)
                            {
                                var relation = string.IsNullOrWhiteSpace(item.Relationship)
                                    ? string.Empty
                                    : " (" + A(item.Relationship) + ")";

                                table.Cell().Element(BodyCell)
                                    .Text(index.ToString("00", CultureInfo.InvariantCulture));
                                table.Cell().Element(BodyCell)
                                    .Text(A(item.AssetName) + " | " + A(item.AssetType));
                                table.Cell().Element(BodyCell)
                                    .Text(A(item.BeneficiaryName) + relation);
                                table.Cell().Element(BodyCell).AlignRight()
                                    .Text(item.AllocationPercentage.ToString(
                                        "0.##", CultureInfo.InvariantCulture) + "%");
                                index++;
                            }
                        });

                        Heading(column, "III. HASH AND CONTENT INTEGRITY");
                        column.Item().Text("SHA-256 of the original minutes:").Bold();
                        column.Item().Text(contentSha256).FontSize(9);
                        column.Item().Text(
                                "This hash is taken from the content of the original minutes. Every " +
                                "electronic signature is bound to it when it is created; changing the " +
                                "content may make the signatures no longer match.")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);

                        Heading(column, "IV. CONFIRMATION AND SIGNATURES");
                        column.Item().ShowEntire().Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem().Element(c =>
                                SignatureBlock(c, "EXECUTOR", executors));
                            row.RelativeItem().Element(c =>
                                SignatureBlock(c, "BENEFICIARY", beneficiaries));
                        });
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().AlignCenter().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                            t.Span("LEGACYVAULT - Handover minutes - Page ");
                            t.CurrentPageNumber();
                            t.Span("/");
                            t.TotalPages();
                        });

                        footer.Item().AlignCenter()
                            .Text("SHA-256: " + contentSha256)
                            .FontSize(6).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();
        }

        private static void Heading(ColumnDescriptor column, string text)
        {
            column.Item().PaddingTop(6)
                .BorderBottom(0.5f).BorderColor(Line).PaddingBottom(2)
                .Text(text).Bold().FontSize(11).FontColor(Brand);
        }

        private static void Info(ColumnDescriptor column, string label, string value)
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(150).Text(label).FontColor(Colors.Grey.Darken2);
                row.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "-" : value);
            });
        }

        private static IContainer HeaderCell(IContainer container)
            => container.Background("#EEF5F2").Border(0.5f).BorderColor(Line)
                .Padding(5).DefaultTextStyle(x => x.Bold());

        private static IContainer BodyCell(IContainer container)
            => container.Border(0.5f).BorderColor(Line).Padding(5);

        private static void SignatureBlock(
            IContainer container,
            string title,
            IReadOnlyList<MinutesSignatureLine> signers)
        {
            container.Column(column =>
            {
                column.Spacing(6);
                column.Item().Text(title).Bold().FontSize(10);
                column.Item().Text("(Sign and write full name)").Italic().FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                if (signers.Count == 0)
                {
                    column.Item().Text("-").FontColor(Colors.Grey.Medium);
                    return;
                }

                foreach (var signer in signers)
                {
                    var signed = signer.Status == "SIGNED" && signer.SignedAt != null;

                    column.Item().Border(0.5f).BorderColor(Line).Padding(8).Column(box =>
                    {
                        box.Spacing(3);
                        box.Item().Text(A(signer.SignerName)).Bold();

                        box.Item().Text(signed
                                ? "Status: Signed on " + signer.SignedAt!.Value.UtcDateTime
                                    .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
                                : "Status: Not signed")
                            .FontColor(signed ? Brand : Colors.Grey.Darken2);

                        if (!string.IsNullOrEmpty(signer.SignatureHash))
                        {
                            box.Item().Text("Signature hash: " + signer.SignatureHash)
                                .FontSize(7).FontColor(Colors.Grey.Darken1);
                        }
                    });
                }
            });
        }
    }
}
