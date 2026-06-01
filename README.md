# Smart Collections — Jellyfin Plugin

Automatically creates and maintains movie collections without babysitting.

## Features

| Feature | Details |
|---|---|
| **Series collections** | Groups movies that share a TMDb collection name (e.g. "John Wick Collection", "Marvel Cinematic Universe"). Requires no manual configuration — uses metadata already on your movies. |
| **Theme collections** | Creates collections from user-defined rules matching title keywords, genres, or tags (e.g. "Christmas Movies", "Halloween Movies"). |
| **Self-healing** | On each sync the plugin checks every collection it created. Any that are missing are automatically recreated. |
| **User-deletion awareness** | If *you* delete a plugin-managed collection, the plugin records it as user-deleted and never recreates it — unless you explicitly restore it via the plugin settings page. |
| **Scheduled task** | Runs automatically every N hours (default 24). Can also be triggered manually from **Administration → Scheduled Tasks**. |

## Installation

### Via Jellyfin plugin catalog (recommended)

1. In Jellyfin, go to **Administration → Plugins → Repositories**
2. Click **Add**, paste the manifest URL, and save:
   ```
   https://raw.githubusercontent.com/Dmantech21/JellyFin-CollectionsPlugin/releases/manifest.json
   ```
3. Go to **Catalog**, find **Smart Collections**, and click **Install**
4. Restart Jellyfin when prompted
5. Configure the plugin under **Administration → Plugins → Smart Collections**

### Manual installation
1. Download the latest `.zip` from the [Releases](../../releases) page
2. Extract its contents into a new folder inside your Jellyfin plugins directory:
   - Linux / Docker: `<jellyfin-data>/plugins/SmartCollections_1.0.0/`
   - Windows: `%APPDATA%\Jellyfin\plugins\SmartCollections_1.0.0\`
3. Restart Jellyfin

## Building from source

Requirements: .NET 8 SDK

```bash
git clone https://github.com/dmantech21/jellyfin-collectionsplugin.git
cd jellyfin-collectionsplugin
dotnet build Jellyfin.Plugin.SmartCollections/Jellyfin.Plugin.SmartCollections.csproj -c Release
```

The DLL will be at:
```
Jellyfin.Plugin.SmartCollections/bin/Release/net8.0/Jellyfin.Plugin.SmartCollections.dll
```

## Configuration

Open **Administration → Plugins → Smart Collections**.

### Series Collections
- **Enable** — on by default. Scans every movie for its `CollectionName` metadata field (set by TMDb during library scan) and groups them.
- **Minimum movies** — only create a collection if at least N movies belong to the series (default: 2).

### Theme Collection Rules
Each rule has:
| Field | Description |
|---|---|
| Name | The collection name shown in Jellyfin |
| Title keywords | Comma-separated; matches if movie title contains any keyword (case-insensitive) |
| Genre keywords | Comma-separated; matches against movie genres |
| Tag keywords | Comma-separated; matches against movie tags |
| Min movies | Minimum matches before creating the collection |
| Mode | **Any** — movie matches if *any* keyword group has a hit. **All** — movie must match *every* non-empty keyword group. |
| On | Enable/disable this rule |

Default rules included (disabled by default except Christmas/Halloween):
- Christmas Movies
- Halloween Movies
- DC Extended Universe (example of a tag-based franchise collection)

### User-Deleted Collections
Lists all collection rule keys the plugin has learned were user-deleted. Remove a key here to let the plugin recreate that collection on the next sync.

### Status API
A read-only status endpoint is available for diagnostics:
```
GET /SmartCollections/Status
```
Returns all managed collections with movie counts and sync timestamps.

## How self-healing works

```
On each sync run:
  1. For every collection ID we previously created:
       → still exists in Jellyfin?  OK, check for new movies to add.
       → no longer exists?          Mark its rule key as "user-deleted". Never recreate.

  2. For every applicable rule (series group / theme rule):
       → rule key is user-deleted?  Skip.
       → collection exists?         Add any new matching movies.
       → collection missing?        Create it fresh with all matching movies.

  3. Save updated state to managed-collections.json in plugin data dir.
```

## Compatibility

- Jellyfin **10.9.x** (net8.0)
- Requires the **Collections** feature to be enabled in your Jellyfin library settings
