using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using BCrypt.Net;
using System.Security.Claims;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;
        private readonly ILogger<AdminController> _logger;

        public AdminController(JurisFlowDbContext context, AuditLogger auditLogger, ILogger<AdminController> logger)
        {
            _context = context;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/admin/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = dto.Email,
                Name = dto.Name,
                Role = dto.Role ?? "Associate",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "Password123!")
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "admin.user.create", "User", user.Id, $"Created user {user.Email}");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role
            });
        }

        // PUT: api/admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Role))
                user.Role = dto.Role;
            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "admin.user.update", "User", user.Id, $"Updated user {user.Email}");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.Name,
                user.Role
            });
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent deleting self
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (user.Id == currentUserId)
            {
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "admin.user.delete", "User", id, $"Deleted user {user.Email}");

            return NoContent();
        }

        // GET: api/admin/clients
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _context.Clients
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    c.Phone,
                    c.Mobile,
                    c.Company,
                    c.Type,
                    c.Status,
                    c.PortalEnabled,
                    c.Address,
                    c.City,
                    c.State,
                    c.ZipCode,
                    c.Country,
                    c.TaxId,
                    c.Notes
                })
                .ToListAsync();

            return Ok(clients);
        }

        // PUT: api/admin/clients/{id}
        [HttpPut("clients/{id}")]
        public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientDto dto)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client not found" });
            }

            if (!string.IsNullOrEmpty(dto.Name))
                client.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email))
                client.Email = dto.Email;
            if (dto.Phone != null)
                client.Phone = dto.Phone;
            if (dto.PortalEnabled.HasValue)
                client.PortalEnabled = dto.PortalEnabled.Value;
            if (!string.IsNullOrEmpty(dto.Status))
                client.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.Company))
                client.Company = dto.Company;
            if (!string.IsNullOrEmpty(dto.Type))
                client.Type = dto.Type;
            if (!string.IsNullOrEmpty(dto.Mobile))
                client.Mobile = dto.Mobile;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "admin.client.update", "Client", client.Id, $"Updated client {client.Email}");

            return Ok(client);
        }

        // DELETE: api/admin/clients/{id}
        [HttpDelete("clients/{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client not found" });
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "admin.client.delete", "Client", id, $"Deleted client {client.Email}");

            return NoContent();
        }

        // POST: api/admin/billing-locks
        [HttpPost("billing-locks")]
        public async Task<IActionResult> CreateBillingLock([FromBody] CreateBillingLockDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.PeriodStart) || string.IsNullOrWhiteSpace(dto.PeriodEnd))
            {
                return BadRequest(new { message = "PeriodStart and PeriodEnd are required (yyyy-MM-dd)." });
            }

            var lockExists = await _context.BillingLocks.AnyAsync(b =>
                string.Compare(dto.PeriodStart, b.PeriodEnd) <= 0 &&
                string.Compare(dto.PeriodEnd, b.PeriodStart) >= 0);
            if (lockExists)
            {
                return BadRequest(new { message = "An overlapping billing lock already exists." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var record = new BillingLock
            {
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                LockedByUserId = userId,
                LockedAt = DateTime.UtcNow,
                Notes = dto.Notes
            };

            _context.BillingLocks.Add(record);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "billing.lock.create", "BillingLock", record.Id, $"Period {record.PeriodStart} to {record.PeriodEnd}");

            return Ok(record);
        }

        // GET: api/admin/billing-locks
        [HttpGet("billing-locks")]
        public async Task<IActionResult> GetBillingLocks()
        {
            var locks = await _context.BillingLocks
                .OrderByDescending(b => b.LockedAt)
                .ToListAsync();
            return Ok(locks);
        }

        // GET: api/admin/audit-logs
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] string? action = null,
            [FromQuery] string? entity = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? clientId = null,
            [FromQuery] string? from = null,
            [FromQuery] string? to = null)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 200);

            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);
            if (!string.IsNullOrWhiteSpace(entity))
                query = query.Where(a => a.Entity == entity);
            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(a => a.UserId == userId);
            if (!string.IsNullOrWhiteSpace(clientId))
                query = query.Where(a => a.ClientId == clientId);

            if (DateTime.TryParse(from, out var fromDate))
                query = query.Where(a => a.CreatedAt >= fromDate.ToUniversalTime());
            if (DateTime.TryParse(to, out var toDate))
                query = query.Where(a => a.CreatedAt <= toDate.ToUniversalTime());

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(a => new
                {
                    a.Id,
                    a.Action,
                    a.Entity,
                    a.EntityId,
                    a.UserId,
                    a.ClientId,
                    a.Role,
                    a.IpAddress,
                    a.UserAgent,
                    a.Details,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                logs,
                total,
                page,
                limit
            });
        }
    }

    // DTOs
    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? Password { get; set; }
    }

    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Password { get; set; }
    }

    public class UpdateClientDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Company { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public bool? PortalEnabled { get; set; }
    }

    public class CreateBillingLockDto
    {
        public string PeriodStart { get; set; } = string.Empty;
        public string PeriodEnd { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
