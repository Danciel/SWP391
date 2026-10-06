using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiTestRunner;

public sealed class TestRunner
{
    private static readonly Regex VarPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _planDir;
    private readonly string _logDir;
    private readonly Dictionary<string, string> _vars = new();

    public string RunId => _vars["runId"];

    /// <summary>Path of the run-*.json written by the last RunAsync call.</summary>
    public string? LastJsonPath { get; private set; }

    public TestRunner(string baseUrl, string planDir, string logDir)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _planDir = planDir;
        _logDir = logDir;

        // Accept the ASP.NET dev certificate on localhost; never follow redirects
        // (an http->https redirect would silently drop the Authorization header).
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (req, _, _, errors) =>
                errors == SslPolicyErrors.None || req.RequestUri?.IsLoopback == true
        };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        // Built-in variables usable in paths/bodies: {{runId}} (7 digits, unique per run), {{timestamp}}
        _vars["runId"] = Random.Shared.Next(1_000_000, 10_000_000).ToString();
        _vars["timestamp"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }

    public async Task<List<StepResult>> RunAsync(
        TestPlan plan, string planPath, bool stopOnFail,
        Func<StepResult, Task>? onStep = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_logDir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var logPath = Path.Combine(_logDir, $"run-{stamp}.log");
        var jsonPath = Path.Combine(_logDir, $"run-{stamp}.json");
        LastJsonPath = jsonPath;
        var started = DateTime.Now;

        await using var log = new StreamWriter(logPath, false, new UTF8Encoding(false)) { AutoFlush = true };
        log.WriteLine($"=== API TEST RUN {started:yyyy-MM-dd HH:mm:ss} ===");
        log.WriteLine($"Plan    : {planPath}");
        log.WriteLine($"Base URL: {_baseUrl}");
        log.WriteLine($"RunId   : {_vars["runId"]}");
        log.WriteLine(new string('-', 78));
        log.WriteLine();

        Console.WriteLine($"Base URL: {_baseUrl} | RunId: {_vars["runId"]} | Steps: {plan.Steps.Count}");
        Console.WriteLine();

        var results = new List<StepResult>();
        try
        {
            for (var i = 0; i < plan.Steps.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var result = await ExecuteAsync(i + 1, plan.Steps[i], ct);
                results.Add(result);
                WriteStepLog(log, result);
                PrintConsole(result);
                if (onStep is not null) await onStep(result);

                if (result.Fatal)
                {
                    log.WriteLine("ABORTED: server unreachable.");
                    Console.WriteLine("Aborting: server unreachable. Is the API running and is baseUrl correct?");
                    break;
                }
                if (!result.Passed && stopOnFail)
                {
                    log.WriteLine("STOPPED: --stop-on-fail.");
                    Console.WriteLine("Stopping (--stop-on-fail).");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            log.WriteLine("CANCELLED: run was stopped from the UI.");
            Console.WriteLine("Run cancelled.");
        }

        var passed = results.Count(r => r.Passed);
        var failed = results.Count - passed;
        var skipped = plan.Steps.Count - results.Count;

        log.WriteLine(new string('=', 78));
        log.WriteLine($"SUMMARY: total={plan.Steps.Count} passed={passed} failed={failed} not-run={skipped}");
        foreach (var f in results.Where(r => !r.Passed))
            log.WriteLine($"  FAIL [{f.Index:D2}] {f.Name}: {string.Join("; ", f.Problems)}");

        Console.WriteLine();
        Console.WriteLine($"SUMMARY: passed={passed} failed={failed} not-run={skipped}");
        Console.WriteLine($"Log : {Path.GetFullPath(logPath)}");
        Console.WriteLine($"JSON: {Path.GetFullPath(jsonPath)}");

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(new
        {
            runId = _vars["runId"],
            baseUrl = _baseUrl,
            startedAt = started,
            finishedAt = DateTime.Now,
            total = plan.Steps.Count,
            passed,
            failed,
            notRun = skipped,
            steps = results
        }, JsonOpts.Write));

        return results;
    }

    private async Task<StepResult> ExecuteAsync(int index, TestStep step, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        string? url = null;
        string? requestBody = null;

        StepResult Failed(string problem, bool fatal = false) => new(
            index, step.Name, step.Method.ToUpperInvariant(), url, null, step.ExpectStatus,
            false, fatal, sw.ElapsedMilliseconds, new List<string> { problem },
            requestBody is null ? null : Masker.Mask(requestBody), null,
            new List<CheckResult> { new("setup", problem, false) });

        try
        {
            url = _baseUrl + "/" + Substitute(step.Path).TrimStart('/');
            using var req = new HttpRequestMessage(new HttpMethod(step.Method.ToUpperInvariant()), url);

            if (!string.IsNullOrWhiteSpace(step.BodyFile))
            {
                var file = Path.Combine(_planDir, step.BodyFile);
                if (!File.Exists(file)) return Failed($"Body file not found: {file}");

                requestBody = Substitute(await File.ReadAllTextAsync(file, ct));
                req.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            if (!string.IsNullOrWhiteSpace(step.Bearer))
            {
                if (!_vars.TryGetValue(step.Bearer, out var token))
                    throw new TestSetupException($"Variable '{step.Bearer}' is not set (an earlier step probably failed).");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            sw.Stop();

            var status = (int)resp.StatusCode;
            var problems = new List<string>();
            var checks = new List<CheckResult>();

            var statusOk = status == step.ExpectStatus;
            checks.Add(new("status",
                statusOk ? $"Status is {step.ExpectStatus}" : $"Status should be {step.ExpectStatus} but was {status}",
                statusOk));
            if (!statusOk) problems.Add($"expected status {step.ExpectStatus} but got {status}");

            foreach (var raw in step.ExpectBodyContains ?? new List<string>())
            {
                var needle = Substitute(raw);
                var ok = body.Contains(needle, StringComparison.Ordinal);
                checks.Add(new("contains", needle, ok));
                if (!ok) problems.Add($"response body does not contain: {needle}");
            }

            foreach (var raw in step.ExpectBodyNotContains ?? new List<string>())
            {
                var needle = Substitute(raw);
                var ok = !body.Contains(needle, StringComparison.Ordinal);
                checks.Add(new("notContains", needle, ok));
                if (!ok) problems.Add($"response body must NOT contain: {needle}");
            }

            Dictionary<string, string>? captured = null;
            if (statusOk && step.Capture is not null)
            {
                captured = new Dictionary<string, string>();
                foreach (var (name, jsonPath) in step.Capture)
                {
                    if (TryGetPath(body, jsonPath, out var value))
                    {
                        _vars[name] = value;
                        captured[name] = ShowVar(name, value);
                        checks.Add(new("capture", $"{name} <- {jsonPath}", true));
                    }
                    else
                    {
                        problems.Add($"capture '{name}': path '{jsonPath}' not found in response");
                        checks.Add(new("capture", $"{name} <- {jsonPath} (path not found)", false));
                    }
                }
            }

            var headers = new Dictionary<string, string>();
            foreach (var h in resp.Headers.Concat(resp.Content.Headers))
            {
                if (h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)) continue;
                headers[h.Key] = string.Join(", ", h.Value);
            }

            return new StepResult(
                index, step.Name, step.Method.ToUpperInvariant(), url, status, step.ExpectStatus,
                problems.Count == 0, false, sw.ElapsedMilliseconds, problems,
                requestBody is null ? null : Masker.Mask(requestBody),
                Masker.Mask(body), checks, headers, captured);
        }
        catch (TestSetupException ex)
        {
            return Failed(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return Failed($"request error: {ex.Message}", fatal: true);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failed("request timed out after 30 s");
        }
        catch (Exception ex) when (ex is UriFormatException or InvalidOperationException)
        {
            return Failed($"invalid request: {ex.Message}", fatal: true);
        }
    }

    /// <summary>Tokens are shortened before being recorded in logs / shown in the UI.</summary>
    private static string ShowVar(string name, string value) =>
        Masker.Enabled && name.Contains("token", StringComparison.OrdinalIgnoreCase) && value.Length > 16
            ? value[..12] + "..."
            : value;

    private string Substitute(string input)
    {
        var missing = new List<string>();
        var output = VarPattern.Replace(input, m =>
        {
            if (_vars.TryGetValue(m.Groups[1].Value, out var v)) return v;
            missing.Add(m.Groups[1].Value);
            return m.Value;
        });

        if (missing.Count > 0)
            throw new TestSetupException(
                $"Unresolved variable(s): {string.Join(", ", missing.Distinct())} (an earlier step probably failed).");

        return output;
    }

    private static bool TryGetPath(string json, string path, out string value)
    {
        value = "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            var el = doc.RootElement;
            foreach (var part in path.Split('.'))
            {
                if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(part, out var next))
                    return false;
                el = next;
            }

            if (el.ValueKind == JsonValueKind.Null) return false;
            value = el.ValueKind == JsonValueKind.String ? el.GetString()! : el.ToString();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void WriteStepLog(StreamWriter log, StepResult r)
    {
        log.WriteLine($"[{r.Index:D2}] {(r.Passed ? "PASS" : "FAIL")}  {r.Name}");
        log.WriteLine($"     {r.Method} {r.Url}");
        if (r.RequestBody is not null)
            log.WriteLine($"     Request body : {OneLine(r.RequestBody)}");
        log.WriteLine($"     Response     : {(r.Status?.ToString() ?? "-")} (expected {r.ExpectedStatus}) in {r.ElapsedMs} ms");
        if (r.ResponseBody is not null)
            log.WriteLine($"     Response body: {Truncate(OneLine(r.ResponseBody), 2000)}");
        foreach (var p in r.Problems)
            log.WriteLine($"     ! {p}");
        log.WriteLine();
    }

    private static void PrintConsole(StepResult r)
    {
        Console.ForegroundColor = r.Passed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write(r.Passed ? "PASS" : "FAIL");
        Console.ResetColor();
        Console.WriteLine($"  [{r.Index:D2}] {r.Name}  ({r.Status?.ToString() ?? "-"}, {r.ElapsedMs} ms)");
        foreach (var p in r.Problems)
            Console.WriteLine($"          ! {p}");
    }

    private static string OneLine(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...(truncated)";
}
