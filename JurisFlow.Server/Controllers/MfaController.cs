using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/mfa")]
    [ApiController]
    public class MfaController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuditLogger _auditLogger;

        public MfaController(JurisFlowDbContext context, IConfiguration configuration, AuditLogger auditLogger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        [Authorize]
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var backupCount = GetBackupCodes(user).Count;

            return Ok(new
            {
                enabled = user.MfaEnabled,
                hasSecret = !string.IsNullOrEmpty(user.MfaSecret),
                backupCodesRemaining = backupCount
            });
        }

        [Authorize]
        [HttpPost("setup")]
        public async Task<IActionResult> SetupMfa()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            var secret = TotpService.GenerateSecret();
            user.MfaSecret = secret;
            user.MfaEnabled = false;
            user.MfaVerifiedAt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var issuer = _configuration["Jwt:Issuer"] ?? "JurisFlow";
            var accountName = user.Email;
            var otpauthUri = TotpService.BuildOtpauthUri(issuer, accountName, secret);

            await _auditLogger.LogAsync(HttpContext, "auth.mfa.setup", "User", user.Id, "MFA setup initiated");

            return Ok(new
            {
                secret,
                otpauthUri,
                issuer,
                accountName
            });
        }

        [Authorize]
        [HttpPost("enable")]
        public async Task<IActionResult> EnableMfa([FromBody] MfaCodeDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();
            if (string.IsNullOrEmpty(user.MfaSecret))
            {
                return BadRequest(new { message = "MFA secret not configured. Run setup first." });
            }

            if (!TotpService.VerifyCode(user.MfaSecret, dto.Code))
            {
                return BadRequest(new { message = "Invalid authentication code." });
            }

            var backupCodes = TotpService.GenerateBackupCodes();
            user.MfaBackupCodesJson = JsonSerializer.Serialize(HashBackupCodes(backupCodes));
            user.MfaEnabled = true;
            user.MfaVerifiedAt = DateTime.UtcNow;
            user.MfaLastUsedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "auth.mfa.enabled", "User", user.Id, "MFA enabled");

            return Ok(new { backupCodes });
        }

        [Authorize]
        [HttpPost("disable")]
        public async Task<IActionResult> DisableMfa([FromBody] MfaCodeDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            if (!VerifyCodeOrBackup(user, dto.Code))
            {
                return BadRequest(new { message = "Invalid authentication code." });
            }

            user.MfaEnabled = false;
            user.MfaSecret = null;
            user.MfaBackupCodesJson = null;
            user.MfaVerifiedAt = null;
            user.MfaLastUsedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "auth.mfa.disabled", "User", user.Id, "MFA disabled");

            return Ok(new { message = "MFA disabled" });
        }

        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ChallengeId))
            {
                return BadRequest(new { message = "Challenge ID is required." });
            }

            var challenge = await _context.MfaChallenges.FindAsync(dto.ChallengeId);
            if (challenge == null || challenge.IsUsed || challenge.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Invalid or expired MFA challenge." });
            }

            var user = await _context.Users.FindAsync(challenge.UserId);
            if (user == null || string.IsNullOrEmpty(user.MfaSecret) || !user.MfaEnabled)
            {
                return Unauthorized(new { message = "MFA not enabled for this account." });
            }

            if (!VerifyCodeOrBackup(user, dto.Code))
            {
                await _auditLogger.LogAsync(HttpContext, "auth.mfa.failed", "User", user.Id, "MFA verification failed");
                return Unauthorized(new { message = "Invalid authentication code." });
            }

            challenge.IsUsed = true;
            challenge.VerifiedAt = DateTime.UtcNow;
            user.MfaLastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var session = await CreateSessionAsync(user.Id);
            var token = GenerateJwtToken(user, session.Id, session.ExpiresAt);

            await _auditLogger.LogAsync(HttpContext, "auth.mfa.success", "User", user.Id, "MFA verified");

            return Ok(new
            {
                token,
                session = new { id = session.Id, expiresAt = session.ExpiresAt },
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role
                }
            });
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        }

        private async Task<AuthSession> CreateSessionAsync(string userId)
        {
            var sessionMinutes = _configuration.GetValue("Security:SessionTimeoutMinutes", 480);
            var session = new AuthSession
            {
                UserId = userId,
                SubjectType = "User",
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(sessionMinutes),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
            };

            _context.AuthSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        private string GenerateJwtToken(User user, string sessionId, DateTime expiresAt)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is missing in configuration.");
            }

            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim("sid", sessionId)
            };

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        private static List<string> HashBackupCodes(IEnumerable<string> codes)
        {
            return codes.Select(code => BCrypt.Net.BCrypt.HashPassword(code)).ToList();
        }

        private static List<string> GetBackupCodes(User user)
        {
            if (string.IsNullOrWhiteSpace(user.MfaBackupCodesJson))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(user.MfaBackupCodesJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private bool VerifyCodeOrBackup(User user, string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            if (!string.IsNullOrEmpty(user.MfaSecret) && TotpService.VerifyCode(user.MfaSecret, code))
            {
                return true;
            }

            var backupCodes = GetBackupCodes(user);
            if (backupCodes.Count == 0) return false;

            var matching = backupCodes.FirstOrDefault(hash => BCrypt.Net.BCrypt.Verify(code, hash));
            if (matching == null) return false;

            backupCodes.Remove(matching);
            user.MfaBackupCodesJson = JsonSerializer.Serialize(backupCodes);
            return true;
        }
    }

    public class MfaCodeDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class MfaVerifyDto
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
