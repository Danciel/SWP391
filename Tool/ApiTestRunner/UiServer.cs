using System.Diagnostics;
using System.Net.Security;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ApiTestRunner;

/// <summary>
/// `dotnet run -- --ui` : a tiny local web server (127.0.0.1 only) that serves ui/index.html and
/// a JSON API used by that page: read/save the plan, change the target base URL, run tests with live
/// progress, and browse old runs in logs/. It reuses TestRunner, so CLI and UI behave identically.
/// </summary>
public static class UiServer
{
    private static readonly JsonSerializerOptions Api = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Used when writing plan.json back to disk: readable, and quotes are not turned into \u0022.
    private static readonly JsonSerializerOptions PlanWrite = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly SemaphoreSlim RunLock = new(1, 1);
    private static readonly Regex RunFileName = new(@"^run-[0-9\-]+\.json$", RegexOptions.Compiled);

    private static readonly string[] Methods = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

    // Ports commonly used by ASP.NET / other local dev servers, probed by "Detect".
    private static readonly int[] CommonPorts =
    {
        5000, 5001, 5002, 5003, 5010, 5080, 5081, 5100, 5200, 5240, 5266, 5300,
        7000, 7001, 7002, 7080, 7081, 7100, 7200, 7238, 7266, 8000, 8080, 8081, 8888, 3000
    };

    private static readonly HttpClient Probe = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = (req, _, _, errors) =>
            errors == SslPolicyErrors.None || req.RequestUri?.IsLoopback == true
    }) { Timeout = TimeSpan.FromSeconds(5) };

    public sealed class SavePayload
    {
        public string? BaseUrl { get; set; }
        public List<TestStep>? Steps { get; set; }
        public Dictionary<string, string>? Bodies { get; set; }
    }

    public sealed class RunRequest
    {
        public string? BaseUrl { get; set; }
        public bool StopOnFail { get; set; }
        /// <summary>Null = use plan.json "maskSecrets".</summary>
        public bool? MaskSecrets { get; set; }
        /// <summary>Run only the first N steps ("run up to here"). Null/0 = all.</summary>
        public int? UpTo { get; set; }
    }

    public sealed class ProbeRequest
    {
        public string? BaseUrl { get; set; }
    }

    public static async Task<int> RunAsync(CliOptions opts)
    {
        var planFull = Path.GetFullPath(opts.PlanPath);
        var planDir = Path.GetDirectoryName(planFull)!;
        var logDir = Path.GetFullPath(opts.LogDir);
        var url = $"http://localhost:{opts.Port}";
        var backedUp = false;

        string indexHtml;
        using (var s = typeof(UiServer).Assembly.GetManifestResourceStream("index.html")
                       ?? throw new InvalidOperationException("ui/index.html is not embedded in the build."))
        using (var r = new StreamReader(s, Encoding.UTF8))
            indexHtml = r.ReadToEnd();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{opts.Port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        var app = builder.Build();

        // ---- helpers -------------------------------------------------------------------------
        IResult Json(object o, int status = 200) =>
            Results.Text(JsonSerializer.Serialize(o, Api), "application/json", Encoding.UTF8, status);

        IResult Error(string message, int status = 400) => Json(new { error = message }, status);

        TestPlan LoadPlan() =>
            JsonSerializer.Deserialize<TestPlan>(File.ReadAllText(planFull), JsonOpts.Read)
            ?? throw new JsonException("Plan is empty.");

        string Rel(string full) => Path.GetRelativePath(planDir, full).Replace('\\', '/');

        // Only *.txt files inside the plan folder may be read/written through the API.
        bool TryResolve(string rel, out string full)
        {
            full = Path.GetFullPath(Path.Combine(planDir, rel));
            var root = planDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                   && full.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsHttpUrl(string? s) =>
            Uri.TryCreate(s, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

        static async Task<(bool ok, int? status, string? error, long ms)> Ping(string baseUrl, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                using var resp = await Probe.GetAsync(baseUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                return (true, (int)resp.StatusCode, null, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                return (false, null, "timed out", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return (false, null, msg, sw.ElapsedMilliseconds);
            }
        }

        // ---- guard: localhost only + CSRF protection -----------------------------------------
        app.Use(async (ctx, next) =>
        {
            var host = ctx.Request.Host.Host;
            if (host != "localhost" && host != "127.0.0.1")
            {
                ctx.Response.StatusCode = 403;
                return;
            }
            // A custom header forces a CORS preflight, which we never answer -> other websites can't call the API.
            if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Request.Method != "GET"
                && !ctx.Request.Headers.ContainsKey("X-ATR"))
            {
                ctx.Response.StatusCode = 403;
                return;
            }
            await next();
        });

        // ---- static page ---------------------------------------------------------------------
        app.MapGet("/", () => Results.Content(indexHtml, "text/html; charset=utf-8"));

        // ---- plan ----------------------------------------------------------------------------
        app.MapGet("/api/plan", () =>
        {
            TestPlan plan;
            try { plan = LoadPlan(); }
            catch (Exception ex) { return Error($"Cannot read plan: {ex.Message}", 500); }

            var bodies = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var bodiesDir = Path.Combine(planDir, "bodies");
            if (Directory.Exists(bodiesDir))
                foreach (var f in Directory.GetFiles(bodiesDir, "*.txt", SearchOption.AllDirectories))
                    bodies[Rel(f)] = File.ReadAllText(f);

            foreach (var st in plan.Steps.Where(x => !string.IsNullOrWhiteSpace(x.BodyFile)))
            {
                var rel = st.BodyFile!.Replace('\\', '/');
                if (!bodies.ContainsKey(rel) && TryResolve(rel, out var full) && File.Exists(full))
                    bodies[rel] = File.ReadAllText(full);
            }

            return Json(new { baseUrl = plan.BaseUrl, maskSecrets = opts.MaskSecrets ?? plan.MaskSecrets, steps = plan.Steps, bodies, planPath = planFull, logDir });
        });

        app.MapPut("/api/plan", async (HttpRequest req) =>
        {
            SavePayload? p;
            try { p = await JsonSerializer.DeserializeAsync<SavePayload>(req.Body, JsonOpts.Read); }
            catch (JsonException ex) { return Error($"Bad JSON: {ex.Message}"); }

            if (p?.Steps is null) return Error("'steps' is required.");

            for (var i = 0; i < p.Steps.Count; i++)
            {
                var st = p.Steps[i];
                st.Name = (st.Name ?? "").Trim();
                st.Method = (st.Method ?? "GET").Trim().ToUpperInvariant();
                st.Path = (st.Path ?? "").Trim();
                if (st.Name.Length == 0) return Error($"Step {i + 1}: name is required.");
                if (!Methods.Contains(st.Method)) return Error($"Step {i + 1} ({st.Name}): unknown method '{st.Method}'.");
                if (st.Path.Length == 0) return Error($"Step {i + 1} ({st.Name}): path is required.");

                st.Group = string.IsNullOrWhiteSpace(st.Group) ? null : st.Group.Trim();
                st.Description = string.IsNullOrWhiteSpace(st.Description) ? null : st.Description.Trim();
                st.BodyFile = string.IsNullOrWhiteSpace(st.BodyFile) ? null : st.BodyFile.Trim().Replace('\\', '/');
                st.Bearer = string.IsNullOrWhiteSpace(st.Bearer) ? null : st.Bearer.Trim();
                st.ExpectBodyContains = st.ExpectBodyContains?.Where(x => x.Length > 0).ToList() is { Count: > 0 } c ? c : null;
                st.ExpectBodyNotContains = st.ExpectBodyNotContains?.Where(x => x.Length > 0).ToList() is { Count: > 0 } n ? n : null;
                st.Capture = st.Capture is { Count: > 0 } cap ? cap : null;

                if (st.BodyFile is not null && !TryResolve(st.BodyFile, out _))
                    return Error($"Step {i + 1} ({st.Name}): body file must be a .txt file inside the testplan folder.");
            }

            if (p.Bodies is not null)
                foreach (var rel in p.Bodies.Keys)
                    if (!TryResolve(rel, out _))
                        return Error($"Invalid body file name '{rel}' (must be a .txt file inside the testplan folder).");

            var existing = LoadPlan();
            var baseUrl = IsHttpUrl(p.BaseUrl) ? p.BaseUrl!.TrimEnd('/') : existing.BaseUrl;

            try
            {
                if (!backedUp && File.Exists(planFull))
                {
                    File.Copy(planFull, planFull + ".bak", true); // one backup per UI session (comments in the old file are lost on save)
                    backedUp = true;
                }

                if (p.Bodies is not null)
                    foreach (var (rel, text) in p.Bodies)
                    {
                        TryResolve(rel, out var full);
                        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                        File.WriteAllText(full, text, new UTF8Encoding(false));
                    }

                var plan = new TestPlan { BaseUrl = baseUrl, MaskSecrets = existing.MaskSecrets, Steps = p.Steps };
                File.WriteAllText(planFull, JsonSerializer.Serialize(plan, PlanWrite) + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                return Error($"Could not write files: {ex.Message}", 500);
            }

            return Json(new { saved = true, baseUrl });
        });

        // ---- target server -------------------------------------------------------------------
        app.MapPost("/api/probe", async (HttpRequest req) =>
        {
            var p = await JsonSerializer.DeserializeAsync<ProbeRequest>(req.Body, JsonOpts.Read);
            if (!IsHttpUrl(p?.BaseUrl)) return Json(new { ok = false, error = "Not a valid http(s) URL." });
            var r = await Ping(p!.BaseUrl!, 4000);
            return Json(new { r.ok, r.status, r.error, r.ms });
        });

        app.MapGet("/api/detect", async () =>
        {
            var tasks = CommonPorts
                .SelectMany(port => new[] { "http", "https" }.Select(scheme => $"{scheme}://localhost:{port}"))
                .Select(async u => (url: u, r: await Ping(u, 1500)));
            var found = (await Task.WhenAll(tasks))
                .Where(x => x.r.ok)
                .Select(x => new { x.url, status = x.r.status })
                .OrderBy(x => x.url)
                .ToList();
            return Json(new { found });
        });

        // ---- run (streams one JSON object per line) ------------------------------------------
        app.MapPost("/api/run", async (HttpContext ctx) =>
        {
            if (!await RunLock.WaitAsync(0))
            {
                ctx.Response.StatusCode = 409;
                await ctx.Response.WriteAsJsonAsync(new { error = "A run is already in progress." });
                return;
            }

            try
            {
                RunRequest? rq;
                try { rq = await JsonSerializer.DeserializeAsync<RunRequest>(ctx.Request.Body, JsonOpts.Read); }
                catch (JsonException) { rq = null; }

                TestPlan plan;
                try { plan = LoadPlan(); }
                catch (Exception ex)
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsJsonAsync(new { error = $"Cannot read plan: {ex.Message}" });
                    return;
                }

                var baseUrl = string.IsNullOrWhiteSpace(rq?.BaseUrl) ? plan.BaseUrl : rq!.BaseUrl!.Trim();
                if (!IsHttpUrl(baseUrl))
                {
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.WriteAsJsonAsync(new { error = $"'{baseUrl}' is not a valid http(s) URL." });
                    return;
                }

                if (rq?.UpTo is int n && n > 0 && n < plan.Steps.Count)
                    plan.Steps = plan.Steps.Take(n).ToList();

                ctx.Response.ContentType = "application/x-ndjson; charset=utf-8";
                ctx.Response.Headers.CacheControl = "no-store";

                async Task Send(object o)
                {
                    await ctx.Response.WriteAsync(JsonSerializer.Serialize(o, Api) + "\n", ctx.RequestAborted);
                    await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
                }

                try
                {
                    Masker.Enabled = rq?.MaskSecrets ?? opts.MaskSecrets ?? plan.MaskSecrets;
                    var runner = new TestRunner(baseUrl, planDir, logDir);
                    await Send(new { type = "start", runId = runner.RunId, baseUrl, total = plan.Steps.Count });

                    var results = await runner.RunAsync(plan, planFull, rq?.StopOnFail ?? false,
                        r => Send(new { type = "step", result = r }), ctx.RequestAborted);

                    var passed = results.Count(x => x.Passed);
                    await Send(new
                    {
                        type = "done",
                        passed,
                        failed = results.Count - passed,
                        notRun = plan.Steps.Count - results.Count,
                        file = runner.LastJsonPath is null ? null : Path.GetFileName(runner.LastJsonPath)
                    });
                }
                catch (OperationCanceledException)
                {
                    // browser tab closed or "Stop" pressed - the runner already wrote what it had
                }
            }
            finally
            {
                RunLock.Release();
            }
        });

        // ---- history -------------------------------------------------------------------------
        app.MapGet("/api/runs", () =>
        {
            var items = new List<object>();
            if (Directory.Exists(logDir))
            {
                foreach (var f in Directory.GetFiles(logDir, "run-*.json").OrderByDescending(x => x).Take(60))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(f));
                        var r = doc.RootElement;
                        int I(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
                        string? S(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                        items.Add(new
                        {
                            file = Path.GetFileName(f),
                            runId = S("runId"),
                            baseUrl = S("baseUrl"),
                            startedAt = S("startedAt"),
                            total = I("total"),
                            passed = I("passed"),
                            failed = I("failed"),
                            notRun = I("notRun")
                        });
                    }
                    catch (Exception)
                    {
                        // unreadable / half-written file: skip
                    }
                }
            }
            return Json(new { runs = items });
        });

        app.MapGet("/api/runs/{name}", (string name) =>
        {
            if (!RunFileName.IsMatch(name)) return Error("Bad run name.", 404);
            var path = Path.Combine(logDir, name);
            return File.Exists(path)
                ? Results.Text(File.ReadAllText(path), "application/json", Encoding.UTF8)
                : Error("Run not found.", 404);
        });

        // ---- agent guide (AGENTS.md next to the project) -------------------------------------
        app.MapGet("/api/guide", () =>
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "AGENTS.md"),
                Path.Combine(planDir, "..", "AGENTS.md"),
                Path.Combine(AppContext.BaseDirectory, "AGENTS.md")
            };
            var found = candidates.FirstOrDefault(File.Exists);
            return found is null
                ? Error("AGENTS.md not found next to the project.", 404)
                : Json(new { text = File.ReadAllText(found) });
        });

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine($"ApiTestRunner UI: {url}   (Ctrl+C to stop)");
            Console.WriteLine($"Plan: {planFull}");
            if (opts.NoOpen) return;
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception) { Console.WriteLine("Could not open the browser automatically - open the address above."); }
        });

        try
        {
            await app.RunAsync();
            return 0;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Cannot start the UI on port {opts.Port}: {ex.Message}");
            Console.Error.WriteLine("Try another port: dotnet run -- --ui --port 5056");
            return 2;
        }
    }
}
