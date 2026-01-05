using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlDoc = DocumentFormat.OpenXml.Wordprocessing.Document;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
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

        public DocumentsController(JurisFlowDbContext context, IWebHostEnvironment env, AuditLogger auditLogger)
        {
            _context = context;
            _env = env;
            _auditLogger = auditLogger;
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

        private static string ExtractDocxText(string filePath)
        {
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return string.Empty;
                return body.InnerText ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static async Task<string> ExtractTextAsync(string fullPath)
        {
            try
            {
                var info = new FileInfo(fullPath);
                if (info.Length > 25 * 1024 * 1024)
                {
                    return string.Empty; // skip very large files
                }
            }
            catch
            {
                return string.Empty;
            }

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".txt" || ext == ".md")
            {
                return await System.IO.File.ReadAllTextAsync(fullPath);
            }
            if (ext == ".docx")
            {
                return ExtractDocxText(fullPath);
            }
            if (ext == ".pdf")
            {
                return ExtractPdfText(fullPath);
            }
            // Unsupported types: return empty to avoid false positives
            return string.Empty;
        }

        private static string ExtractPdfText(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                using var doc = PdfDocument.Open(filePath);
                foreach (Page page in doc.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
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

            var candidates = await docsQuery.OrderByDescending(d => d.CreatedAt).ToListAsync();

            var matches = new List<Document>();
            foreach (var doc in candidates)
            {
                // Name/description/tags quick check
                if ((!string.IsNullOrEmpty(doc.Name) && doc.Name.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(doc.FileName) && doc.FileName.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(doc.Description) && doc.Description.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrEmpty(doc.Tags) && doc.Tags.ToLowerInvariant().Contains(normalized)))
                {
                    matches.Add(doc);
                    continue;
                }

                if (!includeContent)
                {
                    continue;
                }

                // Content check (txt/docx/pdf only)
                var fullPath = Path.Combine(_env.ContentRootPath, doc.FilePath);
                if (!System.IO.File.Exists(fullPath)) continue;

                var content = await ExtractTextAsync(fullPath);
                if (!string.IsNullOrEmpty(content) && content.ToLowerInvariant().Contains(normalized))
                {
                    matches.Add(doc);
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

            await _auditLogger.LogAsync(HttpContext, "document.upload", "Document", document.Id, $"MatterId={matterId}, Name={file.FileName}, Size={file.Length}");

            return Ok(document);
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
}
