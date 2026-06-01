using Jellyfin.Plugin.SmartCollections.Models;
using Jellyfin.Plugin.SmartCollections.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SmartCollections.Api;

/// <summary>
/// Provides REST endpoints consumed by the plugin config page.
/// All endpoints require administrator authentication.
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

    /// <summary>Returns the list of rule keys the user has explicitly deleted.</summary>
    [HttpGet("UserDeletedKeys")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetUserDeletedKeys(CancellationToken cancellationToken)
    {
        var state = await _stateManager.LoadStateAsync(cancellationToken).ConfigureAwait(false);
        return Ok(state.UserDeletedCollectionKeys.OrderBy(k => k));
    }

    /// <summary>Replaces the user-deleted key list (called when the admin removes a key via the UI).</summary>
    [HttpPost("UserDeletedKeys")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SetUserDeletedKeys([FromBody] List<string> keys, CancellationToken cancellationToken)
    {
        var state = await _stateManager.LoadStateAsync(cancellationToken).ConfigureAwait(false);
        state.UserDeletedCollectionKeys = new HashSet<string>(
            keys ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
        await _stateManager.SaveStateAsync(state, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Returns a summary of all plugin-managed collections.</summary>
    [HttpGet("Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetStatus(CancellationToken cancellationToken)
    {
        var state = await _stateManager.LoadStateAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new
        {
            ManagedCollections = state.ManagedCollections.Select(kv => new
            {
                Key              = kv.Key,
                CollectionId     = kv.Value.CollectionId,
                MovieCount       = kv.Value.ItemIds.Count,
                CreatedAt        = kv.Value.CreatedAt,
                LastSyncedAt     = kv.Value.LastSyncedAt
            }),
            UserDeletedKeys = state.UserDeletedCollectionKeys.OrderBy(k => k)
        });
    }
}
