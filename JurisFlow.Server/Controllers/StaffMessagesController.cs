using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using System.Text.Json;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffMessagesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public StaffMessagesController(JurisFlowDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffMessage>>> GetMessages([FromQuery] string? userId)
        {
            var query = _context.StaffMessages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(m => m.SenderId == userId || m.RecipientId == userId);
            }

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(200)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("thread")]
        public async Task<ActionResult<IEnumerable<StaffMessage>>> GetThread([FromQuery] string userA, [FromQuery] string userB)
        {
            if (string.IsNullOrWhiteSpace(userA) || string.IsNullOrWhiteSpace(userB))
            {
                return BadRequest("Both userA and userB are required.");
            }

            var thread = await _context.StaffMessages
                .Where(m => (m.SenderId == userA && m.RecipientId == userB) || (m.SenderId == userB && m.RecipientId == userA))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(thread);
        }

        [HttpPost]
        public async Task<ActionResult<StaffMessage>> SendMessage([FromBody] StaffMessageCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var attachments = await SaveAttachments(dto.Attachments);

            var message = new StaffMessage
            {
                SenderId = dto.SenderId,
                RecipientId = dto.RecipientId,
                Body = dto.Body.Trim(),
                Status = "Unread",
                CreatedAt = DateTime.UtcNow,
                AttachmentsJson = attachments.Count > 0 ? JsonSerializer.Serialize(attachments) : null
            };

            _context.StaffMessages.Add(message);
            await _context.SaveChangesAsync();

            // Create notification for recipient if user exists
            var recipient = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == dto.RecipientId);
            if (recipient?.User != null)
            {
            _context.Notifications.Add(new Notification
            {
                UserId = recipient.UserId,
                Title = "New direct message",
                Message = $"You have a message from {dto.SenderId}",
                Type = "info",
                Link = "tab:communications"
            });
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetMessages), new { id = message.Id }, message);
    }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            var message = await _context.StaffMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.Status = "Read";
            message.ReadAt = DateTime.UtcNow;
            _context.StaffMessages.Update(message);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class StaffMessageCreateDto
        {
            [System.ComponentModel.DataAnnotations.Required]
            public string SenderId { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required]
            public string RecipientId { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required]
            public string Body { get; set; } = string.Empty;

            public List<AttachmentDto>? Attachments { get; set; }
        }

        private async Task<List<MessageAttachment>> SaveAttachments(List<AttachmentDto>? attachments)
        {
            var result = new List<MessageAttachment>();
            if (attachments == null || attachments.Count == 0) return result;

            var root = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "message-attachments");
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            foreach (var att in attachments)
            {
                if (string.IsNullOrWhiteSpace(att.Data)) continue;
                var parts = att.Data.Split(',');
                if (parts.Length != 2) continue;
                var header = parts[0];
                var base64 = parts[1];
                var mime = "application/octet-stream";
                var mimeSplit = header.Split(';').FirstOrDefault()?.Replace("data:", "");
                if (!string.IsNullOrWhiteSpace(mimeSplit)) mime = mimeSplit;

                var bytes = Convert.FromBase64String(base64);
                var ext = MimeToExt(mime, att.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(root, fileName);
                await System.IO.File.WriteAllBytesAsync(savePath, bytes);

                result.Add(new MessageAttachment
                {
                    FileName = att.FileName ?? fileName,
                    FilePath = $"/uploads/message-attachments/{fileName}",
                    MimeType = mime,
                    Size = bytes.Length
                });
            }
            return result;
        }

        private static string MimeToExt(string mime, string? originalName)
        {
            if (!string.IsNullOrEmpty(originalName) && Path.HasExtension(originalName))
            {
                return Path.GetExtension(originalName);
            }
            return mime switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "application/pdf" => ".pdf",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => ".bin"
            };
        }
    }
}
