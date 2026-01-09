using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/security")]
    [ApiController]
    [Authorize(Roles = "Admin,Partner,Associate,Employee")]
    public class SecurityController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuditLogger _auditLogger;

        public SecurityController(JurisFlowDbContext context, IConfiguration configuration, AuditLogger auditLogger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                sessionTimeoutMinutes = _configuration.GetValue("Security:SessionTimeoutMinutes", 480),
                idleTimeoutMinutes = _configuration.GetValue("Security:IdleTimeoutMinutes", 60),
                mfaEnforced = _configuration.GetValue("Security:MfaEnforced", true)
            });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var currentSessionId = User.FindFirst("sid")?.Value;
            var sessions = await _context.AuthSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.LastSeenAt)
                .Select(s => new
                {
                    id = s.Id,
                    createdAt = s.CreatedAt,
                    lastSeenAt = s.LastSeenAt,
                    expiresAt = s.ExpiresAt,
                    ipAddress = s.IpAddress,
                    userAgent = s.UserAgent,
                    revokedAt = s.RevokedAt,
                    revokedReason = s.RevokedReason,
                    isCurrent = s.Id == currentSessionId
                })
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpPost("sessions/{id}/revoke")]
        public async Task<IActionResult> RevokeSession(string id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var session = await _context.AuthSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (session == null) return NotFound();

            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = "user_revoked";
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "auth.session.revoke", "AuthSession", session.Id, "User revoked session");

            return Ok(new { message = "Session revoked" });
        }

        [HttpPost("sessions/revoke-current")]
        public async Task<IActionResult> RevokeCurrentSession()
        {
            var sessionId = User.FindFirst("sid")?.Value;
            var userId = GetUserId();
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId)) return Unauthorized();

            var session = await _context.AuthSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session == null) return NotFound();

            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = "user_logout";
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "auth.session.logout", "AuthSession", session.Id, "User logged out");
            return Ok(new { message = "Session revoked" });
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        }
    }
}
