using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;

        public SmsController(JurisFlowDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/sms/send
        [HttpPost("send")]
        public async Task<ActionResult<SmsMessage>> SendSms([FromBody] SendSmsDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var message = new SmsMessage
            {
                FromNumber = _configuration["Twilio:FromNumber"] ?? "+15551234567",
                ToNumber = dto.ToNumber,
                Body = dto.Body,
                Direction = "Outbound",
                Status = "Queued",
                MatterId = dto.MatterId,
                ClientId = dto.ClientId,
                SentBy = userId,
                TemplateId = dto.TemplateId
            };

            _context.SmsMessages.Add(message);
            await _context.SaveChangesAsync();

            // TODO: Integrate with Twilio API
            // var twilioClient = new TwilioRestClient(...);
            // var twilioMessage = await MessageResource.CreateAsync(...);
            // message.ExternalId = twilioMessage.Sid;

            // For now, simulate send
            message.Status = "Sent";
            message.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        // GET: api/sms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SmsMessage>>> GetMessages(
            [FromQuery] string? clientId = null,
            [FromQuery] string? matterId = null,
            [FromQuery] int limit = 50)
        {
            var query = _context.SmsMessages.AsQueryable();

            if (!string.IsNullOrEmpty(clientId))
            {
                query = query.Where(m => m.ClientId == clientId);
            }

            if (!string.IsNullOrEmpty(matterId))
            {
                query = query.Where(m => m.MatterId == matterId);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(messages);
        }

        // GET: api/sms/conversation/{phoneNumber}
        [HttpGet("conversation/{phoneNumber}")]
        public async Task<ActionResult<IEnumerable<SmsMessage>>> GetConversation(string phoneNumber, [FromQuery] int limit = 50)
        {
            var messages = await _context.SmsMessages
                .Where(m => m.ToNumber == phoneNumber || m.FromNumber == phoneNumber)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(messages);
        }

        // ========== TEMPLATES ==========

        // GET: api/sms/templates
        [HttpGet("templates")]
        public async Task<ActionResult<IEnumerable<SmsTemplate>>> GetTemplates([FromQuery] string? category = null)
        {
            var query = _context.SmsTemplates.Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            var templates = await query.OrderBy(t => t.Name).ToListAsync();
            return Ok(templates);
        }

        // POST: api/sms/templates
        [HttpPost("templates")]
        public async Task<ActionResult<SmsTemplate>> CreateTemplate([FromBody] SmsTemplate template)
        {
            template.Id = Guid.NewGuid().ToString();
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;

            _context.SmsTemplates.Add(template);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplates), template);
        }

        // PUT: api/sms/templates/{id}
        [HttpPut("templates/{id}")]
        public async Task<IActionResult> UpdateTemplate(string id, [FromBody] SmsTemplate updatedTemplate)
        {
            var template = await _context.SmsTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            template.Name = updatedTemplate.Name;
            template.Body = updatedTemplate.Body;
            template.Category = updatedTemplate.Category;
            template.Variables = updatedTemplate.Variables;
            template.IsActive = updatedTemplate.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(template);
        }

        // DELETE: api/sms/templates/{id}
        [HttpDelete("templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(string id)
        {
            var template = await _context.SmsTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ========== REMINDERS ==========

        // GET: api/sms/reminders
        [HttpGet("reminders")]
        public async Task<ActionResult<IEnumerable<SmsReminder>>> GetReminders(
            [FromQuery] string? status = null,
            [FromQuery] int days = 7)
        {
            var query = _context.SmsReminders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var cutoffDate = DateTime.UtcNow.AddDays(days);
            query = query.Where(r => r.ScheduledFor <= cutoffDate);

            var reminders = await query
                .OrderBy(r => r.ScheduledFor)
                .ToListAsync();

            return Ok(reminders);
        }

        // POST: api/sms/reminders
        [HttpPost("reminders")]
        public async Task<ActionResult<SmsReminder>> CreateReminder([FromBody] CreateReminderDto dto)
        {
            var reminder = new SmsReminder
            {
                ReminderType = dto.ReminderType,
                EntityId = dto.EntityId,
                EntityType = dto.EntityType,
                ClientId = dto.ClientId,
                ToNumber = dto.ToNumber,
                Message = dto.Message,
                ScheduledFor = dto.ScheduledFor,
                Status = "Pending"
            };

            _context.SmsReminders.Add(reminder);
            await _context.SaveChangesAsync();

            return Ok(reminder);
        }

        // POST: api/sms/reminders/{id}/cancel
        [HttpPost("reminders/{id}/cancel")]
        public async Task<IActionResult> CancelReminder(string id)
        {
            var reminder = await _context.SmsReminders.FindAsync(id);
            if (reminder == null)
            {
                return NotFound();
            }

            reminder.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reminder cancelled" });
        }

        // POST: api/sms/reminders/process
        [HttpPost("reminders/process")]
        public async Task<IActionResult> ProcessPendingReminders()
        {
            var now = DateTime.UtcNow;
            var pendingReminders = await _context.SmsReminders
                .Where(r => r.Status == "Pending" && r.ScheduledFor <= now)
                .ToListAsync();

            int sentCount = 0;

            foreach (var reminder in pendingReminders)
            {
                try
                {
                    // Create SMS message
                    var message = new SmsMessage
                    {
                        FromNumber = _configuration["Twilio:FromNumber"] ?? "+15551234567",
                        ToNumber = reminder.ToNumber,
                        Body = reminder.Message,
                        Direction = "Outbound",
                        Status = "Sent",
                        ClientId = reminder.ClientId,
                        SentAt = DateTime.UtcNow
                    };

                    _context.SmsMessages.Add(message);

                    // TODO: Send via Twilio API

                    reminder.Status = "Sent";
                    reminder.SmsMessageId = message.Id;
                    sentCount++;
                }
                catch (Exception ex)
                {
                    reminder.Status = "Failed";
                    Console.WriteLine($"Failed to send reminder {reminder.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Processed {pendingReminders.Count} reminders, sent {sentCount}" });
        }

        // POST: api/sms/webhook (Twilio webhook)
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleTwilioWebhook()
        {
            // TODO: Implement Twilio webhook handling
            // Parse incoming message status updates
            // Handle inbound messages

            return Ok();
        }

        // POST: api/sms/templates/seed
        [HttpPost("templates/seed")]
        public async Task<IActionResult> SeedDefaultTemplates()
        {
            if (await _context.SmsTemplates.AnyAsync())
            {
                return BadRequest(new { message = "Templates already exist" });
            }

            var templates = new List<SmsTemplate>
            {
                new SmsTemplate
                {
                    Name = "Appointment Reminder - 24 Hours",
                    Body = "Reminder: You have an appointment with {{firm_name}} tomorrow at {{time}}. Reply C to confirm or R to reschedule.",
                    Category = "Appointment",
                    Variables = "firm_name,time"
                },
                new SmsTemplate
                {
                    Name = "Appointment Reminder - 1 Hour",
                    Body = "Your appointment with {{attorney_name}} is in 1 hour at {{location}}. Please arrive 10 minutes early.",
                    Category = "Appointment",
                    Variables = "attorney_name,location"
                },
                new SmsTemplate
                {
                    Name = "Payment Reminder",
                    Body = "Reminder: Invoice #{{invoice_number}} for ${{amount}} is due on {{due_date}}. Pay online: {{payment_link}}",
                    Category = "Reminder",
                    Variables = "invoice_number,amount,due_date,payment_link"
                },
                new SmsTemplate
                {
                    Name = "Document Signature Request",
                    Body = "{{client_name}}, please sign the {{document_name}} at your earliest convenience: {{signing_link}}",
                    Category = "Reminder",
                    Variables = "client_name,document_name,signing_link"
                },
                new SmsTemplate
                {
                    Name = "Case Update",
                    Body = "Update on your case: {{update_message}}. Questions? Call us at {{phone}}.",
                    Category = "Follow-up",
                    Variables = "update_message,phone"
                }
            };

            _context.SmsTemplates.AddRange(templates);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Seeded {templates.Count} SMS templates" });
        }
    }

    // DTOs
    public class SendSmsDto
    {
        public string ToNumber { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? MatterId { get; set; }
        public string? ClientId { get; set; }
        public string? TemplateId { get; set; }
    }

    public class CreateReminderDto
    {
        public string ReminderType { get; set; } = "Appointment";
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public string? ClientId { get; set; }
        public string ToNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ScheduledFor { get; set; }
    }
}
