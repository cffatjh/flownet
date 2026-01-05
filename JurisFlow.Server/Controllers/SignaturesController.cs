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
    public class SignaturesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuditLogger _auditLogger;

        public SignaturesController(JurisFlowDbContext context, IConfiguration configuration, AuditLogger auditLogger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        // POST: api/signatures/request
        [HttpPost("request")]
        public async Task<ActionResult<SignatureRequest>> CreateSignatureRequest([FromBody] CreateSignatureRequestDto dto)
        {
            var document = await _context.Documents.FindAsync(dto.DocumentId);
            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var signatureRequest = new SignatureRequest
            {
                DocumentId = dto.DocumentId,
                SignerEmail = dto.SignerEmail,
                SignerName = dto.SignerName ?? "",
                MatterId = document.MatterId,
                ClientId = dto.ClientId,
                Status = "Pending",
                ExpiresAt = dto.ExpiresAt ?? DateTime.UtcNow.AddDays(30),
                RequestedBy = userId
            };

            // TODO: Integrate with DocuSign API
            // For now, we'll create a mock signing URL
            signatureRequest.SigningUrl = $"/sign/{signatureRequest.Id}";
            signatureRequest.Status = "Sent";
            signatureRequest.SentAt = DateTime.UtcNow;

            _context.SignatureRequests.Add(signatureRequest);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "esign.request.create", "SignatureRequest", signatureRequest.Id, $"Signer={signatureRequest.SignerEmail}, Document={signatureRequest.DocumentId}");

            return Ok(signatureRequest);
        }

        // GET: api/signatures/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SignatureRequest>> GetSignatureRequest(string id)
        {
            var request = await _context.SignatureRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            return Ok(request);
        }

        // GET: api/signatures/document/{documentId}
        [HttpGet("document/{documentId}")]
        public async Task<ActionResult<IEnumerable<SignatureRequest>>> GetDocumentSignatures(string documentId)
        {
            var requests = await _context.SignatureRequests
                .Where(r => r.DocumentId == documentId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        // GET: api/signatures/matter/{matterId}
        [HttpGet("matter/{matterId}")]
        public async Task<ActionResult<IEnumerable<SignatureRequest>>> GetMatterSignatures(string matterId)
        {
            var requests = await _context.SignatureRequests
                .Where(r => r.MatterId == matterId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        // POST: api/signatures/{id}/sign
        [HttpPost("{id}/sign")]
        public async Task<IActionResult> SignDocument(string id, [FromBody] SignRequestDto dto)
        {
            var request = await _context.SignatureRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status == "Signed")
            {
                return BadRequest(new { message = "Document already signed" });
            }

            request.Status = "Signed";
            request.SignedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.SignerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.SignerUserAgent = Request.Headers.UserAgent.ToString();
            request.SignerLocation = dto.SignerLocation;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "esign.sign", "SignatureRequest", id, $"Signer={request.SignerEmail}, Ip={request.SignerIp}");

            return Ok(new { message = "Document signed successfully", signedAt = request.SignedAt });
        }

        // POST: api/signatures/{id}/decline
        [HttpPost("{id}/decline")]
        public async Task<IActionResult> DeclineSignature(string id, [FromBody] DeclineSignatureDto dto)
        {
            var request = await _context.SignatureRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            request.Status = "Declined";
            request.DeclineReason = dto.Reason;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "esign.decline", "SignatureRequest", id, $"Reason={dto.Reason}");

            return Ok(new { message = "Signature declined" });
        }

        // POST: api/signatures/{id}/remind
        [HttpPost("{id}/remind")]
        public async Task<IActionResult> SendReminder(string id)
        {
            var request = await _context.SignatureRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Sent" && request.Status != "Viewed")
            {
                return BadRequest(new { message = "Cannot send reminder for this status" });
            }

            // TODO: Send email reminder via DocuSign or custom email
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reminder sent" });
        }

        // POST: api/signatures/{id}/void
        [HttpPost("{id}/void")]
        public async Task<IActionResult> VoidSignatureRequest(string id)
        {
            var request = await _context.SignatureRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status == "Signed")
            {
                return BadRequest(new { message = "Cannot void a signed document" });
            }

            request.Status = "Voided";
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "esign.void", "SignatureRequest", id, $"RequestedBy={User.Identity?.Name}");

            return Ok(new { message = "Signature request voided" });
        }

        // POST: api/signatures/webhook (DocuSign webhook endpoint)
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            // TODO: Implement DocuSign Connect webhook handling
            // Verify HMAC signature
            // Parse envelope status
            // Update signature request status

            return Ok();
        }
    }

    // DTOs
    public class CreateSignatureRequestDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string SignerEmail { get; set; } = string.Empty;
        public string? SignerName { get; set; }
        public string? ClientId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class DeclineSignatureDto
    {
        public string? Reason { get; set; }
    }

    public class SignRequestDto
    {
        public string? SignerLocation { get; set; }
    }
}
