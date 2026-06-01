using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SmartCollections.Models;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.Services;

/// <summary>
/// Core service that creates, updates, and self-heals movie collections.
///
/// Self-healing logic:
///   Each sync run first checks every collection ID we previously created.
///   Any that no longer exist in Jellyfin were deleted — we record the rule key
///   in PluginConfiguration as "user-deleted" and never recreate it unless the
///   user removes the key from the plugin settings page.
///   Collections that still exist are updated (new matching movies are added).
/// </summary>
public class CollectionSyncService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly StateManager _stateManager;
    private readonly ILogger<CollectionSyncService> _logger;

    public CollectionSyncService(
        ILibraryManager libraryManager,
        ICollectionManager collectionManager,
        StateManager stateManager,
        ILogger<CollectionSyncService> logger)
    {
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _stateManager = stateManager;
        _logger = logger;
    }

    public async Task SyncAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance!.Configuration;
        var state = await _stateManager.LoadStateAsync(cancellationToken).ConfigureAwait(false);

        // Phase 1 (5 %): detect user-deleted collections and persist to config
        bool configDirty = DetectUserDeletions(state, config);
        if (configDirty) Plugin.Instance.SaveConfiguration();
        progress.Report(5);
        await _stateManager.SaveStateAsync(state, cancellationToken).ConfigureAwait(false);

        // Phase 2 (5–50 %): series collections
        if (config.EnableSeriesCollections)
        {
            _logger.LogInformation("SmartCollections: syncing series collections");
            await SyncSeriesCollectionsAsync(state, config, progress, 5, 50, cancellationToken)
                .ConfigureAwait(false);
        }

        // Phase 3 (50–95 %): theme collections
        if (config.EnableThemeCollections)
        {
            _logger.LogInformation("SmartCollections: syncing theme collections");
            await SyncThemeCollectionsAsync(state, config, progress, 50, 95, cancellationToken)
                .ConfigureAwait(false);
        }

        await _stateManager.SaveStateAsync(state, cancellationToken).ConfigureAwait(false);
        progress.Report(100);
        _logger.LogInformation("SmartCollections: sync complete. Managed: {Count}, UserDeleted: {Del}",
            state.ManagedCollections.Count, config.GetUserDeletedKeys().Count);
    }

    // ── Phase 1: detect deletions ─────────────────────────────────────────────

    /// <returns>True if the config was modified and should be saved.</returns>
    private bool DetectUserDeletions(PluginState state, PluginConfiguration config)
    {
        var userDeleted = config.GetUserDeletedKeys();
        var deletedKeys = new List<string>();
        bool changed = false;

        foreach (var (key, info) in state.ManagedCollections)
        {
            var item = _libraryManager.GetItemById(info.CollectionId);
            if (item is null)
            {
                _logger.LogInformation(
                    "SmartCollections: collection '{Key}' (ID {Id}) no longer exists — marking as user-deleted",
                    key, info.CollectionId);
                userDeleted.Add(key);
                deletedKeys.Add(key);
                changed = true;
            }
        }

        foreach (var key in deletedKeys)
            state.ManagedCollections.Remove(key);

        if (changed)
            config.SetUserDeletedKeys(userDeleted);

        return changed;
    }

    // ── Phase 2: series collections ───────────────────────────────────────────

    private async Task SyncSeriesCollectionsAsync(
        PluginState state,
        PluginConfiguration config,
        IProgress<double> progress,
        double pStart, double pEnd,
        CancellationToken cancellationToken)
    {
        var movies = GetAllMovies();

        var groups = movies
            .Where(m => !string.IsNullOrWhiteSpace(m.CollectionName))
            .GroupBy(m => m.CollectionName!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() >= config.SeriesMinMovieCount)
            .ToList();

        _logger.LogInformation("SmartCollections: found {Count} series groups", groups.Count);

        for (int i = 0; i < groups.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var group = groups[i];
            var key = SeriesKey(group.Key);
            var movieIds = group.Select(m => m.Id).ToList();
            await SyncCollectionAsync(state, config, key, group.Key, movieIds, cancellationToken)
                .ConfigureAwait(false);
            progress.Report(Lerp(pStart, pEnd, (double)(i + 1) / groups.Count));
        }
    }

    // ── Phase 3: theme collections ────────────────────────────────────────────

    private async Task SyncThemeCollectionsAsync(
        PluginState state,
        PluginConfiguration config,
        IProgress<double> progress,
        double pStart, double pEnd,
        CancellationToken cancellationToken)
    {
        var rules = config.GetThemeCollectionRules().Where(r => r.Enabled).ToList();
        if (rules.Count == 0) return;

        var movies = GetAllMovies();

        for (int i = 0; i < rules.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rule = rules[i];
            var key = ThemeKey(rule.Name);

            var matchingIds = movies
                .Where(m => MatchesRule(m, rule))
                .Select(m => m.Id)
                .ToList();

            if (matchingIds.Count < rule.MinMovieCount)
            {
                _logger.LogDebug(
                    "SmartCollections: theme '{Name}' matched {Count} movies (min {Min}) — skipping",
                    rule.Name, matchingIds.Count, rule.MinMovieCount);
            }
            else
            {
                await SyncCollectionAsync(state, config, key, rule.Name, matchingIds, cancellationToken)
                    .ConfigureAwait(false);
            }

            progress.Report(Lerp(pStart, pEnd, (double)(i + 1) / rules.Count));
        }
    }

    // ── Core sync helper ──────────────────────────────────────────────────────

    private async Task SyncCollectionAsync(
        PluginState state,
        PluginConfiguration config,
        string key,
        string name,
        List<Guid> movieIds,
        CancellationToken cancellationToken)
    {
        var userDeleted = config.GetUserDeletedKeys();
        if (userDeleted.Contains(key))
        {
            _logger.LogDebug("SmartCollections: '{Key}' is user-deleted — skipping", key);
            return;
        }

        if (state.ManagedCollections.TryGetValue(key, out var info))
        {
            var newIds = movieIds.Except(info.ItemIds).ToList();
            if (newIds.Count > 0)
            {
                _logger.LogInformation("SmartCollections: adding {Count} new movies to '{Name}'", newIds.Count, name);
                try
                {
                    await _collectionManager.AddToCollectionAsync(info.CollectionId, newIds)
                        .ConfigureAwait(false);
                    info.ItemIds.AddRange(newIds);
                    info.LastSyncedAt = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SmartCollections: failed to add movies to '{Name}'", name);
                }
            }
            else
            {
                _logger.LogDebug("SmartCollections: '{Name}' is up to date", name);
                info.LastSyncedAt = DateTimeOffset.UtcNow;
            }

            return;
        }

        // Create new collection
        _logger.LogInformation("SmartCollections: creating collection '{Name}' with {Count} movies", name, movieIds.Count);
        try
        {
            var collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
            {
                Name = name,
                IsLocked = false,
                ItemIdList = Array.Empty<string>()
            }).ConfigureAwait(false);

            if (movieIds.Count > 0)
            {
                await _collectionManager.AddToCollectionAsync(collection.Id, movieIds)
                    .ConfigureAwait(false);
            }

            state.ManagedCollections[key] = new ManagedCollectionInfo
            {
                CollectionId = collection.Id,
                ItemIds = movieIds.ToList(),
                CreatedAt = DateTimeOffset.UtcNow,
                LastSyncedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("SmartCollections: created '{Name}' (ID {Id})", name, collection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmartCollections: failed to create collection '{Name}'", name);
        }
    }

    // ── Theme matching ────────────────────────────────────────────────────────

    private static bool MatchesRule(Movie movie, ThemeCollectionRule rule)
    {
        var titleKws = ParseKeywords(rule.TitleKeywords);
        var genreKws = ParseKeywords(rule.GenreKeywords);
        var tagKws   = ParseKeywords(rule.TagKeywords);

        bool titleHit = titleKws.Count > 0 &&
            titleKws.Any(k => movie.Name?.Contains(k, StringComparison.OrdinalIgnoreCase) == true);
        bool genreHit = genreKws.Count > 0 && movie.Genres != null &&
            genreKws.Any(k => movie.Genres.Any(g => g.Contains(k, StringComparison.OrdinalIgnoreCase)));
        bool tagHit = tagKws.Count > 0 && movie.Tags != null &&
            tagKws.Any(k => movie.Tags.Any(t => t.Contains(k, StringComparison.OrdinalIgnoreCase)));

        if (string.Equals(rule.MatchMode, "All", StringComparison.OrdinalIgnoreCase))
            return (titleKws.Count == 0 || titleHit) && (genreKws.Count == 0 || genreHit) && (tagKws.Count == 0 || tagHit);

        return titleHit || genreHit || tagHit;
    }

    private static List<string> ParseKeywords(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    // ── Library helpers ───────────────────────────────────────────────────────

    private List<Movie> GetAllMovies() =>
        _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            IsVirtualItem = false,
            Recursive = true
        }).OfType<Movie>().ToList();

    // ── Misc ──────────────────────────────────────────────────────────────────

    private static string SeriesKey(string name) => $"series:{name}";
    private static string ThemeKey(string name)  => $"theme:{name}";
    private static double Lerp(double a, double b, double t) => a + (b - a) * Math.Clamp(t, 0, 1);
}
