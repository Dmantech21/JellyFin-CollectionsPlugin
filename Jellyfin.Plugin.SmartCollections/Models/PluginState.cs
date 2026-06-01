namespace Jellyfin.Plugin.SmartCollections.Models;

/// <summary>
/// Runtime state persisted to managed-collections.json.
/// Tracks which Jellyfin collection IDs this plugin created so it can detect deletions.
/// User-deleted collection keys are stored in PluginConfiguration instead.
/// </summary>
public class PluginState
{
    /// <summary>
    /// Key = rule key ("series:{name}" or "theme:{name}").
    /// Value = collection details including Jellyfin item ID and tracked movie IDs.
    /// </summary>
    public Dictionary<string, ManagedCollectionInfo> ManagedCollections { get; set; } = new();
}

public class ManagedCollectionInfo
{
    public Guid CollectionId { get; set; }
    public List<Guid> ItemIds { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
}
