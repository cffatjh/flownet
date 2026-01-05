using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HolidaysController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public HolidaysController(JurisFlowDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetHolidays([FromQuery] string? jurisdiction = null)
        {
            var query = _context.Holidays.AsQueryable();
            if (!string.IsNullOrEmpty(jurisdiction))
            {
                query = query.Where(h => h.Jurisdiction == jurisdiction);
            }

            var items = await query.OrderBy(h => h.Date).ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHoliday([FromBody] Holiday dto)
        {
            dto.Id = Guid.NewGuid().ToString();
            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;

            _context.Holidays.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoliday(string id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
