using Jellyfin.Plugin.SmartCollections.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SmartCollections.Api;

/// <summary>
/// Read-only diagnostics endpoint. Configuration (including user-deleted keys)
/// is managed via the standard Jellyfin plugin configuration API.
/// </summary>
[ApiController]
[Route("SmartCollections")]
[Authorize(Policy = "RequiresElevation")]
public class SmartCollectionsController : ControllerBase
{
    private readonly StateManager _stateManager;

    public SmartCollectionsController(StateManager stateManager)
    {
        _stateManager = stateManager;
    }

    /// <summary>Returns a summary of all plugin-managed collections and user-deleted keys.</summary>
    [HttpGet("Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetStatus(CancellationToken cancellationToken)
    {
        var state = await _stateManager.LoadStateAsync(cancellationToken).ConfigureAwait(false);
        var config = Plugin.Instance?.Configuration;

        return Ok(new
        {
            ManagedCollections = state.ManagedCollections.Select(kv => new
            {
                Key          = kv.Key,
                CollectionId = kv.Value.CollectionId,
                MovieCount   = kv.Value.ItemIds.Count,
                CreatedAt    = kv.Value.CreatedAt,
                LastSyncedAt = kv.Value.LastSyncedAt
            }),
            UserDeletedKeys = config?.GetUserDeletedKeys().OrderBy(k => k) ?? Enumerable.Empty<string>()
        });
    }
}
