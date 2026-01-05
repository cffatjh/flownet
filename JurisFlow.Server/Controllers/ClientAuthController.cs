using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using BCrypt.Net;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/client")]
    [ApiController]
    public class ClientAuthController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuditLogger _auditLogger;

        public ClientAuthController(JurisFlowDbContext context, IConfiguration configuration, AuditLogger auditLogger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        public class ClientLoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> ClientLogin([FromBody] ClientLoginDto loginDto)
        {
            Console.WriteLine($"[CLIENT LOGIN ATTEMPT] Email: {loginDto.Email}");

            if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == loginDto.Email);

            if (client == null)
            {
                Console.WriteLine($"[CLIENT LOGIN FAILED] Client not found: {loginDto.Email}");
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if portal is enabled for this client
            if (!client.PortalEnabled)
            {
                Console.WriteLine($"[CLIENT LOGIN FAILED] Portal not enabled for: {loginDto.Email}");
                return Unauthorized(new { message = "Portal access not enabled for this account" });
            }

            // Check if password hash exists
            if (string.IsNullOrEmpty(client.PasswordHash))
            {
                Console.WriteLine($"[CLIENT LOGIN FAILED] No password set for: {loginDto.Email}");
                return Unauthorized(new { message = "Invalid email or password" });
            }

            Console.WriteLine($"[CLIENT LOGIN DEBUG] Client Found: {client.Email}, PortalEnabled: {client.PortalEnabled}");

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, client.PasswordHash);
            Console.WriteLine($"[CLIENT LOGIN DEBUG] Password Verify Result: {isPasswordValid}");

            if (!isPasswordValid)
            {
                Console.WriteLine("[CLIENT LOGIN FAILED] Password mismatch");
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Update last login
            client.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateClientJwtToken(client);

            var response = new
            {
                token,
                client = new
                {
                    id = client.Id,
                    name = client.Name,
                    email = client.Email,
                    phone = client.Phone,
                    mobile = client.Mobile,
                    company = client.Company,
                    type = client.Type,
                    status = client.Status
                }
            };

            await _auditLogger.LogAsync(HttpContext, "client.login.success", "Client", client.Id, $"Email: {client.Email}");

            return Ok(response);
        }

        private string GenerateClientJwtToken(Client client)
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
                new Claim(JwtRegisteredClaimNames.Sub, client.Id),
                new Claim(JwtRegisteredClaimNames.Email, client.Email),
                new Claim(ClaimTypes.Role, "Client"),
                new Claim("role", "Client"),
                new Claim("clientId", client.Id)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
