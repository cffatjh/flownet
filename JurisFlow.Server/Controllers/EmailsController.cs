using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;

        public EmailsController(JurisFlowDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/emails
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmailMessage>>> GetEmails(
            [FromQuery] string? matterId = null,
            [FromQuery] string? clientId = null,
            [FromQuery] string? folder = null,
            [FromQuery] int limit = 50)
        {
            var query = _context.EmailMessages.AsQueryable();

            if (!string.IsNullOrEmpty(matterId))
            {
                query = query.Where(e => e.MatterId == matterId);
            }

            if (!string.IsNullOrEmpty(clientId))
            {
                query = query.Where(e => e.ClientId == clientId);
            }

            if (!string.IsNullOrEmpty(folder))
            {
                query = query.Where(e => e.Folder == folder);
            }

            var emails = await query
                .OrderByDescending(e => e.ReceivedAt)
                .Take(limit)
                .Select(e => new
                {
                    e.Id,
                    e.Subject,
                    e.FromAddress,
                    e.FromName,
                    e.ToAddresses,
                    e.Folder,
                    e.IsRead,
                    e.HasAttachments,
                    e.Importance,
                    e.ReceivedAt,
                    e.MatterId,
                    e.ClientId
                })
                .ToListAsync();

            return Ok(emails);
        }

        // GET: api/emails/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EmailMessage>> GetEmail(string id)
        {
            var email = await _context.EmailMessages.FindAsync(id);
            if (email == null)
            {
                return NotFound();
            }

            // Mark as read
            if (!email.IsRead)
            {
                email.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(email);
        }

        // POST: api/emails/{id}/link
        [HttpPost("{id}/link")]
        public async Task<IActionResult> LinkEmail(string id, [FromBody] LinkEmailDto dto)
        {
            var email = await _context.EmailMessages.FindAsync(id);
            if (email == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(dto.MatterId))
            {
                email.MatterId = dto.MatterId;
            }

            if (!string.IsNullOrEmpty(dto.ClientId))
            {
                email.ClientId = dto.ClientId;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email linked successfully" });
        }

        // POST: api/emails/{id}/unlink
        [HttpPost("{id}/unlink")]
        public async Task<IActionResult> UnlinkEmail(string id)
        {
            var email = await _context.EmailMessages.FindAsync(id);
            if (email == null)
            {
                return NotFound();
            }

            email.MatterId = null;
            email.ClientId = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email unlinked" });
        }

        // GET: api/emails/accounts
        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<EmailAccount>>> GetEmailAccounts()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var accounts = await _context.EmailAccounts
                .Where(a => a.UserId == userId)
                .Select(a => new
                {
                    a.Id,
                    a.Provider,
                    a.EmailAddress,
                    a.DisplayName,
                    a.IsActive,
                    a.SyncEnabled,
                    a.LastSyncAt,
                    a.SyncError
                })
                .ToListAsync();

            return Ok(accounts);
        }

        // POST: api/emails/accounts/connect/outlook
        [HttpPost("accounts/connect/outlook")]
        public async Task<IActionResult> ConnectOutlook([FromBody] ConnectOutlookDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // TODO: Exchange authorization code for tokens using Microsoft Graph
            // For now, store placeholder

            var account = new EmailAccount
            {
                UserId = userId ?? "",
                Provider = "Outlook",
                EmailAddress = dto.Email,
                DisplayName = dto.DisplayName,
                AccessToken = dto.AccessToken,
                RefreshToken = dto.RefreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddHours(1),
                IsActive = true,
                SyncEnabled = true
            };

            _context.EmailAccounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Outlook account connected", accountId = account.Id });
        }

        // POST: api/emails/accounts/connect/gmail
        [HttpPost("accounts/connect/gmail")]
        public async Task<IActionResult> ConnectGmail([FromBody] ConnectGmailDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // TODO: Exchange authorization code for tokens using Gmail API
            // For now, store placeholder

            var account = new EmailAccount
            {
                UserId = userId ?? "",
                Provider = "Gmail",
                EmailAddress = dto.Email,
                DisplayName = dto.DisplayName,
                AccessToken = dto.AccessToken,
                RefreshToken = dto.RefreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddHours(1),
                IsActive = true,
                SyncEnabled = true
            };

            _context.EmailAccounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gmail account connected", accountId = account.Id });
        }

        // POST: api/emails/accounts/{id}/sync
        [HttpPost("accounts/{id}/sync")]
        public async Task<IActionResult> SyncAccount(string id)
        {
            var account = await _context.EmailAccounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            // TODO: Implement actual sync with Microsoft Graph or Gmail API
            // For now, just update the last sync timestamp

            account.LastSyncAt = DateTime.UtcNow;
            account.SyncError = null;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Sync started", lastSyncAt = account.LastSyncAt });
        }

        // DELETE: api/emails/accounts/{id}
        [HttpDelete("accounts/{id}")]
        public async Task<IActionResult> DisconnectAccount(string id)
        {
            var account = await _context.EmailAccounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            // Delete associated emails
            var emails = await _context.EmailMessages
                .Where(e => e.EmailAccountId == id)
                .ToListAsync();

            _context.EmailMessages.RemoveRange(emails);
            _context.EmailAccounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/emails/unlinked
        [HttpGet("unlinked")]
        public async Task<ActionResult<IEnumerable<EmailMessage>>> GetUnlinkedEmails([FromQuery] int limit = 50)
        {
            var emails = await _context.EmailMessages
                .Where(e => e.MatterId == null && e.ClientId == null)
                .OrderByDescending(e => e.ReceivedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(emails);
        }

        // POST: api/emails/auto-link
        [HttpPost("auto-link")]
        public async Task<IActionResult> AutoLinkEmails()
        {
            // Find unlinked emails
            var unlinkedEmails = await _context.EmailMessages
                .Where(e => e.MatterId == null && e.ClientId == null)
                .ToListAsync();

            var clients = await _context.Clients.ToListAsync();

            int linkedCount = 0;

            foreach (var email in unlinkedEmails)
            {
                // Try to match email address to a client
                var matchedClient = clients.FirstOrDefault(c =>
                    c.Email.Equals(email.FromAddress, StringComparison.OrdinalIgnoreCase) ||
                    email.ToAddresses.Contains(c.Email, StringComparison.OrdinalIgnoreCase));

                if (matchedClient != null)
                {
                    email.ClientId = matchedClient.Id;

                    // Try to find an active matter for this client
                    var matter = await _context.Matters
                        .Where(m => m.ClientId == matchedClient.Id && m.Status == "Active")
                        .OrderByDescending(m => m.OpenDate)
                        .FirstOrDefaultAsync();

                    if (matter != null)
                    {
                        email.MatterId = matter.Id;
                    }

                    linkedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Auto-linked {linkedCount} emails", linkedCount });
        }
    }

    // DTOs
    public class LinkEmailDto
    {
        public string? MatterId { get; set; }
        public string? ClientId { get; set; }
    }

    public class ConnectOutlookDto
    {
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class ConnectGmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
