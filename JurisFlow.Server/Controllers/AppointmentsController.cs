using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public AppointmentsController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments([FromQuery] string? status, [FromQuery] string? clientId, [FromQuery] string? matterId)
        {
            var query = _context.AppointmentRequests.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(a => a.ClientId == clientId);
            }
            if (!string.IsNullOrWhiteSpace(matterId))
            {
                query = query.Where(a => a.MatterId == matterId);
            }

            var items = await query
                .OrderByDescending(a => a.RequestedDate)
                .ToListAsync();

            var clientIds = items.Select(a => a.ClientId).Distinct().ToList();
            var clients = await _context.Clients
                .Where(c => clientIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.Email })
                .ToListAsync();
            var clientMap = clients.ToDictionary(c => c.Id, c => c);

            var response = items.Select(a => new
            {
                id = a.Id,
                clientId = a.ClientId,
                client = clientMap.TryGetValue(a.ClientId, out var c) ? c : null,
                matterId = a.MatterId,
                requestedDate = a.RequestedDate,
                duration = a.Duration,
                type = a.Type,
                notes = a.Notes,
                status = a.Status,
                assignedTo = a.AssignedTo,
                approvedDate = a.ApprovedDate,
                createdAt = a.CreatedAt,
                updatedAt = a.UpdatedAt
            });

            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(string id, [FromBody] AppointmentUpdateDto dto)
        {
            var appointment = await _context.AppointmentRequests.FindAsync(id);
            if (appointment == null) return NotFound();

            var previousStatus = appointment.Status;
            var previousApprovedDate = appointment.ApprovedDate;

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "pending",
                    "approved",
                    "rejected",
                    "cancelled"
                };
                if (!allowedStatuses.Contains(dto.Status))
                {
                    return BadRequest(new { message = "Invalid appointment status." });
                }
                appointment.Status = dto.Status.ToLowerInvariant();
            }
            if (dto.ApprovedDate.HasValue)
            {
                appointment.ApprovedDate = dto.ApprovedDate;
            }
            if (!string.IsNullOrWhiteSpace(dto.AssignedTo))
            {
                appointment.AssignedTo = dto.AssignedTo;
            }
            if (dto.Duration.HasValue)
            {
                if (dto.Duration.Value < 15 || dto.Duration.Value > 240)
                {
                    return BadRequest(new { message = "Duration must be between 15 and 240 minutes." });
                }
                appointment.Duration = dto.Duration.Value;
            }

            appointment.UpdatedAt = DateTime.UtcNow;

            if (appointment.Status == "approved" && !appointment.ApprovedDate.HasValue)
            {
                appointment.ApprovedDate = appointment.RequestedDate;
            }

            if (!string.Equals(previousStatus, appointment.Status, StringComparison.OrdinalIgnoreCase))
            {
                await CreateClientNotificationAsync(appointment, appointment.Status switch
                {
                    "approved" => "Appointment Approved",
                    "rejected" => "Appointment Rejected",
                    "cancelled" => "Appointment Cancelled",
                    _ => "Appointment Updated"
                }, appointment.Status switch
                {
                    "approved" => $"Your appointment request for {appointment.RequestedDate:g} has been approved.",
                    "rejected" => $"Your appointment request for {appointment.RequestedDate:g} has been rejected.",
                    "cancelled" => $"Your appointment request for {appointment.RequestedDate:g} has been cancelled.",
                    _ => $"Your appointment request for {appointment.RequestedDate:g} has been updated."
                }, appointment.Status == "approved" ? "success" : "info");
            }
            else if (appointment.Status == "approved" && appointment.ApprovedDate.HasValue && appointment.ApprovedDate != previousApprovedDate)
            {
                await CreateClientNotificationAsync(appointment, "Appointment Rescheduled",
                    $"Your appointment has been rescheduled to {appointment.ApprovedDate:MMM d, yyyy h:mm tt}.",
                    "info");
            }

            if (dto.NotifyClient == true)
            {
                var title = appointment.Status == "approved" ? "Appointment Reminder" : "Appointment Update";
                var message = appointment.Status == "approved" && appointment.ApprovedDate.HasValue
                    ? $"Reminder: your appointment is scheduled for {appointment.ApprovedDate:MMM d, yyyy h:mm tt}."
                    : $"Your appointment request status is {appointment.Status}.";
                await CreateClientNotificationAsync(appointment, title, message, "info");
            }

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "appointment.update", "AppointmentRequest", appointment.Id, $"Status={appointment.Status}");

            return Ok(appointment);
        }

        [HttpPost("{id}/notify")]
        public async Task<IActionResult> NotifyAppointment(string id)
        {
            var appointment = await _context.AppointmentRequests.FindAsync(id);
            if (appointment == null) return NotFound();

            var title = appointment.Status == "approved" ? "Appointment Reminder" : "Appointment Update";
            var message = appointment.Status == "approved" && appointment.ApprovedDate.HasValue
                ? $"Reminder: your appointment is scheduled for {appointment.ApprovedDate:MMM d, yyyy h:mm tt}."
                : $"Your appointment request status is {appointment.Status}.";

            await CreateClientNotificationAsync(appointment, title, message, "info");
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "appointment.notify", "AppointmentRequest", appointment.Id, "Client notified");

            return Ok(new { message = "Notification sent." });
        }

        private async System.Threading.Tasks.Task CreateClientNotificationAsync(AppointmentRequest appointment, string title, string message, string type)
        {
            var client = await _context.Clients.FindAsync(appointment.ClientId);
            if (client == null) return;

            _context.Notifications.Add(new Notification
            {
                ClientId = client.Id,
                Title = title,
                Message = message,
                Type = type,
                Link = "tab:appointments"
            });
        }
    }

    public class AppointmentUpdateDto
    {
        public string? Status { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? AssignedTo { get; set; }
        public bool? NotifyClient { get; set; }
        public int? Duration { get; set; }
    }
}
