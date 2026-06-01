using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.SmartCollections;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static readonly Guid PluginGuid = new("7c78ef5d-3741-4b63-a22e-5a1f6c12f0db");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Smart Collections";
    public override Guid Id => PluginGuid;
    public override string Description => "Automatically creates and maintains movie collections based on series and themes. Self-healing: missing collections are recreated unless user-deleted.";

    public IEnumerable<PluginPageInfo> GetPages() =>
        new[]
        {
            new PluginPageInfo
            {
                Name = "SmartCollections",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
                EnableInMainMenu = false
            }
        };

    /// <summary>Path used by StateManager to store managed-collections.json.</summary>
    public string PluginDataPath => Path.Combine(DataFolderPath, "managed-collections.json");
}
