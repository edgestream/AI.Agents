using Microsoft.AspNetCore.Mvc;
using AI.Agents.Microsoft.Client;

namespace AI.Agents.Microsoft;

/// <summary>
/// API controller for retrieving token usage statistics.
/// </summary>
[ApiController]
[Route("api")]
public class TokenUsageController : ControllerBase
{
    private readonly TokenUsageStore _usageStore;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenUsageController"/>.
    /// </summary>
    public TokenUsageController(TokenUsageStore usageStore)
    {
        _usageStore = usageStore;
    }

    /// <summary>
    /// Gets the most recent token usage record.
    /// </summary>
    [HttpGet("token-usage/latest")]
    public IActionResult GetLatest()
    {
        var latest = _usageStore.GetLatest();
        
        if (latest == null)
        {
            return Ok(new { available = false });
        }
        
        return Ok(new
        {
            available = true,
            timestamp = latest.Timestamp,
            inputTokens = latest.InputTokens,
            outputTokens = latest.OutputTokens,
            totalTokens = latest.TotalTokens
        });
    }

    /// <summary>
    /// Gets recent token usage records.
    /// </summary>
    [HttpGet("token-usage/recent")]
    public IActionResult GetRecent([FromQuery] int count = 10)
    {
        var recent = _usageStore.GetRecent(count);
        
        return Ok(recent.Select(r => new
        {
            timestamp = r.Timestamp,
            inputTokens = r.InputTokens,
            outputTokens = r.OutputTokens,
            totalTokens = r.TotalTokens
        }));
    }
}
