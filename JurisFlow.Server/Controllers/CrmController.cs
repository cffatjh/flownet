using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;

namespace JurisFlow.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CrmController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public CrmController(JurisFlowDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Conflict of Interest Check - ABA Model Rules 1.7, 1.9, 1.10
        /// Searches across Clients, Leads, and Matters to identify potential conflicts
        /// </summary>
        [HttpGet("conflict-check")]
        public async Task<ActionResult<IEnumerable<ConflictCheckResult>>> ConflictCheck([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return BadRequest(new { message = "Search query must be at least 2 characters" });
            }

            var query = q.ToLower().Trim();
            var results = new List<ConflictCheckResult>();

            // 1. Search Clients (Current clients = HIGH risk if opposing)
            var clients = await _context.Clients
                .Where(c => 
                    c.Name.ToLower().Contains(query) ||
                    (c.Email != null && c.Email.ToLower().Contains(query)) ||
                    (c.Company != null && c.Company.ToLower().Contains(query)) ||
                    (c.ClientNumber != null && c.ClientNumber.ToLower().Contains(query)) ||
                    (c.TaxId != null && c.TaxId.ToLower().Contains(query)))
                .Select(c => new { c.Id, c.Name, c.Email, c.ClientNumber, c.Company, c.Status })
                .Take(20)
                .ToListAsync();

            foreach (var client in clients)
            {
                results.Add(new ConflictCheckResult
                {
                    Id = client.Id,
                    Name = client.Name,
                    Type = "Client",
                    Detail = client.ClientNumber ?? client.Email ?? client.Company,
                    Status = client.Status ?? "Active",
                    RiskLevel = "high",
                    ConflictReason = "Rule 1.7 - Current client. You cannot represent a party adverse to this client."
                });
            }

            // 2. Search Leads (Potential clients = MEDIUM risk)
            var leads = await _context.Leads
                .Where(l => l.Name.ToLower().Contains(query))
                .Select(l => new { l.Id, l.Name, l.Status, l.PracticeArea })
                .Take(20)
                .ToListAsync();

            foreach (var lead in leads)
            {
                results.Add(new ConflictCheckResult
                {
                    Id = lead.Id,
                    Name = lead.Name,
                    Type = "Lead",
                    Detail = lead.PracticeArea,
                    Status = lead.Status,
                    RiskLevel = "medium",
                    ConflictReason = "Potential client. If a consultation occurred, confidentiality duties may apply."
                });
            }

            // 3. Search Matters (Case names & numbers - for reference)
            var matters = await _context.Matters
                .Where(m => 
                    m.Name.ToLower().Contains(query) ||
                    (m.CaseNumber != null && m.CaseNumber.ToLower().Contains(query)))
                .Select(m => new { m.Id, m.Name, m.CaseNumber, m.Status })
                .Take(20)
                .ToListAsync();

            foreach (var matter in matters)
            {
                results.Add(new ConflictCheckResult
                {
                    Id = matter.Id,
                    Name = matter.Name,
                    Type = "Matter",
                    Detail = matter.CaseNumber,
                    Status = matter.Status.ToString(),
                    RiskLevel = "low",
                    ConflictReason = "Existing or prior matter. Rule 1.9 review may be required."
                });
            }

            // 4. Search Opposing Parties (Previous opponents = HIGH risk)
            var opposingParties = await _context.OpposingParties
                .Where(op => 
                    op.Name.ToLower().Contains(query) ||
                    (op.Company != null && op.Company.ToLower().Contains(query)) ||
                    (op.TaxId != null && op.TaxId.ToLower().Contains(query)) ||
                    (op.CounselName != null && op.CounselName.ToLower().Contains(query)) ||
                    (op.CounselFirm != null && op.CounselFirm.ToLower().Contains(query)))
                .Select(op => new { op.Id, op.Name, op.Company, op.Type, op.MatterId })
                .Take(20)
                .ToListAsync();

            foreach (var op in opposingParties)
            {
                results.Add(new ConflictCheckResult
                {
                    Id = op.Id,
                    Name = op.Name,
                    Type = "OpposingParty",
                    Detail = op.Company ?? op.Type,
                    Status = "Previous Opposing Party",
                    RiskLevel = "high",
                    ConflictReason = "Rule 1.9 - Former opposing party. You may be conflicted against this party."
                });
            }

            return Ok(results);
        }
    }

    /// <summary>
    /// Result object for conflict check search
    /// </summary>
    public class ConflictCheckResult
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Client, Lead, Matter, OpposingParty
        public string? Detail { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "low"; // high, medium, low
        public string? ConflictReason { get; set; }
    }
}

