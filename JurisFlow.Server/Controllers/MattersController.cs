using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MattersController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;
        private readonly FirmStructureService _firmStructure;

        public MattersController(JurisFlowDbContext context, AuditLogger auditLogger, FirmStructureService firmStructure)
        {
            _context = context;
            _auditLogger = auditLogger;
            _firmStructure = firmStructure;
        }

        // GET: api/Matters
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Matter>>> GetMatters([FromQuery] string? status, [FromQuery] string? entityId, [FromQuery] string? officeId)
        {
            var query = _context.Matters.Include(m => m.Client).AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                // Frontend might send "Archive" or "Open"
                // The Matter model has 'Status' field.
                // Assuming "Archived" is the status for archive.
                if (status.ToLower() == "archive" || status.ToLower() == "archived")
                {
                     query = query.Where(m => m.Status == "Archived");
                }
                else
                {
                     query = query.Where(m => m.Status == status || (status == "Open" && m.Status != "Archived" && m.Status != "Closed"));
                }
            }

            if (!string.IsNullOrWhiteSpace(entityId))
            {
                query = query.Where(m => m.EntityId == entityId);
            }

            if (!string.IsNullOrWhiteSpace(officeId))
            {
                query = query.Where(m => m.OfficeId == officeId);
            }

            return await query.OrderByDescending(m => m.OpenDate).ToListAsync();
        }

        // GET: api/Matters/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Matter>> GetMatter(string id)
        {
            var matter = await _context.Matters.Include(m => m.Client).FirstOrDefaultAsync(m => m.Id == id);

            if (matter == null)
            {
                return NotFound();
            }

            return matter;
        }

        // POST: api/Matters
        [HttpPost]
        public async Task<ActionResult<Matter>> PostMatter(Matter matter)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            matter.Id = Guid.NewGuid().ToString();
            matter.OpenDate = DateTime.UtcNow;

            var resolved = await _firmStructure.ResolveEntityOfficeAsync(matter.EntityId, matter.OfficeId);
            matter.EntityId = resolved.entityId;
            matter.OfficeId = resolved.officeId;
            
            _context.Matters.Add(matter);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "matter.create", "Matter", matter.Id, $"ClientId={matter.ClientId}, Name={matter.Name}");

            return CreatedAtAction("GetMatter", new { id = matter.Id }, matter);
        }

        // PUT: api/Matters/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMatter(string id, Matter matter)
        {
             if (id != matter.Id) return BadRequest();

            _context.Entry(matter).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MatterExists(id)) return NotFound();
                else throw;
            }

            await _auditLogger.LogAsync(HttpContext, "matter.update", "Matter", matter.Id, $"Status={matter.Status}");

            return NoContent();
        }

        // DELETE: api/Matters/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMatter(string id)
        {
            var matter = await _context.Matters.FindAsync(id);
            if (matter == null) return NotFound();

            _context.Matters.Remove(matter);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "matter.delete", "Matter", id, $"Deleted matter {matter.Name}");

            return NoContent();
        }
        
        // POST: api/Matters/5/archive
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchiveMatter(string id)
        {
            var matter = await _context.Matters.FindAsync(id);
            if (matter == null) return NotFound();

            matter.Status = "Archived";
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "matter.archive", "Matter", id, "Archived matter");

            return Ok(matter);
        }

        // POST: api/Matters/5/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreMatter(string id)
        {
            var matter = await _context.Matters.FindAsync(id);
            if (matter == null) return NotFound();

            matter.Status = "Open"; // Restore to Open
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "matter.restore", "Matter", id, "Restored matter");

            return Ok(matter);
        }

        private bool MatterExists(string id)
        {
            return _context.Matters.Any(e => e.Id == id);
        }
    }
}
