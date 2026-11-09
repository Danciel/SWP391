using System.Globalization;
using System.Text;

namespace LegacyVault.Features.WillManagement.Services
{
    public sealed record ExecutorCsvStats(
        int? ExperienceYears, decimal? Rating, int? TotalCases, int? WonCases);

    public sealed class ExecutorCsvParseResult
    {
        public ExecutorCsvStats? Stats { get; init; }
        public List<string> Errors { get; init; } = new();
        public bool Ok => Stats != null && Errors.Count == 0;
    }

    // Reads the Executor evaluation CSV. Two layouts are accepted:
    //   1) table:      header row + one data row (the first data row is used)
    //        experience_years,rating,completed_cases,won_cases
    //        6,4.5,40,32
    //   2) key/value:  metric,value
    //        experience_years,6
    // Header names are case-insensitive; spaces, "_" and "-" are ignored.
    public static class ExecutorCsvParser
    {
        private static readonly string[] ExperienceKeys =
            { "experienceyears", "experience", "yearsofexperience", "years", "yearsexperience", "kinhnghiem" };
        private static readonly string[] RatingKeys =
            { "rating", "stars", "star", "sao", "score", "averagerating" };
        private static readonly string[] TotalKeys =
            { "completedcases", "totalcases", "cases", "casescompleted", "caseshandled", "sovuviec" };
        private static readonly string[] WonKeys =
            { "woncases", "wincases", "wincase", "wins", "won", "successfulcases", "successcases", "success", "solanthang" };

        public const string TemplateText =
            "experience_years,rating,completed_cases,won_cases\r\n6,4.5,40,32\r\n";

        public static ExecutorCsvParseResult Parse(byte[] bytes)
        {
            var errors = new List<string>();
            string text = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');

            var lines = text.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
                .ToList();

            if (lines.Count < 2)
                return Fail("The CSV needs a header row and at least one data row.");

            char delimiter = DetectDelimiter(lines[0]);
            var header = SplitLine(lines[0], delimiter).Select(Normalize).ToList();

            var values = new Dictionary<string, string>();

            int keyCol = header.FindIndex(h => h is "metric" or "key" or "field" or "name");
            int valCol = header.FindIndex(h => h is "value" or "score" or "giatri");

            if (keyCol >= 0 && valCol >= 0 && keyCol != valCol)
            {
                foreach (var line in lines.Skip(1))
                {
                    var cells = SplitLine(line, delimiter);
                    if (cells.Count > Math.Max(keyCol, valCol))
                        values[Normalize(cells[keyCol])] = cells[valCol].Trim();
                }
            }
            else
            {
                var cells = SplitLine(lines[1], delimiter);
                for (int i = 0; i < header.Count && i < cells.Count; i++)
                    values[header[i]] = cells[i].Trim();
            }

            int? years = ReadInt(values, ExperienceKeys, "experience_years", 0, 80, errors);
            decimal? rating = ReadDecimal(values, RatingKeys, "rating", 0m, 5m, errors);
            int? total = ReadInt(values, TotalKeys, "completed_cases", 0, 100_000, errors);
            int? won = ReadInt(values, WonKeys, "won_cases", 0, 100_000, errors);

            if (years == null && rating == null && total == null && won == null && errors.Count == 0)
                errors.Add("No known column found. Use: experience_years, rating, completed_cases, won_cases.");

            if (total.HasValue && won.HasValue && won > total)
                errors.Add("won_cases cannot be larger than completed_cases.");

            return errors.Count > 0
                ? new ExecutorCsvParseResult { Errors = errors }
                : new ExecutorCsvParseResult { Stats = new ExecutorCsvStats(years, rating, total, won) };

            ExecutorCsvParseResult Fail(string m) => new() { Errors = { m } };
        }

        private static string Normalize(string raw)
        {
            var sb = new StringBuilder();
            foreach (char c in raw.Trim().ToLowerInvariant())
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        private static char DetectDelimiter(string header)
        {
            int comma = header.Count(c => c == ',');
            int semi = header.Count(c => c == ';');
            int tab = header.Count(c => c == '\t');
            if (semi > comma && semi >= tab) return ';';
            if (tab > comma) return '\t';
            return ',';
        }

        private static List<string> SplitLine(string line, char delimiter)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            foreach (char c in line)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (c == delimiter && !quoted) { cells.Add(sb.ToString()); sb.Clear(); continue; }
                sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells;
        }

        private static string? Find(Dictionary<string, string> values, string[] keys)
        {
            foreach (var k in keys)
                if (values.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            return null;
        }

        private static int? ReadInt(
            Dictionary<string, string> v, string[] keys, string label, int min, int max, List<string> errors)
        {
            var raw = Find(v, keys);
            if (raw == null) return null;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < min || n > max)
            {
                errors.Add($"{label} must be a whole number from {min} to {max}.");
                return null;
            }
            return n;
        }

        private static decimal? ReadDecimal(
            Dictionary<string, string> v, string[] keys, string label, decimal min, decimal max, List<string> errors)
        {
            var raw = Find(v, keys);
            if (raw == null) return null;
            raw = raw.Replace(',', '.');
            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var n) || n < min || n > max)
            {
                errors.Add($"{label} must be a number from {min} to {max}.");
                return null;
            }
            return Math.Round(n, 2);
        }
    }
}
