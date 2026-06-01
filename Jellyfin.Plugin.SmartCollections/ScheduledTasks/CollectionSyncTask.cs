using Jellyfin.Plugin.SmartCollections.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.ScheduledTasks;

/// <summary>
/// Jellyfin scheduled task that drives the collection sync.
/// Appears under Administration → Scheduled Tasks and can be triggered manually.
/// Default schedule: every 24 hours (configurable in plugin settings).
/// </summary>
public class CollectionSyncTask : IScheduledTask
{
    private readonly CollectionSyncService _syncService;
    private readonly ILogger<CollectionSyncTask> _logger;

    public CollectionSyncTask(CollectionSyncService syncService, ILogger<CollectionSyncTask> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public string Name        => "Smart Collections Sync";
    public string Key         => "SmartCollectionsSync";
    public string Description => "Creates and maintains movie collections based on series metadata and theme rules. Recreates any missing plugin-managed collections unless they were user-deleted.";
    public string Category    => "Smart Collections";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SmartCollections: scheduled task started");
        try
        {
            await _syncService.SyncAsync(progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SmartCollections: task was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartCollections: unhandled error during sync");
            throw;
        }
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        var intervalHours = Plugin.Instance?.Configuration.SyncIntervalHours ?? 24;
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(Math.Max(1, intervalHours)).Ticks
            }
        };
    }
}
