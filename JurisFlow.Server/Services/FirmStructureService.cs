using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;

namespace JurisFlow.Server.Services
{
    public class FirmStructureService
    {
        private readonly JurisFlowDbContext _context;

        public FirmStructureService(JurisFlowDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetDefaultEntityIdAsync()
        {
            return await _context.FirmEntities
                .OrderByDescending(e => e.IsDefault)
                .ThenByDescending(e => e.IsActive)
                .ThenBy(e => e.Name)
                .Select(e => e.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<string?> GetDefaultOfficeIdAsync(string? entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return await _context.Offices
                    .OrderByDescending(o => o.IsDefault)
                    .ThenByDescending(o => o.IsActive)
                    .ThenBy(o => o.Name)
                    .Select(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            return await _context.Offices
                .Where(o => o.EntityId == entityId)
                .OrderByDescending(o => o.IsDefault)
                .ThenByDescending(o => o.IsActive)
                .ThenBy(o => o.Name)
                .Select(o => o.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<(string? entityId, string? officeId)> ResolveEntityOfficeAsync(string? entityId, string? officeId)
        {
            var resolvedEntityId = string.IsNullOrWhiteSpace(entityId) ? await GetDefaultEntityIdAsync() : entityId;
            var resolvedOfficeId = string.IsNullOrWhiteSpace(officeId) ? await GetDefaultOfficeIdAsync(resolvedEntityId) : officeId;
            return (resolvedEntityId, resolvedOfficeId);
        }

        public async Task<(string? entityId, string? officeId)> ResolveEntityOfficeFromMatterAsync(string? matterId, string? entityId, string? officeId)
        {
            if (!string.IsNullOrWhiteSpace(matterId))
            {
                var matter = await _context.Matters
                    .Where(m => m.Id == matterId)
                    .Select(m => new { m.EntityId, m.OfficeId })
                    .FirstOrDefaultAsync();
                if (matter != null)
                {
                    entityId ??= matter.EntityId;
                    officeId ??= matter.OfficeId;
                }
            }

            return await ResolveEntityOfficeAsync(entityId, officeId);
        }
    }
}
