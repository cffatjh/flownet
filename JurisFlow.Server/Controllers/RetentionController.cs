using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/admin/retention")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RetentionController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly RetentionService _retentionService;
        private readonly AuditLogger _auditLogger;

        public RetentionController(JurisFlowDbContext context, RetentionService retentionService, AuditLogger auditLogger)
        {
            _context = context;
            _retentionService = retentionService;
            _auditLogger = auditLogger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicies()
        {
            var policies = await _context.RetentionPolicies
                .OrderBy(p => p.EntityName)
                .ToListAsync();
            return Ok(policies);
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePolicies([FromBody] List<RetentionPolicyUpdateDto> updates)
        {
            if (updates == null || updates.Count == 0)
            {
                return BadRequest(new { message = "No retention policies provided." });
            }

            foreach (var update in updates)
            {
                var entityName = update.EntityName?.Trim();
                if (string.IsNullOrEmpty(entityName)) continue;

                var policy = await _context.RetentionPolicies.FirstOrDefaultAsync(p => p.EntityName == entityName);
                if (policy == null)
                {
                    policy = new RetentionPolicy
                    {
                        EntityName = entityName,
                        RetentionDays = Math.Max(1, update.RetentionDays),
                        IsActive = update.IsActive
                    };
                    _context.RetentionPolicies.Add(policy);
                }
                else
                {
                    policy.RetentionDays = Math.Max(1, update.RetentionDays);
                    policy.IsActive = update.IsActive;
                    policy.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "admin.retention.update", "RetentionPolicy", null, "Retention policies updated");

            return Ok(await _context.RetentionPolicies.OrderBy(p => p.EntityName).ToListAsync());
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunRetention()
        {
            var results = await _retentionService.ApplyRetentionAsync();
            await _auditLogger.LogAsync(HttpContext, "admin.retention.run", "RetentionPolicy", null, "Retention run executed");
            return Ok(new { results });
        }
    }

    public class RetentionPolicyUpdateDto
    {
        public string EntityName { get; set; } = string.Empty;
        public int RetentionDays { get; set; } = 365;
        public bool IsActive { get; set; } = true;
    }
}
