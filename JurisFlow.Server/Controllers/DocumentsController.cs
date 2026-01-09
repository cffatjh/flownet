using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogger _auditLogger;
        private readonly DocumentIndexService _documentIndexService;

        public DocumentsController(JurisFlowDbContext context, IWebHostEnvironment env, AuditLogger auditLogger, DocumentIndexService documentIndexService)
        {
            _context = context;
            _env = env;
            _auditLogger = auditLogger;
            _documentIndexService = documentIndexService;
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

        private static string? SerializeTags(List<string>? tags)
        {
            if (tags == null || tags.Count == 0) return null;
            return JsonSerializer.Serialize(tags);
        }

        // GET: api/Documents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetDocuments([FromQuery] string? matterId)
        {
            var query = _context.Documents.AsQueryable();

            if (!string.IsNullOrEmpty(matterId))
            {
                query = query.Where(d => d.MatterId == matterId);
            }

            return await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        }

        // PUT: api/Documents/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(string id, [FromBody] DocumentUpdateDto dto)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            if (dto.MatterId.HasValue)
            {
                var matterValue = dto.MatterId.Value;
                document.MatterId = matterValue.ValueKind == JsonValueKind.Null ? null : matterValue.GetString();
            }
            if (dto.Description != null)
            {
                document.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
            }
            if (dto.Category != null)
            {
                document.Category = string.IsNullOrWhiteSpace(dto.Category) ? null : dto.Category;
            }
            if (dto.Tags.HasValue)
            {
                var tagsValue = dto.Tags.Value;
                if (tagsValue.ValueKind == JsonValueKind.Null)
                {
                    document.Tags = null;
                }
                else if (tagsValue.ValueKind == JsonValueKind.Array)
                {
                    var tags = new List<string>();
                    foreach (var item in tagsValue.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var tag = item.GetString();
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                tags.Add(tag.Trim());
                            }
                        }
                    }
                    document.Tags = SerializeTags(tags);
                }
                else if (tagsValue.ValueKind == JsonValueKind.String)
                {
                    var raw = tagsValue.GetString() ?? string.Empty;
                    var tags = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => t.Length > 0)
                        .ToList();
                    document.Tags = SerializeTags(tags);
                }
            }

            var previousStatus = document.Status;
            if (dto.Status != null)
            {
                document.Status = string.IsNullOrWhiteSpace(dto.Status) ? null : dto.Status;
            }

            if (!string.IsNullOrWhiteSpace(dto.LegalHoldReason))
            {
                document.LegalHoldReason = dto.LegalHoldReason;
            }

            var isLegalHold = string.Equals(document.Status, "Legal Hold", StringComparison.OrdinalIgnoreCase);
            if (isLegalHold)
            {
                if (!document.LegalHoldPlacedAt.HasValue)
                {
                    document.LegalHoldPlacedAt = DateTime.UtcNow;
                }
                if (string.IsNullOrEmpty(document.LegalHoldPlacedBy))
                {
                    document.LegalHoldPlacedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }
                document.LegalHoldReleasedAt = null;
                document.LegalHoldReleasedBy = null;
            }
            else if (string.Equals(previousStatus, "Legal Hold", StringComparison.OrdinalIgnoreCase))
            {
                document.LegalHoldReleasedAt = DateTime.UtcNow;
                document.LegalHoldReleasedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "document.update", "Document", document.Id, $"Status={document.Status}, MatterId={document.MatterId}");

            return Ok(document);
        }

        // PUT: api/Documents/bulk-assign
        [HttpPut("bulk-assign")]
        public async Task<IActionResult> BulkAssign([FromBody] DocumentBulkAssignDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return BadRequest(new { message = "Document ids are required." });
            }

            var docs = await _context.Documents.Where(d => dto.Ids.Contains(d.Id)).ToListAsync();
            foreach (var doc in docs)
            {
                doc.MatterId = dto.MatterId;
                doc.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "document.bulk_assign", "Document", null, $"Count={docs.Count}, MatterId={dto.MatterId}");

            return Ok(new { updated = docs.Count });
        }

        // GET: api/Documents/{id}/versions
        [HttpGet("{id}/versions")]
        public async Task<IActionResult> GetVersions(string id)
        {
            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            return Ok(versions);
        }

        // GET: api/Documents/versions/{versionId}/download
        [HttpGet("versions/{versionId}/download")]
        public async Task<IActionResult> DownloadVersion(string versionId)
        {
            var version = await _context.DocumentVersions.FindAsync(versionId);
            if (version == null) return NotFound(new { message = "Version not found" });

            var fullPath = Path.Combine(_env.ContentRootPath, version.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new { message = "File not found" });
            }

            return PhysicalFile(fullPath, "application/octet-stream", version.FileName);
        }

        // POST: api/Documents/versions/{versionId}/restore
        [HttpPost("versions/{versionId}/restore")]
        public async Task<IActionResult> RestoreVersion(string versionId)
        {
            var version = await _context.DocumentVersions.FindAsync(versionId);
            if (version == null) return NotFound(new { message = "Version not found" });

            var document = await _context.Documents.FindAsync(version.DocumentId);
            if (document == null) return NotFound(new { message = "Document not found" });

            var sourcePath = Path.Combine(_env.ContentRootPath, version.FilePath);
            if (!System.IO.File.Exists(sourcePath))
            {
                return BadRequest(new { message = "Source file missing" });
            }

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + version.FileName;
            var destPath = Path.Combine(uploadsFolder, uniqueFileName);
            System.IO.File.Copy(sourcePath, destPath, true);

            document.FileName = version.FileName;
            document.Name = version.FileName;
            document.FilePath = "uploads/" + uniqueFileName;
            document.FileSize = new FileInfo(destPath).Length;
            document.Version += 1;
            document.UpdatedAt = DateTime.UtcNow;

            var restoredVersion = new DocumentVersion
            {
                DocumentId = document.Id,
                FileName = document.FileName,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                Sha256 = ComputeSha256(destPath),
                UploadedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentVersions.Add(restoredVersion);

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "document.version.restore", "Document", document.Id, $"VersionId={versionId}");

            return Ok(document);
        }

        // GET: api/Documents/versions/diff?leftVersionId=...&rightVersionId=...
        [HttpGet("versions/diff")]
        public async Task<IActionResult> DiffVersions([FromQuery] string leftVersionId, [FromQuery] string rightVersionId)
        {
            var left = await _context.DocumentVersions.FindAsync(leftVersionId);
            var right = await _context.DocumentVersions.FindAsync(rightVersionId);
            if (left == null || right == null) return NotFound(new { message = "Version not found" });

            var leftPath = Path.Combine(_env.ContentRootPath, left.FilePath);
            var rightPath = Path.Combine(_env.ContentRootPath, right.FilePath);

            if (!System.IO.File.Exists(leftPath) || !System.IO.File.Exists(rightPath))
                return BadRequest(new { message = "File(s) missing for diff" });

            var leftText = await System.IO.File.ReadAllTextAsync(leftPath);
            var rightText = await System.IO.File.ReadAllTextAsync(rightPath);

            var diff = BuildSimpleDiff(leftText, rightText);

            await _auditLogger.LogAsync(HttpContext, "document.diff", "DocumentVersion", $"{leftVersionId}->{rightVersionId}", $"Diff length={diff.Length}");

            return Ok(new
            {
                leftVersionId,
                rightVersionId,
                diff
            });
        }

        // GET: api/Documents/search?q=...
        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] string q, [FromQuery] string? matterId = null, [FromQuery] bool includeContent = false)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Query is required." });
            }

            var normalized = q.Trim().ToLowerInvariant();

            var docsQuery = _context.Documents.AsQueryable();
            if (!string.IsNullOrEmpty(matterId))
            {
                docsQuery = docsQuery.Where(d => d.MatterId == matterId);
            }

            var metadataMatches = await docsQuery
                .Where(doc =>
                    (!string.IsNullOrEmpty(doc.Name) && doc.Name.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(doc.FileName) && doc.FileName.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(doc.Description) && doc.Description.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(doc.Tags) && doc.Tags.ToLowerInvariant().Contains(normalized)))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var matches = new List<Document>(metadataMatches);

            if (includeContent)
            {
                var contentMatchIds = new List<string>();
                var tokens = DocumentIndexService.TokenizeQuery(normalized);

                if (tokens.Count > 0)
                {
                    contentMatchIds = await _context.DocumentContentTokens
                        .Where(t => tokens.Contains(t.Token))
                        .GroupBy(t => t.DocumentId)
                        .Where(g => g.Select(x => x.Token).Distinct().Count() >= tokens.Count)
                        .Select(g => g.Key)
                        .ToListAsync();

                    if (contentMatchIds.Count > 0 && normalized.Length >= 4 && normalized.Contains(' '))
                    {
                        contentMatchIds = await _context.DocumentContentIndexes
                            .Where(i => contentMatchIds.Contains(i.DocumentId) && i.NormalizedContent != null && i.NormalizedContent.Contains(normalized))
                            .Select(i => i.DocumentId)
                            .ToListAsync();
                    }
                }
                else if (normalized.Length >= 3)
                {
                    contentMatchIds = await _context.DocumentContentIndexes
                        .Where(i => i.NormalizedContent != null && i.NormalizedContent.Contains(normalized))
                        .Select(i => i.DocumentId)
                        .ToListAsync();
                }

                if (contentMatchIds.Count > 0)
                {
                    var contentMatches = await docsQuery
                        .Where(d => contentMatchIds.Contains(d.Id))
                        .OrderByDescending(d => d.CreatedAt)
                        .ToListAsync();

                    foreach (var doc in contentMatches)
                    {
                        if (matches.All(m => m.Id != doc.Id))
                        {
                            matches.Add(doc);
                        }
                    }
                }
            }

            await _auditLogger.LogAsync(HttpContext, "document.search", "Document", null, $"Query={q}, Results={matches.Count}");

            return Ok(matches);
        }

        private static string BuildSimpleDiff(string left, string right)
        {
            var builder = new InlineDiffBuilder(new Differ());
            var diff = builder.BuildDiffModel(left, right);
            var sb = new StringBuilder();
            sb.AppendLine("--- LEFT");
            sb.AppendLine("+++ RIGHT");
            foreach (var line in diff.Lines)
            {
                var prefix = line.Type switch
                {
                    ChangeType.Inserted => "+ ",
                    ChangeType.Deleted => "- ",
                    ChangeType.Modified => "~ ",
                    _ => "  "
                };
                sb.AppendLine(prefix + (line.Text ?? string.Empty));
            }
            return sb.ToString();
        }

        // POST: api/Documents/upload
        [HttpPost("upload")]
        public async Task<ActionResult<Document>> UploadDocument([FromForm] IFormFile file, [FromForm] string? matterId, [FromForm] string? description)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var document = new Document
            {
                Id = Guid.NewGuid().ToString(),
                Name = file.FileName,
                FileName = file.FileName,
                FilePath = "uploads/" + uniqueFileName, // Relative path for serving
                FileSize = file.Length,
                MimeType = file.ContentType,
                MatterId = matterId,
                Description = description,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Save version record
            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                FileName = file.FileName,
                FilePath = document.FilePath,
                FileSize = file.Length,
                Sha256 = ComputeSha256(filePath),
                UploadedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            await _documentIndexService.UpsertIndexAsync(document, filePath);
            await _auditLogger.LogAsync(HttpContext, "document.upload", "Document", document.Id, $"MatterId={matterId}, Name={file.FileName}, Size={file.Length}");

            return Ok(document);
        }

        // POST: api/Documents/{id}/versions
        [HttpPost("{id}/versions")]
        public async Task<IActionResult> UploadNewVersion(string id, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded." });
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            document.FileName = file.FileName;
            document.Name = file.FileName;
            document.FilePath = "uploads/" + uniqueFileName;
            document.FileSize = file.Length;
            document.MimeType = file.ContentType;
            document.Version += 1;
            document.UpdatedAt = DateTime.UtcNow;

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                FileName = file.FileName,
                FilePath = document.FilePath,
                FileSize = file.Length,
                Sha256 = ComputeSha256(filePath),
                UploadedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentVersions.Add(version);

            await _context.SaveChangesAsync();
            await _documentIndexService.UpsertIndexAsync(document, filePath);
            await _auditLogger.LogAsync(HttpContext, "document.version.upload", "Document", document.Id, $"Version={document.Version}");

            return Ok(document);
        }

        // POST: api/Documents/reindex?limit=200&force=true
        [HttpPost("reindex")]
        public async Task<IActionResult> ReindexDocuments([FromQuery] int limit = 200, [FromQuery] bool force = false)
        {
            var indexed = await _documentIndexService.ReindexAllAsync(limit, force);
            await _auditLogger.LogAsync(HttpContext, "document.reindex", "DocumentContentIndex", null, $"Count={indexed}, Force={force}");
            return Ok(new { indexedCount = indexed });
        }

        // DELETE: api/Documents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
             var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            if (string.Equals(document.Status, "Legal Hold", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Document is on legal hold and cannot be deleted." });
            }

            // Optional: Delete physical file
            var fullPath = Path.Combine(_env.ContentRootPath, document.FilePath); // Assuming FilePath is relative like "uploads/..."
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
            // If path stored was "uploads/...", correct join needs care.
            // If FilePath stored as "uploads/file.pdf", ContentRootPath + FilePath works if running from root.

            // Save version snapshot before delete
            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                FileName = document.FileName,
                FilePath = document.FilePath,
                FileSize = document.FileSize,
                Sha256 = null,
                UploadedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentVersions.Add(version);

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "document.delete", "Document", id, $"Deleted document {document.FileName}");

            return NoContent();
        }
    }

    public class DocumentUpdateDto
    {
        public JsonElement? MatterId { get; set; }
        public string? Description { get; set; }
        public JsonElement? Tags { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public string? LegalHoldReason { get; set; }
    }

    public class DocumentBulkAssignDto
    {
        public List<string> Ids { get; set; } = new();
        public string? MatterId { get; set; }
    }
}
