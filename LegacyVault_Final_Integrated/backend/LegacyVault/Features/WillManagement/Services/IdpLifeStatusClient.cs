using System.Net;
using System.Text.Json;
using LegacyVault.Data;
using LegacyVault.Features.WillManagement.Contracts;
using Microsoft.EntityFrameworkCore;

namespace LegacyVault.Features.WillManagement.Services
{
    // D05/R10: reads LifeStatus from the separate MockIdpApi (read-only, HTTP).
    //   Step 1: GET api/idp-users/search?email=...  -> account id
    //   Step 2: GET api/idp-users/{id}              -> lifeStatus
    // Base URL comes from configuration "Idp:BaseUrl" (default http://localhost:5100).
    // Any doubt (no email, not found, several accounts, IdP down or slow,
    // unknown value) answers UNKNOWN. This client never invents DECEASED.
    public class IdpLifeStatusClient : ILifeStatusClient
    {
        private static readonly HashSet<string> Known =
            new(StringComparer.OrdinalIgnoreCase)
            { "ALIVE", "UNKNOWN", "PENDING", "DECEASED" };

        private readonly HttpClient _http;
        private readonly LegacyVaultDbContext _db;
        private readonly ILogger<IdpLifeStatusClient> _logger;

        public IdpLifeStatusClient(
            HttpClient http,
            LegacyVaultDbContext db,
            ILogger<IdpLifeStatusClient> logger)
        {
            _http = http;
            _db = db;
            _logger = logger;
        }

        public async Task<string> GetStatusAsync(
            Guid ownerAccountId,
            CancellationToken ct)
        {
            try
            {
                var email = await _db.Accounts
                    .AsNoTracking()
                    .Where(x => x.Id == ownerAccountId)
                    .Select(x => x.Email)
                    .FirstOrDefaultAsync(ct);

                if (string.IsNullOrWhiteSpace(email))
                    return "UNKNOWN";

                // Step 1: email -> id. The IdP answers one object (a list is
                // tolerated too); anything but exactly one account is doubt.
                var searchJson = await GetJsonAsync(
                    "api/idp-users/search?email=" +
                    Uri.EscapeDataString(email.Trim().ToLowerInvariant()),
                    ct);

                if (searchJson == null)
                    return "UNKNOWN";

                var ids = ExtractIds(searchJson.Value);
                if (ids.Count != 1)
                {
                    _logger.LogWarning(
                        "LifeStatus: IdP returned {Count} accounts for owner {OwnerId}; answering UNKNOWN.",
                        ids.Count, ownerAccountId);
                    return "UNKNOWN";
                }

                // Step 2: id -> lifeStatus (source of truth).
                var detailJson = await GetJsonAsync(
                    "api/idp-users/" + ids[0], ct);

                if (detailJson == null)
                    return "UNKNOWN";

                var value = ReadString(detailJson.Value, "lifeStatus");

                return value != null && Known.Contains(value.Trim())
                    ? value.Trim().ToUpperInvariant()
                    : "UNKNOWN";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // IdP down, timeout, bad JSON ... never block or fake a death.
                _logger.LogWarning(ex,
                    "LifeStatus lookup failed for owner {OwnerId}; answering UNKNOWN.",
                    ownerAccountId);
                return "UNKNOWN";
            }
        }

        // Returns null when the IdP says "not found" or answers an error.
        private async Task<JsonElement?> GetJsonAsync(
            string relativeUrl, CancellationToken ct)
        {
            using var response = await _http.GetAsync(relativeUrl, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "LifeStatus: IdP answered {Status} for {Url}.",
                    (int)response.StatusCode, relativeUrl);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.Clone();
        }

        private static List<Guid> ExtractIds(JsonElement element)
        {
            var result = new List<Guid>();

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    AddId(item, result);
            }
            else
            {
                AddId(element, result);
            }

            return result.Distinct().ToList();
        }

        private static void AddId(JsonElement item, List<Guid> result)
        {
            if (Guid.TryParse(ReadString(item, "id"), out var id))
                result.Add(id);
        }

        // Case-insensitive property read (the IdP answers camelCase JSON).
        private static string? ReadString(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name,
                        StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind != JsonValueKind.Null)
                {
                    return property.Value.ToString();
                }
            }

            return null;
        }
    }
}
