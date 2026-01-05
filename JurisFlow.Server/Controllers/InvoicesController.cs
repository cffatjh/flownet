using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Enums;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;
using System.Text;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public InvoicesController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        private async Task<bool> IsPeriodLocked(DateTime date)
        {
            var key = date.ToString("yyyy-MM-dd");
            return await _context.BillingLocks.AnyAsync(b => string.Compare(key, b.PeriodStart) >= 0 && string.Compare(key, b.PeriodEnd) <= 0);
        }

        private static void RecalculateTotals(Invoice invoice)
        {
            var subtotal = invoice.LineItems.Sum(li => li.Amount);
            var total = subtotal + invoice.Tax - invoice.Discount;
            invoice.Subtotal = subtotal;
            invoice.Total = total;
            invoice.Balance = total - invoice.AmountPaid;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<IActionResult> GetInvoices()
        {
            var invoices = await _context.Invoices
                .Include(i => i.LineItems)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
            return Ok(invoices);
        }

        // GET: api/Invoices/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(string id)
        {
            var invoice = await _context.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        // POST: api/Invoices
        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto dto)
        {
            if (await IsPeriodLocked(dto.IssueDate ?? DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot create invoice." });
            }

            var invoice = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                Number = dto.Number,
                ClientId = dto.ClientId,
                MatterId = dto.MatterId,
                Status = dto.Status ?? InvoiceStatus.Draft,
                IssueDate = dto.IssueDate ?? DateTime.UtcNow,
                DueDate = dto.DueDate,
                Notes = dto.Notes,
                Terms = dto.Terms,
                Discount = dto.Discount ?? 0,
                Tax = dto.Tax ?? 0,
                AmountPaid = 0
            };

            if (dto.LineItems != null)
            {
                foreach (var li in dto.LineItems)
                {
                    invoice.LineItems.Add(new InvoiceLineItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = li.Type ?? "time",
                        Description = li.Description ?? string.Empty,
                        Quantity = li.Quantity ?? 1,
                        Rate = li.Rate ?? 0,
                        Amount = (li.Quantity ?? 1) * (li.Rate ?? 0),
                        TaskCode = li.TaskCode,
                        ExpenseCode = li.ExpenseCode,
                        ActivityCode = li.ActivityCode,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            RecalculateTotals(invoice);

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "invoice.create", "Invoice", invoice.Id, $"Client={invoice.ClientId}, Total={invoice.Total}");

            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }

        // PUT: api/Invoices/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(string id, [FromBody] InvoiceUpdateDto dto)
        {
            var invoice = await _context.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot update invoice." });
            }

            if (!string.IsNullOrWhiteSpace(dto.Number)) invoice.Number = dto.Number;
            if (!string.IsNullOrWhiteSpace(dto.ClientId)) invoice.ClientId = dto.ClientId;
            if (!string.IsNullOrWhiteSpace(dto.MatterId)) invoice.MatterId = dto.MatterId;
            if (dto.Status.HasValue) invoice.Status = dto.Status.Value;
            if (dto.IssueDate.HasValue) invoice.IssueDate = dto.IssueDate.Value;
            if (dto.DueDate.HasValue) invoice.DueDate = dto.DueDate.Value;
            if (dto.Notes != null) invoice.Notes = dto.Notes;
            if (dto.Terms != null) invoice.Terms = dto.Terms;
            if (dto.Discount is not null) invoice.Discount = dto.Discount.Value;
            if (dto.Tax is not null) invoice.Tax = dto.Tax.Value;

            // Replace line items if provided
            if (dto.LineItems != null)
            {
                _context.InvoiceLineItems.RemoveRange(invoice.LineItems);
                invoice.LineItems.Clear();
                foreach (var li in dto.LineItems)
                {
                    invoice.LineItems.Add(new InvoiceLineItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        InvoiceId = invoice.Id,
                        Type = li.Type ?? "time",
                        Description = li.Description ?? string.Empty,
                        Quantity = li.Quantity ?? 1,
                        Rate = li.Rate ?? 0,
                        Amount = (li.Quantity ?? 1) * (li.Rate ?? 0),
                        TaskCode = li.TaskCode,
                        ExpenseCode = li.ExpenseCode,
                        ActivityCode = li.ActivityCode,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            RecalculateTotals(invoice);
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "invoice.update", "Invoice", invoice.Id, $"Status={invoice.Status}, Total={invoice.Total}");

            return Ok(invoice);
        }

        // POST: api/Invoices/{id}/pay
        [HttpPost("{id}/pay")]
        public async Task<IActionResult> ApplyPayment(string id, [FromBody] InvoicePaymentDto dto)
        {
            var invoice = await _context.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot apply payment." });
            }

            var amount = dto.Amount;
            if (amount <= 0) return BadRequest(new { message = "Amount must be positive" });

            invoice.AmountPaid += amount;
            invoice.Balance -= amount;
            if (invoice.Balance < 0) invoice.Balance = 0;

            if (invoice.Balance == 0)
            {
                invoice.Status = InvoiceStatus.Paid;
            }
            else if (invoice.Status == InvoiceStatus.Sent || invoice.Status == InvoiceStatus.Approved)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "invoice.payment.apply", "Invoice", invoice.Id, $"Amount={amount}, Balance={invoice.Balance}");

            return Ok(invoice);
        }

        // GET: api/Invoices/{id}/ledes
        [HttpGet("{id}/ledes")]
        public async Task<IActionResult> ExportLedes(string id)
        {
            var invoice = await _context.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            // Simple LEDES 1998B-like export (stub) - commas replaced with pipes to keep safe
            var sb = new StringBuilder();
            sb.AppendLine("INVOICE_DATE,INVOICE_NUMBER,CLIENT_ID,MATTER_ID,INVOICE_TOTAL");
            sb.AppendLine($"{invoice.IssueDate:yyyyMMdd},{invoice.Number},{invoice.ClientId},{invoice.MatterId},{invoice.Total:F2}");
            sb.AppendLine("LINE_ITEM_NUMBER,LINE_ITEM_DATE,TASK_CODE,EXPENSE_CODE,ACTIVITY_CODE,LINE_ITEM_DESCRIPTION,LINE_ITEM_UNIT_COST,LINE_ITEM_UNITS,LINE_ITEM_FEE");

            int lineNo = 1;
            foreach (var li in invoice.LineItems)
            {
                var desc = (li.Description ?? string.Empty).Replace(",", ";");
                var units = li.Quantity;
                var unitCost = li.Rate;
                var fee = li.Amount;
                sb.AppendLine($"{lineNo},{invoice.IssueDate:yyyyMMdd},{li.TaskCode},{li.ExpenseCode},{li.ActivityCode},{desc},{unitCost:F2},{units:F2},{fee:F2}");
                lineNo++;
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/plain", $"invoice_{invoice.Number ?? invoice.Id}_ledes.txt");
        }

        // POST: api/Invoices/{id}/write-off
        [HttpPost("{id}/write-off")]
        public async Task<IActionResult> WriteOff(string id, [FromBody] InvoiceWriteOffDto dto)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot write off invoice." });
            }

            invoice.Status = InvoiceStatus.WrittenOff;
            invoice.Balance = 0;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "invoice.writeoff", "Invoice", invoice.Id, $"Reason={dto.Reason}");

            return Ok(invoice);
        }

        // POST: api/Invoices/{id}/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(string id, [FromBody] InvoiceCancelDto dto)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NotFound();

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot cancel invoice." });
            }

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.Balance = 0;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "invoice.cancel", "Invoice", invoice.Id, $"Reason={dto.Reason}");

            return Ok(invoice);
        }

        // DELETE: api/Invoices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(string id)
        {
            var invoice = await _context.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) return NoContent();

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot delete invoice." });
            }

            _context.InvoiceLineItems.RemoveRange(invoice.LineItems);
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "invoice.delete", "Invoice", id, "Deleted invoice");

            return NoContent();
        }
    }

    // DTOs
    public class InvoiceLineItemDto
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public double? Quantity { get; set; }
        public double? Rate { get; set; }
        public string? TaskCode { get; set; }
        public string? ExpenseCode { get; set; }
        public string? ActivityCode { get; set; }
    }

    public class InvoiceCreateDto
    {
        public string? Number { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string? MatterId { get; set; }
        public InvoiceStatus? Status { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? DueDate { get; set; }
        public double? Tax { get; set; }
        public double? Discount { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public List<InvoiceLineItemDto>? LineItems { get; set; }
    }

    public class InvoiceUpdateDto : InvoiceCreateDto
    {
    }

    public class InvoicePaymentDto
    {
        public double Amount { get; set; }
        public string? Reference { get; set; }
    }

    public class InvoiceWriteOffDto
    {
        public string? Reason { get; set; }
    }

    public class InvoiceCancelDto
    {
        public string? Reason { get; set; }
    }
}
