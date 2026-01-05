using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using System.Text.RegularExpressions;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConflictsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public ConflictsController(JurisFlowDbContext context)
        {
            _context = context;
        }

        // POST: api/conflicts/check
        [HttpPost("check")]
        public async Task<ActionResult<ConflictCheckResultDto>> RunConflictCheck([FromBody] ConflictCheckRequestDto request)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Create conflict check record
            var conflictCheck = new ConflictCheck
            {
                SearchQuery = request.SearchQuery,
                CheckType = request.CheckType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                CheckedBy = userId,
                Status = "Pending"
            };

            _context.ConflictChecks.Add(conflictCheck);
            await _context.SaveChangesAsync();

            var results = new List<JurisFlow.Server.Models.ConflictResult>();
            var searchQuery = request.SearchQuery.ToLowerInvariant().Trim();

            // Search Clients
            var clients = await _context.Clients.ToListAsync();
            foreach (var client in clients)
            {
                if (client.Id == request.EntityId) continue;
                if (client.Name.ToLowerInvariant().Contains(searchQuery) ||
                    (client.Email?.ToLowerInvariant().Contains(searchQuery) ?? false))
                {
                    var matter = await _context.Matters.FirstOrDefaultAsync(m => m.ClientId == client.Id);
                    results.Add(new JurisFlow.Server.Models.ConflictResult
                    {
                        ConflictCheckId = conflictCheck.Id,
                        MatchedEntityType = "Client",
                        MatchedEntityId = client.Id,
                        MatchedEntityName = client.Name,
                        MatchType = "Exact",
                        MatchScore = 100,
                        RiskLevel = "High",
                        RelatedMatterId = matter?.Id,
                        RelatedMatterName = matter?.Name
                    });
                }
            }

            // Search Opposing Parties
            var parties = await _context.OpposingParties.ToListAsync();
            foreach (var party in parties)
            {
                if (party.Id == request.EntityId) continue;
                if (party.Name.ToLowerInvariant().Contains(searchQuery))
                {
                    var matter = await _context.Matters.FindAsync(party.MatterId);
                    results.Add(new JurisFlow.Server.Models.ConflictResult
                    {
                        ConflictCheckId = conflictCheck.Id,
                        MatchedEntityType = "OpposingParty",
                        MatchedEntityId = party.Id,
                        MatchedEntityName = party.Name,
                        MatchType = "Exact",
                        MatchScore = 100,
                        RiskLevel = "High",
                        RelatedMatterId = party.MatterId,
                        RelatedMatterName = matter?.Name
                    });
                }
            }

            // Save results
            if (results.Any())
            {
                _context.ConflictResults.AddRange(results);
                conflictCheck.Status = "Conflict";
                conflictCheck.MatchCount = results.Count;
            }
            else
            {
                conflictCheck.Status = "Clear";
            }

            await _context.SaveChangesAsync();

            return Ok(new ConflictCheckResultDto
            {
                Id = conflictCheck.Id,
                Status = conflictCheck.Status,
                MatchCount = results.Count,
                Results = results.Select(r => new ConflictResultDto
                {
                    Id = r.Id,
                    MatchedEntityType = r.MatchedEntityType,
                    MatchedEntityId = r.MatchedEntityId,
                    MatchedEntityName = r.MatchedEntityName,
                    MatchType = r.MatchType,
                    MatchScore = r.MatchScore,
                    RiskLevel = r.RiskLevel,
                    RelatedMatterId = r.RelatedMatterId,
                    RelatedMatterName = r.RelatedMatterName
                }).ToList()
            });
        }

        // GET: api/conflicts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ConflictCheckResultDto>> GetConflictCheck(string id)
        {
            var check = await _context.ConflictChecks.FindAsync(id);
            if (check == null) return NotFound();

            var results = await _context.ConflictResults
                .Where(r => r.ConflictCheckId == id)
                .ToListAsync();

            return Ok(new ConflictCheckResultDto
            {
                Id = check.Id,
                Status = check.Status,
                MatchCount = check.MatchCount,
                SearchQuery = check.SearchQuery,
                CheckType = check.CheckType,
                WaivedBy = check.WaivedBy,
                WaiverReason = check.WaiverReason,
                Results = results.Select(r => new ConflictResultDto
                {
                    Id = r.Id,
                    MatchedEntityType = r.MatchedEntityType,
                    MatchedEntityId = r.MatchedEntityId,
                    MatchedEntityName = r.MatchedEntityName,
                    MatchType = r.MatchType,
                    MatchScore = r.MatchScore,
                    RiskLevel = r.RiskLevel,
                    RelatedMatterId = r.RelatedMatterId,
                    RelatedMatterName = r.RelatedMatterName
                }).ToList()
            });
        }

        // POST: api/conflicts/{id}/waive
        [HttpPost("{id}/waive")]
        public async Task<IActionResult> WaiveConflict(string id, [FromBody] WaiveConflictDto dto)
        {
            var check = await _context.ConflictChecks.FindAsync(id);
            if (check == null) return NotFound();

            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            check.Status = "Waived";
            check.WaivedBy = userId;
            check.WaiverReason = dto.Reason;
            check.WaivedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Conflict waived successfully" });
        }

        // GET: api/conflicts/history
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<ConflictCheckSummaryDto>>> GetConflictHistory([FromQuery] int limit = 50)
        {
            var checks = await _context.ConflictChecks
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new ConflictCheckSummaryDto
                {
                    Id = c.Id,
                    SearchQuery = c.SearchQuery,
                    CheckType = c.CheckType,
                    Status = c.Status,
                    MatchCount = c.MatchCount,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(checks);
        }
    }

    // DTOs
    public class ConflictCheckRequestDto
    {
        public string SearchQuery { get; set; } = string.Empty;
        public string? CheckType { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
    }

    public class ConflictCheckResultDto
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "";
        public int MatchCount { get; set; }
        public string? SearchQuery { get; set; }
        public string? CheckType { get; set; }
        public string? WaivedBy { get; set; }
        public string? WaiverReason { get; set; }
        public List<ConflictResultDto> Results { get; set; } = new();
    }

    public class ConflictResultDto
    {
        public string Id { get; set; } = "";
        public string MatchedEntityType { get; set; } = "";
        public string MatchedEntityId { get; set; } = "";
        public string MatchedEntityName { get; set; } = "";
        public string MatchType { get; set; } = "";
        public double MatchScore { get; set; }
        public string RiskLevel { get; set; } = "";
        public string? RelatedMatterId { get; set; }
        public string? RelatedMatterName { get; set; }
    }

    public class WaiveConflictDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ConflictCheckSummaryDto
    {
        public string Id { get; set; } = "";
        public string? SearchQuery { get; set; }
        public string? CheckType { get; set; }
        public string Status { get; set; } = "";
        public int MatchCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
