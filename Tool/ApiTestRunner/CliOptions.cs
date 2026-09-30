namespace ApiTestRunner;

public sealed class CliOptions
{
    public string PlanPath { get; private set; } = Path.Combine("testplan", "plan.json");
    public string? BaseUrl { get; private set; }
    public string LogDir { get; private set; } = "logs";
    public bool StopOnFail { get; private set; }
    public bool ShowHelp { get; private set; }
    public bool Ui { get; private set; }
    public int Port { get; private set; } = 5055;
    public bool NoOpen { get; private set; }
    public bool? MaskSecrets { get; private set; }

    public const string Usage = """
        Usage: dotnet run -- [options]

          --plan <file>       Test plan JSON (default: testplan/plan.json)
          --base-url <url>    Override baseUrl from the plan (e.g. https://localhost:7080)
          --log-dir <dir>     Where to write run-*.log / run-*.json (default: logs)
          --stop-on-fail      Stop at the first failing step
          --ui                Open the browser UI (Postman/Swagger-style) instead of running in the console
          --port <n>          Port for the UI (default: 5055)
          --no-mask           Show real passwords/tokens in logs (default comes from plan.json "maskSecrets")
          --mask              Force masking of passwords/tokens in logs
          --no-open           With --ui: do not launch the browser automatically
          -h, --help          Show this help

        Exit code: 0 = all steps passed, 1 = at least one failed, 2 = bad arguments/plan.
        """;

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length
                ? args[++i]
                : throw new ArgumentException($"Option '{args[i]}' needs a value.");

            switch (args[i])
            {
                case "--plan": o.PlanPath = Next(); break;
                case "--base-url": o.BaseUrl = Next(); break;
                case "--log-dir": o.LogDir = Next(); break;
                case "--stop-on-fail": o.StopOnFail = true; break;
                case "--ui": o.Ui = true; break;
                case "--no-open": o.NoOpen = true; break;
                case "--no-mask": o.MaskSecrets = false; break;
                case "--mask": o.MaskSecrets = true; break;
                case "--port":
                {
                    if (!int.TryParse(Next(), out var port) || port is < 1 or > 65535)
                        throw new ArgumentException("--port needs a number between 1 and 65535.");
                    o.Port = port;
                    break;
                }
                case "-h":
                case "--help": o.ShowHelp = true; break;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }
        return o;
    }
}
