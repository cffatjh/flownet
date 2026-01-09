using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/time-entries")]
    [ApiController]
    [Authorize]
    public class TimeEntriesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public TimeEntriesController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeEntries([FromQuery] string? matterId = null, [FromQuery] string? approvalStatus = null)
        {
            var query = _context.TimeEntries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(matterId))
            {
                query = query.Where(t => t.MatterId == matterId);
            }

            if (!string.IsNullOrWhiteSpace(approvalStatus))
            {
                query = query.Where(t => t.ApprovalStatus == approvalStatus);
            }

            var items = await query.OrderByDescending(t => t.Date).ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTimeEntry([FromBody] TimeEntryCreateDto dto)
        {
            if (dto.Duration <= 0)
            {
                return BadRequest(new { message = "Duration must be greater than zero." });
            }

            var userId = GetUserId();
            var isApprover = IsApprover();

            var entry = new TimeEntry
            {
                MatterId = dto.MatterId,
                Description = dto.Description ?? string.Empty,
                Duration = dto.Duration,
                Rate = dto.Rate,
                Date = dto.Date ?? DateTime.UtcNow,
                Billed = dto.Billed,
                IsBillable = dto.IsBillable,
                Type = "time",
                ActivityCode = NormalizeUtbmsCode(dto.ActivityCode),
                TaskCode = NormalizeUtbmsCode(dto.TaskCode),
                ApprovalStatus = isApprover ? "Approved" : "Pending",
                SubmittedBy = userId,
                SubmittedAt = DateTime.UtcNow,
                ApprovedBy = isApprover ? userId : null,
                ApprovedAt = isApprover ? DateTime.UtcNow : null,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TimeEntries.Add(entry);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "time.create", "TimeEntry", entry.Id, $"MatterId={entry.MatterId}, Duration={entry.Duration}");

            return Ok(entry);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTimeEntry(string id, [FromBody] TimeEntryUpdateDto dto)
        {
            var entry = await _context.TimeEntries.FindAsync(id);
            if (entry == null) return NotFound();

            if (entry.ApprovalStatus == "Approved")
            {
                return BadRequest(new { message = "Approved time entries cannot be edited." });
            }

            if (dto.Description != null) entry.Description = dto.Description;
            if (dto.Duration.HasValue) entry.Duration = dto.Duration.Value;
            if (dto.Rate.HasValue) entry.Rate = dto.Rate.Value;
            if (dto.Date.HasValue) entry.Date = dto.Date.Value;
            if (dto.IsBillable.HasValue) entry.IsBillable = dto.IsBillable.Value;
            if (dto.ActivityCode != null) entry.ActivityCode = NormalizeUtbmsCode(dto.ActivityCode);
            if (dto.TaskCode != null) entry.TaskCode = NormalizeUtbmsCode(dto.TaskCode);
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "time.update", "TimeEntry", entry.Id, "Time entry updated");

            return Ok(entry);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveTimeEntry(string id)
        {
            if (!IsApprover()) return Forbid();

            var entry = await _context.TimeEntries.FindAsync(id);
            if (entry == null) return NotFound();

            entry.ApprovalStatus = "Approved";
            entry.ApprovedBy = GetUserId();
            entry.ApprovedAt = DateTime.UtcNow;
            entry.RejectedBy = null;
            entry.RejectedAt = null;
            entry.RejectionReason = null;
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "time.approve", "TimeEntry", entry.Id, "Time entry approved");

            return Ok(entry);
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectTimeEntry(string id, [FromBody] ApprovalRejectDto dto)
        {
            if (!IsApprover()) return Forbid();

            var entry = await _context.TimeEntries.FindAsync(id);
            if (entry == null) return NotFound();

            entry.ApprovalStatus = "Rejected";
            entry.RejectedBy = GetUserId();
            entry.RejectedAt = DateTime.UtcNow;
            entry.RejectionReason = dto.Reason;
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "time.reject", "TimeEntry", entry.Id, $"Reason={dto.Reason}");

            return Ok(entry);
        }

        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        }

        private bool IsApprover()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? string.Empty;
            return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Partner", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Associate", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Accountant", StringComparison.OrdinalIgnoreCase);
        }

        private string? NormalizeUtbmsCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var trimmed = code.Trim();
            var split = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return split.Length > 0 ? split[0].Trim() : trimmed;
        }
    }

    public class TimeEntryCreateDto
    {
        public string? MatterId { get; set; }
        public string? Description { get; set; }
        public int Duration { get; set; }
        public double Rate { get; set; }
        public DateTime? Date { get; set; }
        public bool Billed { get; set; }
        public bool IsBillable { get; set; } = true;
        public string? ActivityCode { get; set; }
        public string? TaskCode { get; set; }
    }

    public class TimeEntryUpdateDto
    {
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public double? Rate { get; set; }
        public DateTime? Date { get; set; }
        public bool? IsBillable { get; set; }
        public string? ActivityCode { get; set; }
        public string? TaskCode { get; set; }
    }

    public class ApprovalRejectDto
    {
        public string? Reason { get; set; }
    }
}
