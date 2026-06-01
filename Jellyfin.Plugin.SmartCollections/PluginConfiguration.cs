using System.Text.Json;
using Jellyfin.Plugin.SmartCollections.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SmartCollections;

public class PluginConfiguration : BasePluginConfiguration
{
    // ── Series collections ────────────────────────────────────────────────────

    /// <summary>Auto-create collections for movie series (uses TMDb CollectionName metadata).</summary>
    public bool EnableSeriesCollections { get; set; } = true;

    /// <summary>Minimum number of movies in a series before a collection is created.</summary>
    public int SeriesMinMovieCount { get; set; } = 2;

    // ── Theme collections ─────────────────────────────────────────────────────

    /// <summary>Auto-create collections based on user-defined keyword/genre rules.</summary>
    public bool EnableThemeCollections { get; set; } = true;

    /// <summary>JSON-serialized List&lt;ThemeCollectionRule&gt;.</summary>
    public string ThemeCollectionRulesJson { get; set; } = BuildDefaultRulesJson();

    // ── Scheduler ─────────────────────────────────────────────────────────────

    /// <summary>How often the sync task runs automatically (hours).</summary>
    public int SyncIntervalHours { get; set; } = 24;

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
        catch
        {
            return new List<ThemeCollectionRule>();
        }
    }

    private static string BuildDefaultRulesJson()
    {
        var defaults = new List<ThemeCollectionRule>
        {
            new()
            {
                Name = "Christmas Movies",
                Enabled = true,
                MinMovieCount = 2,
                TitleKeywords = "christmas,xmas",
                GenreKeywords = "",
                TagKeywords = "christmas,holiday",
                MatchMode = "Any"
            },
            new()
            {
                Name = "Halloween Movies",
                Enabled = true,
                MinMovieCount = 2,
                TitleKeywords = "halloween",
                GenreKeywords = "",
                TagKeywords = "halloween",
                MatchMode = "Any"
            },
            new()
            {
                Name = "DC Extended Universe",
                Enabled = false,
                MinMovieCount = 2,
                TitleKeywords = "",
                GenreKeywords = "",
                TagKeywords = "dceu,dc extended universe",
                MatchMode = "Any"
            }
        };

        return JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
    }
}
