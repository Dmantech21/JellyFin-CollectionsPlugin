using System.Text.Json;
using Jellyfin.Plugin.SmartCollections.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SmartCollections;

public class PluginConfiguration : BasePluginConfiguration
{
    // ── Series collections ────────────────────────────────────────────────────

    public bool EnableSeriesCollections { get; set; } = true;
    public int SeriesMinMovieCount { get; set; } = 2;

    // ── Theme collections ─────────────────────────────────────────────────────

    public bool EnableThemeCollections { get; set; } = true;

    /// <summary>JSON-serialized List&lt;ThemeCollectionRule&gt;.</summary>
    public string ThemeCollectionRulesJson { get; set; } = BuildDefaultRulesJson();

    // ── Scheduler ─────────────────────────────────────────────────────────────

    public int SyncIntervalHours { get; set; } = 24;

    // ── User-deleted collections ──────────────────────────────────────────────

    /// <summary>
    /// JSON-serialized List&lt;string&gt; of rule keys (e.g. "series:John Wick Collection")
    /// that the user has explicitly deleted. The plugin will never recreate these.
    /// </summary>
    public string UserDeletedCollectionKeysJson { get; set; } = "[]";

    // ── Helpers ───────────────────────────────────────────────────────────────

    public List<ThemeCollectionRule> GetThemeCollectionRules()
    {
        if (string.IsNullOrWhiteSpace(ThemeCollectionRulesJson))
            return new List<ThemeCollectionRule>();
        try
        {
            return JsonSerializer.Deserialize<List<ThemeCollectionRule>>(ThemeCollectionRulesJson)
                   ?? new List<ThemeCollectionRule>();
        }
        catch { return new List<ThemeCollectionRule>(); }
    }

    public HashSet<string> GetUserDeletedKeys()
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(UserDeletedCollectionKeysJson)
                       ?? new List<string>();
            return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
        }
        catch { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
    }

    public void SetUserDeletedKeys(HashSet<string> keys)
    {
        UserDeletedCollectionKeysJson = JsonSerializer.Serialize(
            keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string BuildDefaultRulesJson()
    {
        var defaults = new List<ThemeCollectionRule>
        {
            new() { Name = "Christmas Movies", Enabled = true,  MinMovieCount = 2, TitleKeywords = "christmas,xmas", GenreKeywords = "", TagKeywords = "christmas,holiday", MatchMode = "Any" },
            new() { Name = "Halloween Movies", Enabled = true,  MinMovieCount = 2, TitleKeywords = "halloween",      GenreKeywords = "", TagKeywords = "halloween",         MatchMode = "Any" },
            new() { Name = "DC Extended Universe", Enabled = false, MinMovieCount = 2, TitleKeywords = "", GenreKeywords = "", TagKeywords = "dceu,dc extended universe", MatchMode = "Any" }
        };
        return JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
    }
}
