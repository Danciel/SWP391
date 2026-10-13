using SWP301BETASK.Features.WillManagement.Contracts;

namespace SWP301BETASK.Features.WillManagement.Dms
{
    /// <summary>Checks for overdue check-ins every hour and triggers the handover when MaxMissed is reached.</summary>
    public class DmsTriggerWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<DmsTriggerWorker> _logger;

        public DmsTriggerWorker(IServiceScopeFactory scopes, ILogger<DmsTriggerWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // small delay so a first run does not race with migrations at startup
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var dms = scope.ServiceProvider.GetRequiredService<IDmsService>();
                    var triggered = await dms.EvaluateOverdueAsync(DateTime.UtcNow, stoppingToken);
                    if (triggered > 0) _logger.LogInformation("DMS triggered handover for {Count} will(s)", triggered);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DMS evaluation failed");
                }

                try { await Task.Delay(Interval, stoppingToken); } catch (OperationCanceledException) { return; }
            }
        }
    }
}
