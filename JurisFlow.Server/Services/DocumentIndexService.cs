using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Services
{
    public class DocumentIndexService
    {
        private const int MaxIndexLength = 200000;
        private const int MaxTokenCount = 500;
        private const int MinTokenLength = 3;
        private readonly JurisFlowDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentIndexService(JurisFlowDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<DocumentContentIndex?> UpsertIndexAsync(Document document)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                return null;
            }

            var fullPath = Path.Combine(_env.ContentRootPath, document.FilePath);
            return await UpsertIndexAsync(document, fullPath);
        }

        public async Task<DocumentContentIndex?> UpsertIndexAsync(Document document, string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                return null;
            }

            var content = await ExtractTextAsync(fullPath);
            var normalized = NormalizeContent(content);
            var contentHash = ComputeSha256(normalized);
            var truncated = normalized.Length > MaxIndexLength ? normalized[..MaxIndexLength] : normalized;
            var tokens = Tokenize(normalized);

            var index = await _context.DocumentContentIndexes.FindAsync(document.Id);
            if (index == null)
            {
                index = new DocumentContentIndex
                {
                    DocumentId = document.Id
                };
                _context.DocumentContentIndexes.Add(index);
            }

            var existingTokenCount = await _context.DocumentContentTokens
                .Where(t => t.DocumentId == document.Id)
                .CountAsync();

            var contentChanged = index.ContentHash != contentHash;
            if (contentChanged)
            {
                index.Content = truncated;
                index.NormalizedContent = truncated;
                index.ContentHash = contentHash;
                index.ContentLength = normalized.Length;
                index.IndexedAt = DateTime.UtcNow;
            }

            if (contentChanged || existingTokenCount == 0)
            {
                var existingTokens = await _context.DocumentContentTokens
                    .Where(t => t.DocumentId == document.Id)
                    .ToListAsync();
                if (existingTokens.Count > 0)
                {
                    _context.DocumentContentTokens.RemoveRange(existingTokens);
                }

                if (tokens.Count > 0)
                {
                    var tokenEntities = tokens.Select(token => new DocumentContentToken
                    {
                        DocumentId = document.Id,
                        Token = token
                    });
                    await _context.DocumentContentTokens.AddRangeAsync(tokenEntities);
                }
            }

            await _context.SaveChangesAsync();
            return index;
        }

        public async Task<int> ReindexAllAsync(int limit = 200, bool force = false)
        {
            var query = _context.Documents.AsQueryable();
            if (!force)
            {
                query = query.Where(d => !_context.DocumentContentIndexes.Any(i => i.DocumentId == d.Id)
                    || !_context.DocumentContentTokens.Any(t => t.DocumentId == d.Id));
            }

            var docs = await query
                .OrderByDescending(d => d.UpdatedAt)
                .Take(limit)
                .ToListAsync();

            var count = 0;
            foreach (var doc in docs)
            {
                if (string.IsNullOrWhiteSpace(doc.FilePath))
                {
                    continue;
                }
                var fullPath = Path.Combine(_env.ContentRootPath, doc.FilePath);
                var indexed = await UpsertIndexAsync(doc, fullPath);
                if (indexed != null)
                {
                    count += 1;
                }
            }

            return count;
        }

        public static List<string> TokenizeQuery(string input, int maxTokens = 8)
        {
            return Tokenize(NormalizeContent(input), maxTokens);
        }

        private static string NormalizeContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;
            return content.ToLowerInvariant();
        }

        private static List<string> Tokenize(string normalized, int maxTokens = MaxTokenCount)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return new List<string>();
            var matches = Regex.Matches(normalized, "[a-z0-9]+");
            var tokens = new HashSet<string>();
            foreach (Match match in matches)
            {
                var value = match.Value;
                if (value.Length < MinTokenLength) continue;
                if (tokens.Add(value) && tokens.Count >= maxTokens)
                {
                    break;
                }
            }
            return tokens.ToList();
        }

        private static async Task<string> ExtractTextAsync(string fullPath)
        {
            try
            {
                var info = new FileInfo(fullPath);
                if (info.Length > 25 * 1024 * 1024)
                {
                    return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".txt" || ext == ".md")
            {
                return await File.ReadAllTextAsync(fullPath);
            }
            if (ext == ".docx")
            {
                return ExtractDocxText(fullPath);
            }
            if (ext == ".pdf")
            {
                return ExtractPdfText(fullPath);
            }

            return string.Empty;
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

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
