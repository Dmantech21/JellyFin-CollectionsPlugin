using Jellyfin.Plugin.SmartCollections.ScheduledTasks;
using Jellyfin.Plugin.SmartCollections.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SmartCollections;

/// <summary>
/// Registers plugin services with Jellyfin's DI container.
/// Jellyfin discovers this class automatically via assembly scanning.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<StateManager>();
        serviceCollection.AddSingleton<CollectionSyncService>();
        serviceCollection.AddSingleton<CollectionSyncTask>();
    }
}
