namespace Jellyfin.Plugin.SmartCollections.Models;

/// <summary>
/// Persisted state for tracking plugin-managed collections.
/// Stored as JSON in the plugin data directory.
/// </summary>
public class PluginState
{
    /// <summary>
    /// Collections currently managed by this plugin.
    /// Key = rule key ("series:{name}" or "theme:{name}").
    /// Value = collection details including Jellyfin item ID and tracked movie IDs.
    /// </summary>
    public Dictionary<string, ManagedCollectionInfo> ManagedCollections { get; set; } = new();

    /// <summary>
    /// Rule keys of collections the user explicitly deleted.
    /// The plugin will never recreate collections whose rule key appears here.
    /// </summary>
    public HashSet<string> UserDeletedCollectionKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ManagedCollectionInfo
{
    public Guid CollectionId { get; set; }

    /// <summary>Movie item IDs currently tracked as members of this collection.</summary>
    public List<Guid> ItemIds { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
}
