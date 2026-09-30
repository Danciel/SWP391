using System.Text.Json;

namespace ApiTestRunner;

public sealed class TestPlan
{
    public string BaseUrl { get; set; } = "https://localhost:7080";
    public List<TestStep> Steps { get; set; } = new();
}

public sealed class TestStep
{
    public string Name { get; set; } = "";
    public string Method { get; set; } = "GET";

    /// <summary>Path + query. Supports {{variables}}. Remember to URL-encode '+' as %2B.</summary>
    public string Path { get; set; } = "";

    /// <summary>.txt file (relative to the plan folder) holding the JSON request body. Supports {{variables}}.</summary>
    public string? BodyFile { get; set; }

    /// <summary>Name of a captured variable holding the JWT to send as "Authorization: Bearer".</summary>
    public string? Bearer { get; set; }

    public int ExpectStatus { get; set; } = 200;
    public List<string>? ExpectBodyContains { get; set; }
    public List<string>? ExpectBodyNotContains { get; set; }

    /// <summary>variableName -> dotted JSON path in the response, e.g. { "token": "accessToken", "id": "account.id" }.</summary>
    public Dictionary<string, string>? Capture { get; set; }
}

public sealed record StepResult(
    int Index,
    string Name,
    string Method,
    string? Url,
    int? Status,
    int ExpectedStatus,
    bool Passed,
    bool Fatal,
    long ElapsedMs,
    List<string> Problems,
    string? RequestBody,
    string? ResponseBody);

public sealed class TestSetupException(string message) : Exception(message);

public static class JsonOpts
{
    public static readonly JsonSerializerOptions Read = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static readonly JsonSerializerOptions Write = new() { WriteIndented = true };
}
