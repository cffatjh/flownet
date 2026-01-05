using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeadlinesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public DeadlinesController(JurisFlowDbContext context)
        {
            _context = context;
        }

        // GET: api/deadlines
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Deadline>>> GetDeadlines(
            [FromQuery] string? matterId = null,
            [FromQuery] string? status = null,
            [FromQuery] int days = 30)
        {
            var query = _context.Deadlines.AsQueryable();

            if (!string.IsNullOrEmpty(matterId))
            {
                query = query.Where(d => d.MatterId == matterId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            // Default: get deadlines within next N days
            var cutoffDate = DateTime.UtcNow.AddDays(days);
            query = query.Where(d => d.DueDate <= cutoffDate);

            var deadlines = await query
                .OrderBy(d => d.DueDate)
                .ToListAsync();

            return Ok(deadlines);
        }

        // GET: api/deadlines/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Deadline>> GetDeadline(string id)
        {
            var deadline = await _context.Deadlines.FindAsync(id);
            if (deadline == null)
            {
                return NotFound();
            }

            return Ok(deadline);
        }

        // POST: api/deadlines
        [HttpPost]
        public async Task<ActionResult<Deadline>> CreateDeadline([FromBody] CreateDeadlineDto dto)
        {
            var deadline = new Deadline
            {
                MatterId = dto.MatterId,
                CourtRuleId = dto.CourtRuleId,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                TriggerDate = dto.TriggerDate,
                Priority = dto.Priority ?? "Medium",
                DeadlineType = dto.DeadlineType ?? "Filing",
                ReminderDays = dto.ReminderDays ?? 3,
                AssignedTo = dto.AssignedTo,
                Notes = dto.Notes
            };

            _context.Deadlines.Add(deadline);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDeadline), new { id = deadline.Id }, deadline);
        }

        // POST: api/deadlines/calculate
        [HttpPost("calculate")]
        public async Task<ActionResult<CalculatedDeadlineDto>> CalculateDeadline([FromBody] CalculateDeadlineDto dto)
        {
            var rule = await _context.CourtRules.FindAsync(dto.CourtRuleId);
            if (rule == null)
            {
                return NotFound(new { message = "Court rule not found" });
            }

            var triggerDate = dto.TriggerDate ?? DateTime.UtcNow.Date;
            var holidays = await LoadHolidays(rule.Jurisdiction);
            var calculatedDate = CalculateDeadlineDate(triggerDate, rule, dto.ServiceMethod, holidays);

            return Ok(new CalculatedDeadlineDto
            {
                TriggerDate = triggerDate,
                DueDate = calculatedDate,
                RuleName = rule.Name,
                RuleCitation = rule.Citation,
                DaysCount = rule.DaysCount,
                ServiceDaysAdded = rule.ServiceDaysAdd,
                Description = $"{rule.DaysCount} {rule.DayType.ToLower()} days {rule.Direction.ToLower()} {rule.TriggerEvent}"
            });
        }

        // PUT: api/deadlines/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeadline(string id, [FromBody] UpdateDeadlineDto dto)
        {
            var deadline = await _context.Deadlines.FindAsync(id);
            if (deadline == null)
            {
                return NotFound();
            }

            if (dto.Title != null) deadline.Title = dto.Title;
            if (dto.Description != null) deadline.Description = dto.Description;
            if (dto.DueDate.HasValue) deadline.DueDate = dto.DueDate.Value;
            if (dto.Status != null) deadline.Status = dto.Status;
            if (dto.Priority != null) deadline.Priority = dto.Priority;
            if (dto.AssignedTo != null) deadline.AssignedTo = dto.AssignedTo;
            if (dto.Notes != null) deadline.Notes = dto.Notes;
            if (dto.ReminderDays.HasValue) deadline.ReminderDays = dto.ReminderDays.Value;

            deadline.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(deadline);
        }

        // POST: api/deadlines/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteDeadline(string id)
        {
            var deadline = await _context.Deadlines.FindAsync(id);
            if (deadline == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            deadline.Status = "Completed";
            deadline.CompletedAt = DateTime.UtcNow;
            deadline.CompletedBy = userId;
            deadline.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Deadline completed", completedAt = deadline.CompletedAt });
        }

        // DELETE: api/deadlines/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeadline(string id)
        {
            var deadline = await _context.Deadlines.FindAsync(id);
            if (deadline == null)
            {
                return NotFound();
            }

            _context.Deadlines.Remove(deadline);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/deadlines/upcoming
        [HttpGet("upcoming")]
        public async Task<ActionResult<UpcomingDeadlinesDto>> GetUpcomingDeadlines([FromQuery] int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(days);
            var today = DateTime.UtcNow.Date;

            var deadlines = await _context.Deadlines
                .Where(d => d.Status == "Pending" && d.DueDate <= cutoffDate)
                .OrderBy(d => d.DueDate)
                .ToListAsync();

            var overdue = deadlines.Where(d => d.DueDate.Date < today).ToList();
            var dueToday = deadlines.Where(d => d.DueDate.Date == today).ToList();
            var upcoming = deadlines.Where(d => d.DueDate.Date > today).ToList();

            return Ok(new UpcomingDeadlinesDto
            {
                Overdue = overdue,
                DueToday = dueToday,
                Upcoming = upcoming,
                TotalCount = deadlines.Count
            });
        }

        // Helper method for deadline calculation
        private async Task<List<DateTime>> LoadHolidays(string? jurisdiction)
        {
            var query = _context.Holidays.AsQueryable();
            query = query.Where(h => h.IsCourtHoliday);
            if (!string.IsNullOrEmpty(jurisdiction))
            {
                query = query.Where(h => h.Jurisdiction == jurisdiction);
            }
            return await query.Select(h => h.Date.Date).ToListAsync();
        }

        private DateTime CalculateDeadlineDate(DateTime triggerDate, CourtRule rule, string? serviceMethod, List<DateTime> holidays)
        {
            var days = rule.DaysCount;

            // Add service days if applicable
            if (!string.IsNullOrEmpty(serviceMethod) && serviceMethod != "Personal")
            {
                days += rule.ServiceDaysAdd;
            }

            DateTime result;

            if (rule.Direction == "Before")
            {
                result = triggerDate.AddDays(-days);
            }
            else
            {
                result = triggerDate.AddDays(days);
            }

            // If court days, skip weekends
            if (rule.DayType == "Court")
            {
                result = AdjustForCourtDays(triggerDate, days, rule.Direction == "Before", holidays);
            }

            // Extend if falls on weekend/holiday
            if (rule.ExtendIfWeekend)
            {
                result = AdjustForWeekendOrHoliday(result, holidays);
            }

            return result;
        }

        private DateTime AdjustForCourtDays(DateTime start, int courtDays, bool backwards, List<DateTime> holidays)
        {
            var current = start;
            var direction = backwards ? -1 : 1;
            var daysRemaining = courtDays;

            while (daysRemaining > 0)
            {
                current = current.AddDays(direction);
                if (IsBusinessDay(current, holidays))
                {
                    daysRemaining--;
                }
            }

            return current;
        }

        private bool IsBusinessDay(DateTime date, List<DateTime> holidays)
        {
            var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            var isHoliday = holidays.Any(h => h.Date == date.Date);
            return !isWeekend && !isHoliday;
        }

        private DateTime AdjustForWeekendOrHoliday(DateTime date, List<DateTime> holidays)
        {
            while (!IsBusinessDay(date, holidays))
            {
                date = date.AddDays(1);
            }
            return date;
        }
    }

    // DTOs
    public class CreateDeadlineDto
    {
        public string MatterId { get; set; } = string.Empty;
        public string? CourtRuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? TriggerDate { get; set; }
        public string? Priority { get; set; }
        public string? DeadlineType { get; set; }
        public int? ReminderDays { get; set; }
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateDeadlineDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
        public int? ReminderDays { get; set; }
    }

    public class CalculateDeadlineDto
    {
        public string CourtRuleId { get; set; } = string.Empty;
        public DateTime? TriggerDate { get; set; }
        public string? ServiceMethod { get; set; } // Personal, Mail, Electronic
    }

    public class CalculatedDeadlineDto
    {
        public DateTime TriggerDate { get; set; }
        public DateTime DueDate { get; set; }
        public string RuleName { get; set; } = "";
        public string? RuleCitation { get; set; }
        public int DaysCount { get; set; }
        public int ServiceDaysAdded { get; set; }
        public string Description { get; set; } = "";
    }

    public class UpcomingDeadlinesDto
    {
        public List<Deadline> Overdue { get; set; } = new();
        public List<Deadline> DueToday { get; set; } = new();
        public List<Deadline> Upcoming { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
