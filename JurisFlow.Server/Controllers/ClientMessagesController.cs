using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/client/messages")]
    [ApiController]
    [Authorize]
    public class ClientMessagesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogger _auditLogger;

        public ClientMessagesController(JurisFlowDbContext context, IWebHostEnvironment env, AuditLogger auditLogger)
        {
            _context = context;
            _env = env;
            _auditLogger = auditLogger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetForClient([FromQuery] string? clientId)
        {
            if (!IsClient()) return Forbid();
            var resolvedClientId = GetClientId();
            if (string.IsNullOrWhiteSpace(resolvedClientId)) return Unauthorized();
            if (!string.IsNullOrWhiteSpace(clientId) && !string.Equals(clientId, resolvedClientId, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var items = await _context.ClientMessages
                .Where(m => m.ClientId == resolvedClientId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(100)
                .ToListAsync();

            var matterIds = items.Where(m => !string.IsNullOrEmpty(m.MatterId)).Select(m => m.MatterId!).Distinct().ToList();
            var matters = await _context.Matters
                .Where(m => matterIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Name, m.CaseNumber })
                .ToListAsync();
            var matterMap = matters.ToDictionary(m => m.Id, m => m);

            var response = items.Select(m => new
            {
                id = m.Id,
                subject = m.Subject,
                message = m.Body,
                read = string.Equals(m.Status, "Read", StringComparison.OrdinalIgnoreCase),
                createdAt = m.CreatedAt,
                matterId = m.MatterId,
                matter = m.MatterId != null && matterMap.TryGetValue(m.MatterId, out var matter) ? matter : null,
                attachmentsJson = m.AttachmentsJson
            });

            return Ok(response);
        }

        [HttpGet("~/api/messages/client")]
        public async Task<ActionResult<IEnumerable<object>>> GetForStaff([FromQuery] string? clientId)
        {
            if (IsClient()) return Forbid();

            var query = _context.ClientMessages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                query = query.Where(m => m.ClientId == clientId);
            }

            var items = await query.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();

            var matterIds = items.Where(m => !string.IsNullOrEmpty(m.MatterId)).Select(m => m.MatterId!).Distinct().ToList();
            var matters = await _context.Matters
                .Where(m => matterIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Name, m.CaseNumber })
                .ToListAsync();
            var matterMap = matters.ToDictionary(m => m.Id, m => m);

            var response = items.Select(m => new
            {
                id = m.Id,
                clientId = m.ClientId,
                subject = m.Subject,
                message = m.Body,
                read = string.Equals(m.Status, "Read", StringComparison.OrdinalIgnoreCase),
                createdAt = m.CreatedAt,
                matterId = m.MatterId,
                matter = m.MatterId != null && matterMap.TryGetValue(m.MatterId, out var matter) ? matter : null,
                attachmentsJson = m.AttachmentsJson
            });

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<object>> Send(ClientMessageCreateDto dto)
        {
            if (!IsClient()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Unauthorized();

            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return BadRequest("Client not found");

            if (!string.IsNullOrWhiteSpace(dto.MatterId))
            {
                var ownsMatter = await _context.Matters.AnyAsync(m => m.Id == dto.MatterId && m.ClientId == clientId);
                if (!ownsMatter) return Forbid();
            }

            var attachments = await SaveAttachments(dto.Attachments);
            var msg = new ClientMessage
            {
                ClientId = client.Id,
                EmployeeId = dto.EmployeeId,
                MatterId = dto.MatterId,
                Subject = dto.Subject,
                Body = dto.Message,
                Status = "Unread",
                CreatedAt = DateTime.UtcNow,
                AttachmentsJson = attachments.Count > 0 ? JsonSerializer.Serialize(attachments) : null
            };

            _context.ClientMessages.Add(msg);

            if (!string.IsNullOrEmpty(dto.EmployeeId))
            {
                var emp = await _context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);
                if (emp?.UserId != null)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = emp.UserId,
                        Title = "New client message",
                        Message = $"{client.Name} sent you a message.",
                        Type = "info",
                        Link = "tab:communications"
                    });
                }
            }

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "client.message.send", "ClientMessage", msg.Id, $"Client={client.Email}");

            return Ok(new
            {
                id = msg.Id,
                subject = msg.Subject,
                message = msg.Body,
                read = false,
                createdAt = msg.CreatedAt,
                matterId = msg.MatterId,
                attachmentsJson = msg.AttachmentsJson
            });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            var msg = await _context.ClientMessages.FindAsync(id);
            if (msg == null) return NotFound();
            if (IsClient())
            {
                var clientId = GetClientId();
                if (string.IsNullOrWhiteSpace(clientId) || msg.ClientId != clientId) return Forbid();
            }

            msg.Status = "Read";
            msg.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "client.message.read", "ClientMessage", id, null);
            return NoContent();
        }

        private async Task<List<MessageAttachment>> SaveAttachments(List<AttachmentDto>? attachments)
        {
            var result = new List<MessageAttachment>();
            if (attachments == null || attachments.Count == 0) return result;

            var root = Path.Combine(_env.ContentRootPath, "uploads", "message-attachments");
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
                var ext = MimeToExt(mime, att.FileName ?? att.Name);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(root, fileName);
                await System.IO.File.WriteAllBytesAsync(savePath, bytes);

                result.Add(new MessageAttachment
                {
                    FileName = att.FileName ?? att.Name ?? fileName,
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

        private bool IsClient()
        {
            return User.IsInRole("Client") || string.Equals(User.FindFirst("role")?.Value, "Client", StringComparison.OrdinalIgnoreCase);
        }

        private string? GetClientId()
        {
            return User.FindFirst("clientId")?.Value
                   ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

    public class ClientMessageCreateDto
    {
        public string? ClientId { get; set; }
        public string? EmployeeId { get; set; }
        public string? MatterId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Subject { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string Message { get; set; } = string.Empty;

        public List<AttachmentDto>? Attachments { get; set; }
    }

    public class AttachmentDto
    {
        public string? FileName { get; set; }
        public string? Name { get; set; }
        public long Size { get; set; }
        public string? Type { get; set; }
        public string Data { get; set; } = string.Empty; // data URL base64
    }
}
