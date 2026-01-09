using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.DTOs;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public ClientsController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        // GET: api/Clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients()
        {
            return await _context.Clients
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // GET: api/Clients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(string id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            return client;
        }

        // POST: api/Clients
        [HttpPost]
        public async Task<ActionResult<Client>> PostClient(ClientCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var client = new Client
            {
                Id = Guid.NewGuid().ToString(),
                ClientNumber = dto.ClientNumber,
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Mobile = dto.Mobile,
                Company = dto.Company,
                Type = dto.Type,
                Status = dto.Status,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Country = dto.Country,
                TaxId = dto.TaxId,
                IncorporationState = dto.IncorporationState,
                RegisteredAgent = dto.RegisteredAgent,
                AuthorizedRepresentatives = dto.AuthorizedRepresentatives,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Password handling for portal access - only if explicitly provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                client.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                client.PortalEnabled = true;
            }

            _context.Clients.Add(client);
            _context.ClientStatusHistories.Add(new ClientStatusHistory
            {
                ClientId = client.Id,
                PreviousStatus = "New",
                NewStatus = client.Status,
                ChangedByUserId = GetUserId(),
                ChangedByName = GetUserEmail(),
                CreatedAt = DateTime.UtcNow
            });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                if (ClientExists(client.Id)) return Conflict();
                else throw;
            }

            var result = CreatedAtAction("GetClient", new { id = client.Id }, client);
            await _auditLogger.LogAsync(HttpContext, "client.create", "Client", client.Id, $"Created client {client.Email}");
            return result;
        }

        // PUT: api/Clients/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(string id, Client client)
        {
            if (id != client.Id) return BadRequest();

            var existing = await _context.Clients.FindAsync(id);
            if (existing == null) return NotFound();

            var previousStatus = existing.Status;

            existing.ClientNumber = client.ClientNumber;
            existing.Name = client.Name;
            existing.Email = client.Email;
            existing.Phone = client.Phone;
            existing.Mobile = client.Mobile;
            existing.Company = client.Company;
            existing.Type = client.Type;
            existing.Status = client.Status;
            existing.Address = client.Address;
            existing.City = client.City;
            existing.State = client.State;
            existing.ZipCode = client.ZipCode;
            existing.Country = client.Country;
            existing.TaxId = client.TaxId;
            existing.IncorporationState = client.IncorporationState;
            existing.RegisteredAgent = client.RegisteredAgent;
            existing.AuthorizedRepresentatives = client.AuthorizedRepresentatives;
            existing.Notes = client.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            if (!string.Equals(previousStatus, existing.Status, StringComparison.OrdinalIgnoreCase))
            {
                _context.ClientStatusHistories.Add(new ClientStatusHistory
                {
                    ClientId = existing.Id,
                    PreviousStatus = previousStatus ?? "Unknown",
                    NewStatus = existing.Status ?? "Unknown",
                    ChangedByUserId = GetUserId(),
                    ChangedByName = GetUserEmail(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(id)) return NotFound();
                else throw;
            }

            await _auditLogger.LogAsync(HttpContext, "client.update", "Client", client.Id, $"Updated client {client.Email}");
            return NoContent();
        }

        [HttpGet("{id}/status-history")]
        public async Task<IActionResult> GetStatusHistory(string id)
        {
            var exists = await _context.Clients.AnyAsync(c => c.Id == id);
            if (!exists) return NotFound();

            var history = await _context.ClientStatusHistories
                .Where(h => h.ClientId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.ClientId,
                    h.PreviousStatus,
                    h.NewStatus,
                    h.Notes,
                    h.ChangedByUserId,
                    h.ChangedByName,
                    h.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // DELETE: api/Clients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "client.delete", "Client", id, $"Deleted client {client.Email}");
            return NoContent();
        }

        private bool ClientExists(string id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        }

        private string? GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
        }

        // POST: api/Clients/{id}/set-password
        public class SetPasswordDto
        {
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("{id}/set-password")]
        public async Task<IActionResult> SetClientPassword(string id, [FromBody] SetPasswordDto dto)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            if (string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new { message = "Password is required" });
            }

            client.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            client.PortalEnabled = true;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password set successfully", portalEnabled = true });
        }
    }
}
