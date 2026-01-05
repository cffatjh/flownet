using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Enums;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/client")]
    [ApiController]
    [Authorize(Roles = "Client")]
    public class ClientPortalController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogger _auditLogger;

        public ClientPortalController(JurisFlowDbContext context, IWebHostEnvironment env, AuditLogger auditLogger)
        {
            _context = context;
            _env = env;
            _auditLogger = auditLogger;
        }

        [HttpGet("matters")]
        public async Task<IActionResult> GetMatters()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();

            var matters = await _context.Matters
                .Where(m => m.ClientId == clientId)
                .OrderByDescending(m => m.OpenDate)
                .ToListAsync();

            var matterIds = matters.Select(m => m.Id).ToList();
            var events = await _context.CalendarEvents
                .Where(e => e.MatterId != null && matterIds.Contains(e.MatterId))
                .OrderBy(e => e.Date)
                .ToListAsync();

            var eventsByMatter = events
                .GroupBy(e => e.MatterId)
                .ToDictionary(g => g.Key!, g => g.Select(e => (object)new
                {
                    id = e.Id,
                    title = e.Title,
                    date = e.Date,
                    type = e.Type,
                    matterId = e.MatterId
                }).ToList());

            var response = matters.Select(m => new
            {
                id = m.Id,
                caseNumber = m.CaseNumber,
                name = m.Name,
                practiceArea = m.PracticeArea,
                status = m.Status,
                feeStructure = m.FeeStructure,
                openDate = m.OpenDate,
                responsibleAttorney = m.ResponsibleAttorney,
                billableRate = m.BillableRate,
                trustBalance = m.TrustBalance,
                courtType = m.CourtType,
                outcome = m.Outcome,
                events = eventsByMatter.TryGetValue(m.Id, out var list) ? list : new List<object>(),
                timeEntries = Array.Empty<object>(),
                expenses = Array.Empty<object>()
            });

            return Ok(response);
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var client = await _context.Clients.FindAsync(clientId);

            var invoices = await _context.Invoices
                .Where(i => i.ClientId == clientId)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            var response = invoices.Select(i => new
            {
                id = i.Id,
                number = i.Number,
                clientId = i.ClientId,
                client = client == null ? null : new { id = client.Id, name = client.Name },
                status = NormalizeInvoiceStatus(i.Status),
                issueDate = i.IssueDate,
                dueDate = i.DueDate,
                amount = i.Total,
                amountPaid = i.AmountPaid,
                balance = i.Balance,
                notes = i.Notes,
                terms = i.Terms,
                createdAt = i.CreatedAt,
                updatedAt = i.UpdatedAt
            });

            return Ok(response);
        }

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var matterIds = await _context.Matters
                .Where(m => m.ClientId == clientId)
                .Select(m => m.Id)
                .ToListAsync();

            var documents = await _context.Documents
                .Where(d => (d.MatterId != null && matterIds.Contains(d.MatterId)) || d.UploadedBy == clientId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var response = documents.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                fileName = d.FileName,
                filePath = NormalizeFilePath(d.FilePath),
                fileSize = d.FileSize,
                mimeType = d.MimeType,
                matterId = d.MatterId,
                description = d.Description,
                tags = d.Tags,
                category = d.Category,
                version = d.Version,
                createdAt = d.CreatedAt,
                updatedAt = d.UpdatedAt
            });

            return Ok(response);
        }

        [HttpPost("documents/upload")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string? matterId, [FromForm] string? description)
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is required." });
            }

            if (file.Length > 25 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds 25 MB limit." });
            }

            if (!string.IsNullOrEmpty(matterId))
            {
                var ownsMatter = await _context.Matters.AnyAsync(m => m.Id == matterId && m.ClientId == clientId);
                if (!ownsMatter)
                {
                    return Forbid();
                }
            }

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new Document
            {
                Id = Guid.NewGuid().ToString(),
                Name = file.FileName,
                FileName = file.FileName,
                FilePath = "uploads/" + uniqueFileName,
                FileSize = file.Length,
                MimeType = file.ContentType,
                MatterId = matterId,
                Description = description,
                UploadedBy = clientId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                FileName = document.FileName,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                Sha256 = ComputeSha256(filePath),
                UploadedByUserId = clientId,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "client.document.upload", "Document", document.Id, $"MatterId={matterId}, Name={file.FileName}");

            return Ok(new
            {
                id = document.Id,
                name = document.Name,
                fileName = document.FileName,
                filePath = NormalizeFilePath(document.FilePath),
                fileSize = document.FileSize,
                mimeType = document.MimeType,
                matterId = document.MatterId,
                description = document.Description,
                tags = document.Tags,
                category = document.Category,
                version = document.Version,
                createdAt = document.CreatedAt,
                updatedAt = document.UpdatedAt
            });
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var items = await _context.Notifications
                .Where(n => n.ClientId == clientId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return NotFound();

            return Ok(new
            {
                id = client.Id,
                name = client.Name,
                email = client.Email,
                phone = client.Phone,
                mobile = client.Mobile,
                company = client.Company,
                type = client.Type,
                status = client.Status,
                address = client.Address,
                city = client.City,
                state = client.State,
                zipCode = client.ZipCode,
                country = client.Country
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ClientProfileUpdateDto dto)
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return NotFound();

            client.Name = dto.Name ?? client.Name;
            client.Phone = dto.Phone;
            client.Mobile = dto.Mobile;
            client.Address = dto.Address;
            client.City = dto.City;
            client.State = dto.State;
            client.ZipCode = dto.ZipCode;
            client.Country = dto.Country;
            client.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "client.profile.update", "Client", client.Id, "Client profile updated.");

            return Ok(new
            {
                id = client.Id,
                name = client.Name,
                email = client.Email,
                phone = client.Phone,
                mobile = client.Mobile,
                company = client.Company,
                type = client.Type,
                status = client.Status,
                address = client.Address,
                city = client.City,
                state = client.State,
                zipCode = client.ZipCode,
                country = client.Country
            });
        }

        [HttpGet("signatures")]
        public async Task<IActionResult> GetSignatureRequests()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var client = await _context.Clients.FindAsync(clientId);

            var requests = await _context.SignatureRequests
                .Where(r => r.ClientId == clientId || (client != null && r.SignerEmail == client.Email))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var docIds = requests.Select(r => r.DocumentId).Distinct().ToList();
            var docs = await _context.Documents
                .Where(d => docIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();
            var docMap = docs.ToDictionary(d => d.Id, d => d.Name);

            var response = requests.Select(r => new
            {
                id = r.Id,
                documentId = r.DocumentId,
                document = docMap.TryGetValue(r.DocumentId, out var name) ? new { id = r.DocumentId, name } : null,
                status = NormalizeSignatureStatus(r.Status),
                signedAt = r.SignedAt,
                createdAt = r.CreatedAt,
                expiresAt = r.ExpiresAt
            });

            return Ok(response);
        }

        [HttpPost("sign/{id}")]
        public async Task<IActionResult> SignRequest(string id, [FromBody] ClientSignatureDto dto)
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var client = await _context.Clients.FindAsync(clientId);

            var request = await _context.SignatureRequests.FindAsync(id);
            if (request == null) return NotFound();

            var isClientOwner = request.ClientId == clientId;
            var isSigner = client != null && string.Equals(request.SignerEmail, client.Email, StringComparison.OrdinalIgnoreCase);
            if (!isClientOwner && !isSigner)
            {
                return Forbid();
            }

            if (request.Status == "Signed")
            {
                return BadRequest(new { message = "Document already signed." });
            }

            request.Status = "Signed";
            request.SignedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.SignerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.SignerUserAgent = Request.Headers.UserAgent.ToString();
            request.SignerLocation = dto.SignerLocation;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "client.signature.sign", "SignatureRequest", id, $"Signer={request.SignerEmail}");

            return Ok(new { message = "Document signed.", signedAt = request.SignedAt });
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments()
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();
            var appointments = await _context.AppointmentRequests
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.RequestedDate)
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpPost("appointments")]
        public async Task<IActionResult> CreateAppointment([FromBody] ClientAppointmentCreateDto dto)
        {
            if (!TryGetClientId(out var clientId)) return Unauthorized();

            if (dto.RequestedDate == default)
            {
                return BadRequest(new { message = "Requested date is required." });
            }

            if (dto.RequestedDate < DateTime.UtcNow.AddMinutes(-5))
            {
                return BadRequest(new { message = "Requested date must be in the future." });
            }

            if (dto.Duration <= 0 || dto.Duration > 240)
            {
                return BadRequest(new { message = "Duration must be between 15 and 240 minutes." });
            }

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "consultation",
                "meeting",
                "call",
                "court"
            };

            if (!string.IsNullOrWhiteSpace(dto.Type) && !allowedTypes.Contains(dto.Type))
            {
                return BadRequest(new { message = "Invalid appointment type." });
            }

            if (!string.IsNullOrEmpty(dto.MatterId))
            {
                var ownsMatter = await _context.Matters.AnyAsync(m => m.Id == dto.MatterId && m.ClientId == clientId);
                if (!ownsMatter)
                {
                    return Forbid();
                }
            }

            var appointment = new AppointmentRequest
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = clientId,
                MatterId = dto.MatterId,
                RequestedDate = dto.RequestedDate,
                Duration = dto.Duration,
                Type = string.IsNullOrWhiteSpace(dto.Type) ? "consultation" : dto.Type.ToLowerInvariant(),
                Notes = dto.Notes,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AppointmentRequests.Add(appointment);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "client.appointment.create", "AppointmentRequest", appointment.Id, $"RequestedDate={dto.RequestedDate:o}");

            return Ok(appointment);
        }

        private bool TryGetClientId(out string clientId)
        {
            clientId = GetClientId() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(clientId);
        }

        private string? GetClientId()
        {
            return User.FindFirst("clientId")?.Value
                   ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private static string NormalizeInvoiceStatus(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.Draft => "Draft",
                InvoiceStatus.PendingApproval => "Pending Approval",
                InvoiceStatus.Approved => "Approved",
                InvoiceStatus.Sent => "Sent",
                InvoiceStatus.PartiallyPaid => "Partially Paid",
                InvoiceStatus.Paid => "Paid",
                InvoiceStatus.Overdue => "Overdue",
                InvoiceStatus.WrittenOff => "Written Off",
                InvoiceStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }

        private static string NormalizeSignatureStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "pending";
            var normalized = status.ToLowerInvariant();
            return normalized switch
            {
                "sent" => "pending",
                "pending" => "pending",
                "viewed" => "pending",
                "signed" => "signed",
                "declined" => "declined",
                "voided" => "declined",
                "expired" => "expired",
                _ => normalized
            };
        }

        private static string NormalizeFilePath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
            return filePath.StartsWith("/") ? filePath : "/" + filePath;
        }

        private static string ComputeSha256(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = System.IO.File.OpenRead(filePath);
            var hash = sha.ComputeHash(stream);
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public class ClientProfileUpdateDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
    }

    public class ClientSignatureDto
    {
        public string? SignatureData { get; set; }
        public string? SignerLocation { get; set; }
    }

    public class ClientAppointmentCreateDto
    {
        public string? MatterId { get; set; }
        public DateTime RequestedDate { get; set; }
        public int Duration { get; set; } = 30;
        public string? Type { get; set; }
        public string? Notes { get; set; }
    }
}

