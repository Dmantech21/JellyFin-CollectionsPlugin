namespace Jellyfin.Plugin.SmartCollections.Models;

/// <summary>
/// A user-defined rule for creating a theme-based collection.
/// </summary>
public class ThemeCollectionRule
{
    /// <summary>Display name for the collection.</summary>
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    /// <summary>Minimum number of movies required to create the collection.</summary>
    public int MinMovieCount { get; set; } = 2;

    /// <summary>
    /// Comma-separated keywords matched against the movie's title (case-insensitive).
    /// </summary>
    public string TitleKeywords { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated keywords matched against the movie's genres (case-insensitive).
    /// </summary>
    public string GenreKeywords { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated keywords matched against the movie's tags (case-insensitive).
    /// </summary>
    public string TagKeywords { get; set; } = string.Empty;

    /// <summary>
    /// "Any" (default) — movie matches if any single keyword condition is met.
    /// "All" — movie must satisfy every non-empty keyword group.
    /// </summary>
    public string MatchMode { get; set; } = "Any";
}
