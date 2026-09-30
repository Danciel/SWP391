using System.Text.Json;
using ApiTestRunner;

CliOptions opts;
try
{
    opts = CliOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(CliOptions.Usage);
    return 2;
}

if (opts.ShowHelp)
{
    Console.WriteLine(CliOptions.Usage);
    return 0;
}

if (!File.Exists(opts.PlanPath))
{
    Console.Error.WriteLine($"Plan file not found: {Path.GetFullPath(opts.PlanPath)}");
    Console.Error.WriteLine("Run from the ApiTestRunner folder or pass --plan <file>.");
    return 2;
}

TestPlan plan;
try
{
    plan = JsonSerializer.Deserialize<TestPlan>(File.ReadAllText(opts.PlanPath), JsonOpts.Read)
           ?? throw new JsonException("Plan is empty.");
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Invalid plan file: {ex.Message}");
    return 2;
}

Masker.Enabled = opts.MaskSecrets ?? plan.MaskSecrets;

if (opts.Ui)
    return await UiServer.RunAsync(opts);

var planFull = Path.GetFullPath(opts.PlanPath);
var runner = new TestRunner(opts.BaseUrl ?? plan.BaseUrl, Path.GetDirectoryName(planFull)!, opts.LogDir);
var results = await runner.RunAsync(plan, planFull, opts.StopOnFail);

return results.Count == plan.Steps.Count && results.All(r => r.Passed) ? 0 : 1;
