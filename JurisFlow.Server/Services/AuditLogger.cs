using System.Security.Claims;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using Task = System.Threading.Tasks.Task;

namespace JurisFlow.Server.Services
{
    public class AuditLogger
    {
        private readonly JurisFlowDbContext _context;

        public AuditLogger(JurisFlowDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(HttpContext httpContext, string action, string? entity = null, string? entityId = null, string? details = null)
        {
            try
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var clientId = httpContext.User.FindFirst("clientId")?.Value;
                var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? httpContext.User.FindFirst("role")?.Value;

                var audit = new AuditLog
                {
                    UserId = userId,
                    ClientId = clientId,
                    Role = role,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Swallow logging failures to avoid breaking primary flow
            }
        }
    }
}
