using Adaplio.Api.Models;
using Adaplio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Adaplio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("DefaultPolicy")]
public class InvitesController : ControllerBase
{
    private readonly IInviteService _inviteService;
    private readonly ILogger<InvitesController> _logger;

    public InvitesController(IInviteService inviteService, ILogger<InvitesController> logger)
    {
        _inviteService = inviteService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new invite (Trainer only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "trainer")]
    public async Task<ActionResult<InviteResponse>> CreateInvite([FromBody] InviteCreateRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int trainerId))
            {
                return Unauthorized("Invalid user ID");
            }

            var result = await _inviteService.CreateInviteAsync(trainerId, request);

            _logger.LogInformation("Invite created by trainer {TrainerId}", trainerId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invite");
            return StatusCode(500, "Failed to create invite");
        }
    }

    /// <summary>
    /// Validate an invite token (Public endpoint)
    /// </summary>
    [HttpGet("validate/{token}")]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<ActionResult<InviteValidationResponse>> ValidateInvite(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Token is required");
            }

            var result = await _inviteService.ValidateInviteAsync(token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate invite token {Token}", token);
            return StatusCode(500, "Failed to validate invite");
        }
    }

    /// <summary>
    /// Accept an invite (Authenticated users only)
    /// </summary>
    [HttpPost("accept")]
    [Authorize]
    [EnableRateLimiting("StrictPolicy")]
    public async Task<ActionResult> AcceptInvite([FromBody] InviteAcceptRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            var success = await _inviteService.AcceptInviteAsync(userId, request);

            if (!success)
            {
                return BadRequest("Failed to accept invite. The invite may be invalid, expired, or already used.");
            }

            _logger.LogInformation("Invite {Token} accepted by user {UserId}", request.Token, userId);
            return Ok(new { message = "Invite accepted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept invite {Token}", request.Token);
            return StatusCode(500, "Failed to accept invite");
        }
    }

    /// <summary>
    /// Revoke an invite (Trainer only)
    /// </summary>
    [HttpPost("revoke/{token}")]
    [Authorize(Roles = "trainer")]
    public async Task<ActionResult> RevokeInvite(string token)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int trainerId))
            {
                return Unauthorized("Invalid user ID");
            }

            var success = await _inviteService.RevokeInviteAsync(trainerId, token);

            if (!success)
            {
                return NotFound("Invite not found or already redeemed");
            }

            _logger.LogInformation("Invite {Token} revoked by trainer {TrainerId}", token, trainerId);
            return Ok(new { message = "Invite revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke invite {Token}", token);
            return StatusCode(500, "Failed to revoke invite");
        }
    }

    /// <summary>
    /// Get trainer's invites (Trainer only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "trainer")]
    public async Task<ActionResult<List<Invite>>> GetInvites()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int trainerId))
            {
                return Unauthorized("Invalid user ID");
            }

            var invites = await _inviteService.GetTrainerInvitesAsync(trainerId);
            return Ok(invites);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invites for trainer");
            return StatusCode(500, "Failed to get invites");
        }
    }
}