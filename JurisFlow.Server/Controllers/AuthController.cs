using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.DTOs;
using BCrypt.Net;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuditLogger _auditLogger;

        public AuthController(JurisFlowDbContext context, IConfiguration configuration, AuditLogger auditLogger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            Console.WriteLine($"[LOGIN ATTEMPT] Email: {loginDto.Email}");

            if (!ModelState.IsValid)
            {
                 Console.WriteLine("[LOGIN FAILED] Invalid ModelState");
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null) 
            {
                 Console.WriteLine($"[LOGIN FAILED] User not found: {loginDto.Email}");
                 return Unauthorized(new { message = "Invalid credentials" });
            }

            Console.WriteLine($"[LOGIN DEBUG] User Found: {user.Email}, Hash: {user.PasswordHash}");
            
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            Console.WriteLine($"[LOGIN DEBUG] Password Verify Result: {isPasswordValid}");

            if (!isPasswordValid)
            {
                 Console.WriteLine("[LOGIN FAILED] Password mismatch");
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var mfaEnforced = _configuration.GetValue("Security:MfaEnforced", true);
            if (mfaEnforced && user.MfaEnabled && !string.IsNullOrEmpty(user.MfaSecret))
            {
                var challengeMinutes = _configuration.GetValue("Security:MfaChallengeMinutes", 10);
                var challenge = new MfaChallenge
                {
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(challengeMinutes),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
                };

                _context.MfaChallenges.Add(challenge);
                await _context.SaveChangesAsync();

                await _auditLogger.LogAsync(HttpContext, "auth.login.mfa_required", "User", user.Id, $"Email: {user.Email}");

                return Ok(new
                {
                    mfaRequired = true,
                    challengeId = challenge.Id,
                    challengeExpiresAt = challenge.ExpiresAt
                });
            }

            var session = await CreateSessionAsync(user.Id, "User");
            var token = GenerateJwtToken(user, session.Id, session.ExpiresAt);

            var response = new
            {
                token,
                session = new
                {
                    id = session.Id,
                    expiresAt = session.ExpiresAt
                },
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role
                }
            };

            await _auditLogger.LogAsync(HttpContext, "auth.login.success", "User", user.Id, $"Email: {user.Email}");

            return Ok(response);
        }

        private async Task<AuthSession> CreateSessionAsync(string userId, string subjectType)
        {
            var sessionMinutes = _configuration.GetValue("Security:SessionTimeoutMinutes", 480);
            var session = new AuthSession
            {
                UserId = userId,
                SubjectType = subjectType,
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

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim("sid", sessionId)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
