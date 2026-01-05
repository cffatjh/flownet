using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace JurisFlow.Server.Controllers
{
    [Route("api/court-rules")]
    [ApiController]
    [Authorize]
    public class CourtRulesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;

        public CourtRulesController(JurisFlowDbContext context)
        {
            _context = context;
        }

        // GET: api/court-rules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourtRule>>> GetCourtRules(
            [FromQuery] string? jurisdiction = null,
            [FromQuery] string? ruleType = null,
            [FromQuery] string? triggerEvent = null)
        {
            var query = _context.CourtRules.Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(jurisdiction))
            {
                query = query.Where(r => r.Jurisdiction == jurisdiction);
            }

            if (!string.IsNullOrEmpty(ruleType))
            {
                query = query.Where(r => r.RuleType == ruleType);
            }

            if (!string.IsNullOrEmpty(triggerEvent))
            {
                query = query.Where(r => r.TriggerEvent.Contains(triggerEvent));
            }

            var rules = await query
                .OrderBy(r => r.Jurisdiction)
                .ThenBy(r => r.TriggerEvent)
                .ToListAsync();

            return Ok(rules);
        }

        // GET: api/court-rules/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CourtRule>> GetCourtRule(string id)
        {
            var rule = await _context.CourtRules.FindAsync(id);
            if (rule == null)
            {
                return NotFound();
            }

            return Ok(rule);
        }

        // GET: api/court-rules/jurisdictions
        [HttpGet("jurisdictions")]
        public async Task<ActionResult<IEnumerable<string>>> GetJurisdictions()
        {
            var jurisdictions = await _context.CourtRules
                .Where(r => r.IsActive)
                .Select(r => r.Jurisdiction)
                .Distinct()
                .OrderBy(j => j)
                .ToListAsync();

            return Ok(jurisdictions);
        }

        // GET: api/court-rules/trigger-events
        [HttpGet("trigger-events")]
        public async Task<ActionResult<IEnumerable<string>>> GetTriggerEvents([FromQuery] string? jurisdiction = null)
        {
            var query = _context.CourtRules.Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(jurisdiction))
            {
                query = query.Where(r => r.Jurisdiction == jurisdiction);
            }

            var events = await query
                .Select(r => r.TriggerEvent)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();

            return Ok(events);
        }

        // POST: api/court-rules
        [HttpPost]
        public async Task<ActionResult<CourtRule>> CreateCourtRule([FromBody] CourtRule rule)
        {
            rule.Id = Guid.NewGuid().ToString();
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;

            _context.CourtRules.Add(rule);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCourtRule), new { id = rule.Id }, rule);
        }

        // PUT: api/court-rules/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourtRule(string id, [FromBody] CourtRule updatedRule)
        {
            var rule = await _context.CourtRules.FindAsync(id);
            if (rule == null)
            {
                return NotFound();
            }

            rule.Name = updatedRule.Name;
            rule.RuleType = updatedRule.RuleType;
            rule.Jurisdiction = updatedRule.Jurisdiction;
            rule.CourtType = updatedRule.CourtType;
            rule.Citation = updatedRule.Citation;
            rule.TriggerEvent = updatedRule.TriggerEvent;
            rule.DaysCount = updatedRule.DaysCount;
            rule.DayType = updatedRule.DayType;
            rule.Direction = updatedRule.Direction;
            rule.ServiceDaysAdd = updatedRule.ServiceDaysAdd;
            rule.Description = updatedRule.Description;
            rule.ExtendIfWeekend = updatedRule.ExtendIfWeekend;
            rule.IsActive = updatedRule.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(rule);
        }

        // DELETE: api/court-rules/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourtRule(string id)
        {
            var rule = await _context.CourtRules.FindAsync(id);
            if (rule == null)
            {
                return NotFound();
            }

            // Soft delete
            rule.IsActive = false;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/court-rules/seed
        [HttpPost("seed")]
        public async Task<IActionResult> SeedDefaultRules()
        {
            // Check if already seeded
            if (await _context.CourtRules.AnyAsync())
            {
                return BadRequest(new { message = "Rules already exist. Delete existing rules first." });
            }

            var defaultRules = GetDefaultRules();

            _context.CourtRules.AddRange(defaultRules);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Seeded {defaultRules.Count} court rules", count = defaultRules.Count });
        }

        private List<CourtRule> GetDefaultRules()
        {
            return new List<CourtRule>
            {
                // Federal Rules
                new CourtRule
                {
                    Name = "Answer to Complaint",
                    RuleType = "Federal",
                    Jurisdiction = "FRCP",
                    Citation = "FRCP Rule 12(a)(1)(A)(i)",
                    TriggerEvent = "Service of Summons and Complaint",
                    DaysCount = 21,
                    DayType = "Calendar",
                    Direction = "After",
                    Description = "Defendant must answer within 21 days after service"
                },
                new CourtRule
                {
                    Name = "Motion Response",
                    RuleType = "Federal",
                    Jurisdiction = "FRCP",
                    Citation = "Local Rules",
                    TriggerEvent = "Motion Filing",
                    DaysCount = 14,
                    DayType = "Calendar",
                    Direction = "After",
                    Description = "Opposition to motion due 14 days after filing"
                },
                new CourtRule
                {
                    Name = "Reply Brief",
                    RuleType = "Federal",
                    Jurisdiction = "FRCP",
                    Citation = "Local Rules",
                    TriggerEvent = "Opposition Filing",
                    DaysCount = 7,
                    DayType = "Calendar",
                    Direction = "After",
                    Description = "Reply brief due 7 days after opposition"
                },

                // California State Rules
                new CourtRule
                {
                    Name = "Answer to Complaint",
                    RuleType = "State",
                    Jurisdiction = "CA",
                    CourtType = "Superior",
                    Citation = "CCP § 412.20",
                    TriggerEvent = "Service of Summons and Complaint",
                    DaysCount = 30,
                    DayType = "Calendar",
                    Direction = "After",
                    Description = "30 days to file answer in California"
                },
                new CourtRule
                {
                    Name = "Motion Hearing Notice",
                    RuleType = "State",
                    Jurisdiction = "CA",
                    CourtType = "Superior",
                    Citation = "CCP § 1005(b)",
                    TriggerEvent = "Motion Hearing Date",
                    DaysCount = 16,
                    DayType = "Court",
                    Direction = "Before",
                    ServiceDaysAdd = 5,
                    Description = "Motion must be filed 16 court days before hearing, +5 for mail service"
                },
                new CourtRule
                {
                    Name = "Opposition to Motion",
                    RuleType = "State",
                    Jurisdiction = "CA",
                    CourtType = "Superior",
                    Citation = "CCP § 1005(b)",
                    TriggerEvent = "Motion Hearing Date",
                    DaysCount = 9,
                    DayType = "Court",
                    Direction = "Before",
                    Description = "Opposition due 9 court days before hearing"
                },
                new CourtRule
                {
                    Name = "Reply to Opposition",
                    RuleType = "State",
                    Jurisdiction = "CA",
                    CourtType = "Superior",
                    Citation = "CCP § 1005(b)",
                    TriggerEvent = "Motion Hearing Date",
                    DaysCount = 5,
                    DayType = "Court",
                    Direction = "Before",
                    Description = "Reply due 5 court days before hearing"
                },

                // New York State Rules
                new CourtRule
                {
                    Name = "Answer to Complaint",
                    RuleType = "State",
                    Jurisdiction = "NY",
                    CourtType = "Supreme",
                    Citation = "CPLR § 320(a)",
                    TriggerEvent = "Service of Summons and Complaint",
                    DaysCount = 20,
                    DayType = "Calendar",
                    Direction = "After",
                    ServiceDaysAdd = 5,
                    Description = "20 days to answer if personally served, +5 if by mail"
                },
                new CourtRule
                {
                    Name = "Motion Notice",
                    RuleType = "State",
                    Jurisdiction = "NY",
                    CourtType = "Supreme",
                    Citation = "CPLR § 2214(b)",
                    TriggerEvent = "Motion Hearing Date",
                    DaysCount = 8,
                    DayType = "Calendar",
                    Direction = "Before",
                    Description = "Motion papers must be served at least 8 days before return date"
                },

                // Texas State Rules
                new CourtRule
                {
                    Name = "Answer to Petition",
                    RuleType = "State",
                    Jurisdiction = "TX",
                    CourtType = "District",
                    Citation = "TRCP Rule 99",
                    TriggerEvent = "Service of Citation",
                    DaysCount = 20,
                    DayType = "Calendar",
                    Direction = "After",
                    ExtendIfWeekend = true,
                    Description = "Answer due by 10:00 AM on the Monday following 20 days"
                }
            };
        }
    }
}
