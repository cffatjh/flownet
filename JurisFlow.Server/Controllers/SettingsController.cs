using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;
using System.Text.Json;

namespace JurisFlow.Server.Controllers
{
    [Route("api/settings")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public SettingsController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        [HttpGet("billing")]
        public async Task<IActionResult> GetBillingSettings()
        {
            var settings = await GetOrCreateBillingSettingsAsync();
            return Ok(settings);
        }

        [HttpPut("billing")]
        public async Task<IActionResult> UpdateBillingSettings([FromBody] BillingSettings dto)
        {
            var settings = await GetOrCreateBillingSettingsAsync();

            settings.DefaultHourlyRate = dto.DefaultHourlyRate;
            settings.PartnerRate = dto.PartnerRate;
            settings.AssociateRate = dto.AssociateRate;
            settings.ParalegalRate = dto.ParalegalRate;
            settings.BillingIncrement = dto.BillingIncrement;
            settings.MinimumTimeEntry = dto.MinimumTimeEntry;
            settings.RoundingRule = dto.RoundingRule;
            settings.DefaultPaymentTerms = dto.DefaultPaymentTerms;
            settings.InvoicePrefix = dto.InvoicePrefix;
            settings.DefaultTaxRate = dto.DefaultTaxRate;
            settings.LedesEnabled = dto.LedesEnabled;
            settings.UtbmsCodesRequired = dto.UtbmsCodesRequired;
            settings.EvergreenRetainerMinimum = dto.EvergreenRetainerMinimum;
            settings.TrustBalanceAlerts = dto.TrustBalanceAlerts;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "settings.billing.update", "BillingSettings", settings.Id, "Billing settings updated");

            return Ok(settings);
        }

        [HttpGet("firm")]
        public async Task<IActionResult> GetFirmSettings()
        {
            var settings = await GetOrCreateFirmSettingsAsync();
            return Ok(settings);
        }

        [HttpPut("firm")]
        public async Task<IActionResult> UpdateFirmSettings([FromBody] FirmSettings dto)
        {
            var settings = await GetOrCreateFirmSettingsAsync();

            settings.FirmName = dto.FirmName;
            settings.TaxId = dto.TaxId;
            settings.LedesFirmId = dto.LedesFirmId;
            settings.Address = dto.Address;
            settings.City = dto.City;
            settings.State = dto.State;
            settings.ZipCode = dto.ZipCode;
            settings.Phone = dto.Phone;
            settings.Website = dto.Website;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "settings.firm.update", "FirmSettings", settings.Id, "Firm settings updated");

            return Ok(settings);
        }

        [HttpGet("integrations")]
        public async Task<IActionResult> GetIntegrations()
        {
            var settings = await GetOrCreateFirmSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.IntegrationsJson))
            {
                return Ok(new List<IntegrationItemDto>());
            }

            try
            {
                var items = JsonSerializer.Deserialize<List<IntegrationItemDto>>(
                    settings.IntegrationsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<IntegrationItemDto>();
                return Ok(items);
            }
            catch
            {
                return Ok(new List<IntegrationItemDto>());
            }
        }

        [HttpPut("integrations")]
        public async Task<IActionResult> UpdateIntegrations([FromBody] IntegrationsUpdateDto dto)
        {
            var settings = await GetOrCreateFirmSettingsAsync();
            var items = dto?.Items ?? new List<IntegrationItemDto>();

            var normalized = items
                .Where(i => !string.IsNullOrWhiteSpace(i.Provider) && !string.IsNullOrWhiteSpace(i.Category))
                .Select(i => new IntegrationItemDto
                {
                    Id = string.IsNullOrWhiteSpace(i.Id) ? Guid.NewGuid().ToString() : i.Id,
                    Provider = i.Provider.Trim(),
                    Category = i.Category.Trim(),
                    Status = string.IsNullOrWhiteSpace(i.Status) ? "connected" : i.Status.Trim(),
                    AccountLabel = i.AccountLabel,
                    AccountEmail = i.AccountEmail,
                    SyncEnabled = i.SyncEnabled,
                    LastSyncAt = i.LastSyncAt,
                    Notes = i.Notes
                })
                .ToList();

            settings.IntegrationsJson = JsonSerializer.Serialize(normalized);
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "settings.integrations.update", "FirmSettings", settings.Id, "Integration settings updated");

            return Ok(normalized);
        }

        private async Task<BillingSettings> GetOrCreateBillingSettingsAsync()
        {
            var settings = await _context.BillingSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new BillingSettings();
                _context.BillingSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        private async Task<FirmSettings> GetOrCreateFirmSettingsAsync()
        {
            var settings = await _context.FirmSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new FirmSettings();
                _context.FirmSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        public class IntegrationItemDto
        {
            public string? Id { get; set; }
            public string Provider { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Status { get; set; } = "connected";
            public string? AccountLabel { get; set; }
            public string? AccountEmail { get; set; }
            public bool SyncEnabled { get; set; } = true;
            public DateTime? LastSyncAt { get; set; }
            public string? Notes { get; set; }
        }

        public class IntegrationsUpdateDto
        {
            public List<IntegrationItemDto> Items { get; set; } = new();
        }
    }
}
