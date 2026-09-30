namespace ApiTestRunner;

public sealed class CliOptions
{
    public string PlanPath { get; private set; } = Path.Combine("testplan", "plan.json");
    public string? BaseUrl { get; private set; }
    public string LogDir { get; private set; } = "logs";
    public bool StopOnFail { get; private set; }
    public bool ShowHelp { get; private set; }

    public const string Usage = """
        Usage: dotnet run -- [options]

          --plan <file>       Test plan JSON (default: testplan/plan.json)
          --base-url <url>    Override baseUrl from the plan (e.g. https://localhost:7080)
          --log-dir <dir>     Where to write run-*.log / run-*.json (default: logs)
          --stop-on-fail      Stop at the first failing step
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
                case "-h":
                case "--help": o.ShowHelp = true; break;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }
        return o;
    }
}
