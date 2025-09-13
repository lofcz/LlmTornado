using A2A;
using LlmTornado.A2A.Hosting.Models;
using LlmTornado.A2A.Hosting.Services;
using LlmTornado.Agents.ChatRuntime;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace LlmTornado.A2A.Hosting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class A2ADispatchController : ControllerBase
{
    private readonly ILogger<A2ADispatchController> _logger;
    private readonly IA2ADispatchService _dispatchService;

    public A2ADispatchController(
       ILogger<A2ADispatchController> logger,
       IA2ADispatchService dispatchService)
    {
        _logger = logger;
        _dispatchService = dispatchService;
    }

    /// <summary>
    /// Creates a new ChatRuntime instance
    /// </summary>
    /// <param name="request">Runtime creation request</param>
    /// <returns>Runtime creation response with ID</returns>
    [HttpPost("create")]
    public async Task<ActionResult<ServerCreationResult>> CreateRuntime([FromBody] ServerCreationRequest request)
    {
        try
        {
            ServerCreationResult result = await _dispatchService.DispatchServerAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create server");
            return StatusCode(500, new { error = "Failed to create server", details = ex.Message });
        }
    }

    [HttpDelete("delete/{serverId}")]
    public async Task<ActionResult> DeleteRuntime(string serverId)
    {
        try
        {
            await _dispatchService.RemoveServerAsync(serverId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete server");
            return StatusCode(500, new { error = "Failed to delete server", details = ex.Message });
        }
    }

    [HttpGet("configurations")]
    public async Task<ActionResult<string[]>> GetServerConfigurations()
    {
        try
        {
            var cards = _dispatchService.GetServerConfigurations();
            return Ok(cards.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server configurations");
            return StatusCode(500, new { error = "Failed to get server configurations", details = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<string[]>> GetActiveServers()
    {
        try
        {
            var activeServers = _dispatchService.ListServers();
            return Ok(activeServers.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active servers");
            return StatusCode(500, new { error = "Failed to get active servers", details = ex.Message });
        }
    }

    [HttpGet("status/{serverId}")]
    public async Task<ActionResult<ServerStatus>> GetServerStatus(string serverId)
    {
        try
        {
            var status = await _dispatchService.GetServerStatusAsync(serverId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get server status");
            return StatusCode(500, new { error = "Failed to get server status", details = ex.Message });
        }
    }
}
