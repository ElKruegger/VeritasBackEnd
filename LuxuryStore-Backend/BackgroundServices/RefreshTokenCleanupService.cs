using LuxuryStore.Application.Interfaces;

namespace LuxuryStore_Backend.BackgroundServices;

/// <summary>
/// Every login and every refresh adds a row to RefreshTokens, and nothing
/// ever removed them: the table only grew. This background job deletes the
/// expired ones every few hours.
///
/// A BackgroundService starts with the web server (app.Run) and stops with
/// it. It never runs in the one-shot --migrate / --seed modes, which exit
/// before app.Run.
///
/// Safe with several instances: DELETE ... WHERE ExpiresAt &lt; cutoff is
/// idempotent, two instances running it at the same time just delete the
/// same rows once.
/// </summary>
public sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    // Not right at startup: let the API come up (and pass the Render
    // health check) before touching the database for housekeeping.
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            do
            {
                await PurgeOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task PurgeOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            // AppDbContext/UnitOfWork are Scoped and a BackgroundService is
            // a Singleton, so each run creates its own short-lived scope.
            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var deleted = await unitOfWork.RefreshTokens.DeleteExpiredAsync(DateTime.UtcNow, cancellationToken);
            logger.LogInformation("Refresh token cleanup: deleted {Count} expired token(s)", deleted);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Never let a failed run (database briefly unreachable...) escape:
            // an unhandled exception in a BackgroundService stops the whole
            // host, i.e. it would take the API down. Try again next tick.
            logger.LogWarning(ex, "Refresh token cleanup failed; retrying in {Interval}", Interval);
        }
    }
}
