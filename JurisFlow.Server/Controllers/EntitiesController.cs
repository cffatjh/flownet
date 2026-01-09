using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/entities")]
    [ApiController]
    [Authorize]
    public class EntitiesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public EntitiesController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        [HttpGet]
        public async Task<IActionResult> GetEntities()
        {
            var entities = await _context.FirmEntities
                .OrderByDescending(e => e.IsDefault)
                .ThenBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.LegalName,
                    e.TaxId,
                    e.Email,
                    e.Phone,
                    e.Website,
                    e.Address,
                    e.City,
                    e.State,
                    e.ZipCode,
                    e.Country,
                    e.IsDefault,
                    e.IsActive,
                    e.CreatedAt,
                    e.UpdatedAt,
                    officeCount = e.Offices.Count
                })
                .ToListAsync();

            return Ok(entities);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEntity([FromBody] EntityDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Entity name is required." });
            }

            var hasEntities = await _context.FirmEntities.AnyAsync();
            var entity = new FirmEntity
            {
                Name = dto.Name.Trim(),
                LegalName = dto.LegalName,
                TaxId = dto.TaxId,
                Email = dto.Email,
                Phone = dto.Phone,
                Website = dto.Website,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Country = dto.Country,
                IsActive = dto.IsActive ?? true,
                IsDefault = dto.IsDefault ?? !hasEntities,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (entity.IsDefault)
            {
                var defaults = await _context.FirmEntities.Where(e => e.IsDefault).ToListAsync();
                foreach (var existing in defaults)
                {
                    existing.IsDefault = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.FirmEntities.Add(entity);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "entity.create", "FirmEntity", entity.Id, $"Created entity {entity.Name}");

            return Ok(entity);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEntity(string id, [FromBody] EntityDto dto)
        {
            var entity = await _context.FirmEntities.FindAsync(id);
            if (entity == null) return NotFound();

            if (dto.Name != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Entity name is required." });
                }
                entity.Name = dto.Name.Trim();
            }
            if (dto.LegalName != null) entity.LegalName = dto.LegalName;
            if (dto.TaxId != null) entity.TaxId = dto.TaxId;
            if (dto.Email != null) entity.Email = dto.Email;
            if (dto.Phone != null) entity.Phone = dto.Phone;
            if (dto.Website != null) entity.Website = dto.Website;
            if (dto.Address != null) entity.Address = dto.Address;
            if (dto.City != null) entity.City = dto.City;
            if (dto.State != null) entity.State = dto.State;
            if (dto.ZipCode != null) entity.ZipCode = dto.ZipCode;
            if (dto.Country != null) entity.Country = dto.Country;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;

            if (dto.IsDefault.HasValue)
            {
                if (dto.IsDefault.Value)
                {
                    var defaults = await _context.FirmEntities.Where(e => e.IsDefault && e.Id != entity.Id).ToListAsync();
                    foreach (var existing in defaults)
                    {
                        existing.IsDefault = false;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    entity.IsDefault = true;
                }
                else if (entity.IsDefault)
                {
                    var hasOtherDefaults = await _context.FirmEntities.AnyAsync(e => e.Id != entity.Id && e.IsDefault);
                    if (hasOtherDefaults)
                    {
                        entity.IsDefault = false;
                    }
                }
            }

            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "entity.update", "FirmEntity", entity.Id, $"Updated entity {entity.Name}");

            return Ok(entity);
        }

        [HttpPost("{id}/default")]
        public async Task<IActionResult> SetDefaultEntity(string id)
        {
            var entity = await _context.FirmEntities.FindAsync(id);
            if (entity == null) return NotFound();

            var defaults = await _context.FirmEntities.Where(e => e.IsDefault && e.Id != id).ToListAsync();
            foreach (var existing in defaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            entity.IsDefault = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "entity.default", "FirmEntity", entity.Id, $"Set default entity {entity.Name}");

            return Ok(entity);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntity(string id)
        {
            var entity = await _context.FirmEntities.FindAsync(id);
            if (entity == null) return NotFound();

            if (entity.IsDefault)
            {
                var fallback = await _context.FirmEntities
                    .Where(e => e.Id != id)
                    .OrderByDescending(e => e.IsActive)
                    .ThenBy(e => e.Name)
                    .FirstOrDefaultAsync();
                if (fallback != null)
                {
                    fallback.IsDefault = true;
                    fallback.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.FirmEntities.Remove(entity);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "entity.delete", "FirmEntity", entity.Id, $"Deleted entity {entity.Name}");

            return NoContent();
        }

        [HttpGet("{id}/offices")]
        public async Task<IActionResult> GetOffices(string id)
        {
            var entityExists = await _context.FirmEntities.AnyAsync(e => e.Id == id);
            if (!entityExists) return NotFound();

            var offices = await _context.Offices
                .Where(o => o.EntityId == id)
                .OrderByDescending(o => o.IsDefault)
                .ThenBy(o => o.Name)
                .ToListAsync();

            return Ok(offices);
        }

        [HttpPost("{id}/offices")]
        public async Task<IActionResult> CreateOffice(string id, [FromBody] OfficeDto dto)
        {
            var entity = await _context.FirmEntities.FindAsync(id);
            if (entity == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Office name is required." });
            }

            var hasOffices = await _context.Offices.AnyAsync(o => o.EntityId == id);
            var office = new Office
            {
                EntityId = id,
                Name = dto.Name.Trim(),
                Code = dto.Code,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Country = dto.Country,
                TimeZone = dto.TimeZone,
                IsActive = dto.IsActive ?? true,
                IsDefault = dto.IsDefault ?? !hasOffices,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (office.IsDefault)
            {
                var defaults = await _context.Offices.Where(o => o.EntityId == id && o.IsDefault).ToListAsync();
                foreach (var existing in defaults)
                {
                    existing.IsDefault = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.Offices.Add(office);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "office.create", "Office", office.Id, $"Created office {office.Name} ({entity.Name})");

            return Ok(office);
        }

        [HttpPut("{id}/offices/{officeId}")]
        public async Task<IActionResult> UpdateOffice(string id, string officeId, [FromBody] OfficeDto dto)
        {
            var office = await _context.Offices.FirstOrDefaultAsync(o => o.Id == officeId && o.EntityId == id);
            if (office == null) return NotFound();

            if (dto.Name != null)
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Office name is required." });
                }
                office.Name = dto.Name.Trim();
            }
            if (dto.Code != null) office.Code = dto.Code;
            if (dto.Email != null) office.Email = dto.Email;
            if (dto.Phone != null) office.Phone = dto.Phone;
            if (dto.Address != null) office.Address = dto.Address;
            if (dto.City != null) office.City = dto.City;
            if (dto.State != null) office.State = dto.State;
            if (dto.ZipCode != null) office.ZipCode = dto.ZipCode;
            if (dto.Country != null) office.Country = dto.Country;
            if (dto.TimeZone != null) office.TimeZone = dto.TimeZone;
            if (dto.IsActive.HasValue) office.IsActive = dto.IsActive.Value;

            if (dto.IsDefault.HasValue)
            {
                if (dto.IsDefault.Value)
                {
                    var defaults = await _context.Offices.Where(o => o.EntityId == id && o.IsDefault && o.Id != office.Id).ToListAsync();
                    foreach (var existing in defaults)
                    {
                        existing.IsDefault = false;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    office.IsDefault = true;
                }
                else if (office.IsDefault)
                {
                    var hasOtherDefaults = await _context.Offices.AnyAsync(o => o.EntityId == id && o.Id != office.Id && o.IsDefault);
                    if (hasOtherDefaults)
                    {
                        office.IsDefault = false;
                    }
                }
            }

            office.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "office.update", "Office", office.Id, $"Updated office {office.Name}");

            return Ok(office);
        }

        [HttpPost("{id}/offices/{officeId}/default")]
        public async Task<IActionResult> SetDefaultOffice(string id, string officeId)
        {
            var office = await _context.Offices.FirstOrDefaultAsync(o => o.Id == officeId && o.EntityId == id);
            if (office == null) return NotFound();

            var defaults = await _context.Offices.Where(o => o.EntityId == id && o.IsDefault && o.Id != office.Id).ToListAsync();
            foreach (var existing in defaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            office.IsDefault = true;
            office.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "office.default", "Office", office.Id, $"Set default office {office.Name}");

            return Ok(office);
        }

        [HttpDelete("{id}/offices/{officeId}")]
        public async Task<IActionResult> DeleteOffice(string id, string officeId)
        {
            var office = await _context.Offices.FirstOrDefaultAsync(o => o.Id == officeId && o.EntityId == id);
            if (office == null) return NotFound();

            if (office.IsDefault)
            {
                var fallback = await _context.Offices
                    .Where(o => o.EntityId == id && o.Id != officeId)
                    .OrderByDescending(o => o.IsActive)
                    .ThenBy(o => o.Name)
                    .FirstOrDefaultAsync();
                if (fallback != null)
                {
                    fallback.IsDefault = true;
                    fallback.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.Offices.Remove(office);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "office.delete", "Office", office.Id, $"Deleted office {office.Name}");

            return NoContent();
        }
    }

    public class EntityDto
    {
        public string? Name { get; set; }
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsActive { get; set; }
    }

    public class OfficeDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? TimeZone { get; set; }
        public bool? IsDefault { get; set; }
        public bool? IsActive { get; set; }
    }
}
