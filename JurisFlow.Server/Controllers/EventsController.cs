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
    public class EventsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public EventsController(JurisFlowDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CalendarEvent>>> GetEvents()
        {
            return await _context.CalendarEvents
                .Include(e => e.Matter)
                .OrderBy(e => e.Date)
                .ToListAsync();
        }

        // GET: api/Events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CalendarEvent>> GetEvent(string id)
        {
            var calendarEvent = await _context.CalendarEvents
                .Include(e => e.Matter)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (calendarEvent == null)
            {
                return NotFound();
            }

            return calendarEvent;
        }

        // POST: api/Events
        [HttpPost]
        public async Task<ActionResult<CalendarEvent>> CreateEvent(CalendarEventDto dto)
        {
            var calendarEvent = new CalendarEvent
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title,
                Date = dto.Date,
                Type = dto.Type ?? "Meeting",
                Description = dto.Description,
                Location = dto.Location,
                RecurrencePattern = dto.RecurrencePattern,
                Duration = dto.Duration ?? 60,
                ReminderMinutes = dto.ReminderMinutes ?? 30,
                ReminderSent = false,
                MatterId = dto.MatterId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CalendarEvents.Add(calendarEvent);
            await _context.SaveChangesAsync();

            TryCreateUpcomingNotification(calendarEvent);

            return CreatedAtAction(nameof(GetEvent), new { id = calendarEvent.Id }, calendarEvent);
        }

        // PUT: api/Events/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(string id, CalendarEventDto dto)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null)
            {
                return NotFound();
            }

            calendarEvent.Title = dto.Title;
            calendarEvent.Date = dto.Date;
            calendarEvent.Type = dto.Type ?? calendarEvent.Type;
            calendarEvent.Description = dto.Description;
            calendarEvent.Location = dto.Location;
            calendarEvent.RecurrencePattern = dto.RecurrencePattern;
            calendarEvent.Duration = dto.Duration ?? calendarEvent.Duration;
            calendarEvent.ReminderMinutes = dto.ReminderMinutes ?? calendarEvent.ReminderMinutes;
            calendarEvent.MatterId = dto.MatterId;
            calendarEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TryCreateUpcomingNotification(calendarEvent);

            return Ok(calendarEvent);
        }

        // DELETE: api/Events/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(string id)
        {
            var calendarEvent = await _context.CalendarEvents.FindAsync(id);
            if (calendarEvent == null)
            {
                return NotFound();
            }

            _context.CalendarEvents.Remove(calendarEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private void TryCreateUpcomingNotification(CalendarEvent evt)
        {
            // basic check: if event is within 24h, create info notification for all users
            var now = DateTime.UtcNow;
            if (evt.Date <= now) return;
            if ((evt.Date - now).TotalHours <= 24)
            {
                var users = _context.Users.Take(50).ToList();
                foreach (var u in users)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = u.Id,
                        Title = "Upcoming event",
                        Message = $"{evt.Title} - {evt.Date.ToLocalTime():g}",
                        Type = "warning",
                        Link = "tab:calendar"
                    });
                }
            }
        }
    }

    // DTO
    public class CalendarEventDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string? RecurrencePattern { get; set; }
        public int? Duration { get; set; }
        public int? ReminderMinutes { get; set; }
        public string? MatterId { get; set; }
    }
}

