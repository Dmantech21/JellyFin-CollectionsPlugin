using System.Text.Json;
using Jellyfin.Plugin.SmartCollections.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.Services;

/// <summary>
/// Reads and writes the plugin state file (managed-collections.json).
/// The state tracks which collections this plugin created and which were user-deleted.
/// </summary>
public class StateManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<StateManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public StateManager(ILogger<StateManager> logger)
    {
        _logger = logger;
    }

    private static string StatePath =>
        Plugin.Instance?.PluginDataPath
        ?? throw new InvalidOperationException("Plugin.Instance is null");

    public async Task<PluginState> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(StatePath))
                return new PluginState();

            await using var stream = File.OpenRead(StatePath);
            var state = await JsonSerializer.DeserializeAsync<PluginState>(stream, _jsonOptions, cancellationToken)
                        .ConfigureAwait(false);
            return state ?? new PluginState();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load plugin state from {Path} — starting fresh", StatePath);
            return new PluginState();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveStateAsync(PluginState state, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var dir = Path.GetDirectoryName(StatePath)!;
            Directory.CreateDirectory(dir);

            var tmp = StatePath + ".tmp";
            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, state, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Atomic replace
            File.Move(tmp, StatePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save plugin state to {Path}", StatePath);
        }
        finally
        {
            _lock.Release();
        }
    }
}
