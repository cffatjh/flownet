using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuditLogger _auditLogger;

        public PaymentsController(JurisFlowDbContext context, IConfiguration configuration, AuditLogger auditLogger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogger = auditLogger;
        }

        private async Task<bool> IsPeriodLocked(DateTime date)
        {
            var dateKey = date.ToString("yyyy-MM-dd");
            return await _context.BillingLocks.AnyAsync(b => string.Compare(dateKey, b.PeriodStart) >= 0 && string.Compare(dateKey, b.PeriodEnd) <= 0);
        }

        // POST: api/payments/create-checkout
        [HttpPost("create-checkout")]
        public async Task<ActionResult> CreateCheckoutSession([FromBody] CreateCheckoutDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Locked period guard
            var txnDate = DateTime.UtcNow;
            if (await IsPeriodLocked(txnDate))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot create checkout in a locked period." });
            }

            // Surcharge guard: do not allow charging above invoice balance if invoiceId provided
            if (!string.IsNullOrEmpty(dto.InvoiceId))
            {
                var invoice = await _context.Invoices.FindAsync(dto.InvoiceId);
                if (invoice != null && dto.Amount > invoice.Balance + 0.01)
                {
                    return BadRequest(new { message = "Charge amount exceeds invoice balance. Surcharges are not allowed." });
                }
            }

            // Create payment transaction record
            var transaction = new PaymentTransaction
            {
                InvoiceId = dto.InvoiceId,
                MatterId = dto.MatterId,
                ClientId = dto.ClientId,
                Amount = dto.Amount,
                Currency = dto.Currency ?? "USD",
                TaskCode = dto.TaskCode,
                ExpenseCode = dto.ExpenseCode,
                ActivityCode = dto.ActivityCode,
                PaymentMethod = "Stripe",
                Status = "Pending",
                PayerEmail = dto.PayerEmail,
                PayerName = dto.PayerName,
                ProcessedBy = userId
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "payment.create_checkout", "PaymentTransaction", transaction.Id, $"Invoice={dto.InvoiceId}, Amount={dto.Amount} {transaction.Currency}");

            // TODO: Integrate with Stripe API
            // Create a Stripe Checkout Session and return the URL
            // For now, return a mock checkout URL
            var checkoutUrl = $"/payment/checkout/{transaction.Id}";

            return Ok(new
            {
                transactionId = transaction.Id,
                checkoutUrl = checkoutUrl,
                amount = dto.Amount,
                currency = transaction.Currency
            });
        }

        // GET: api/payments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentTransaction>> GetPayment(string id)
        {
            var transaction = await _context.PaymentTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }

        // GET: api/payments/invoice/{invoiceId}
        [HttpGet("invoice/{invoiceId}")]
        public async Task<ActionResult<IEnumerable<PaymentTransaction>>> GetInvoicePayments(string invoiceId)
        {
            var payments = await _context.PaymentTransactions
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/payments/matter/{matterId}
        [HttpGet("matter/{matterId}")]
        public async Task<ActionResult<IEnumerable<PaymentTransaction>>> GetMatterPayments(string matterId)
        {
            var payments = await _context.PaymentTransactions
                .Where(p => p.MatterId == matterId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/payments/client/{clientId}
        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<IEnumerable<PaymentTransaction>>> GetClientPayments(string clientId)
        {
            var payments = await _context.PaymentTransactions
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(payments);
        }

        // POST: api/payments/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompletePayment(string id, [FromBody] CompletePaymentDto dto)
        {
            var transaction = await _context.PaymentTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot modify payment." });
            }

            transaction.Status = "Succeeded";
            transaction.ExternalTransactionId = dto.ExternalTransactionId;
            transaction.CardLast4 = dto.CardLast4;
            transaction.CardBrand = dto.CardBrand;
            transaction.ReceiptUrl = dto.ReceiptUrl;
            transaction.ProcessedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "payment.complete", "PaymentTransaction", transaction.Id, $"ExternalId={dto.ExternalTransactionId}, Amount={transaction.Amount}");

            return Ok(new { message = "Payment completed", transactionId = transaction.Id });
        }

        // POST: api/payments/{id}/fail
        [HttpPost("{id}/fail")]
        public async Task<IActionResult> FailPayment(string id, [FromBody] FailPaymentDto dto)
        {
            var transaction = await _context.PaymentTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot modify payment." });
            }

            transaction.Status = "Failed";
            transaction.FailureReason = dto.Reason;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "payment.fail", "PaymentTransaction", transaction.Id, $"Reason={dto.Reason}");

            return Ok(new { message = "Payment failed", reason = dto.Reason });
        }

        // POST: api/payments/{id}/refund
        [HttpPost("{id}/refund")]
        public async Task<IActionResult> RefundPayment(string id, [FromBody] RefundPaymentDto dto)
        {
            var transaction = await _context.PaymentTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            if (transaction.Status != "Succeeded")
            {
                return BadRequest(new { message = "Can only refund completed payments" });
            }

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest(new { message = "Billing period is locked. Cannot refund in locked period." });
            }

            var refundAmount = dto.Amount ?? transaction.Amount;
            if (refundAmount > transaction.Amount)
            {
                return BadRequest(new { message = "Refund amount exceeds payment amount" });
            }

            // TODO: Process refund via Stripe API
            // Surcharge guard: ensure refunds never increase collected amount beyond net
            if (refundAmount < 0)
            {
                return BadRequest(new { message = "Refund amount must be positive" });
            }

            transaction.Status = refundAmount >= transaction.Amount ? "Refunded" : "Partially Refunded";
            transaction.RefundAmount = refundAmount;
            transaction.RefundReason = dto.Reason;
            transaction.RefundedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "payment.refund", "PaymentTransaction", transaction.Id, $"RefundAmount={refundAmount}, Reason={dto.Reason}");

            return Ok(new { message = "Refund processed", refundAmount = refundAmount });
        }

        // POST: api/payments/webhook (Stripe webhook endpoint)
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            // TODO: Implement Stripe webhook handling
            // Verify webhook signature
            // Parse event type
            // Update payment transaction status

            return Ok();
        }

        // GET: api/payments/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetPaymentStats([FromQuery] string? from = null, [FromQuery] string? to = null)
        {
            var query = _context.PaymentTransactions.AsQueryable();

            if (DateTime.TryParse(from, out var fromDate))
            {
                query = query.Where(p => p.CreatedAt >= fromDate);
            }

            if (DateTime.TryParse(to, out var toDate))
            {
                query = query.Where(p => p.CreatedAt <= toDate);
            }

            var stats = await query
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            var totalSucceeded = stats.Where(s => s.Status == "Succeeded").Sum(s => s.TotalAmount);
            var totalRefunded = stats.Where(s => s.Status == "Refunded" || s.Status == "Partially Refunded").Sum(s => s.TotalAmount);

            return Ok(new
            {
                stats,
                totalSucceeded,
                totalRefunded,
                netRevenue = totalSucceeded - totalRefunded
            });
        }
    }

    // DTOs
    public class CreateCheckoutDto
    {
        public string? InvoiceId { get; set; }
        public string? MatterId { get; set; }
        public string? ClientId { get; set; }
        public double Amount { get; set; }
        public string? Currency { get; set; }
        public string? PayerEmail { get; set; }
        public string? PayerName { get; set; }
        public string? TaskCode { get; set; }
        public string? ExpenseCode { get; set; }
        public string? ActivityCode { get; set; }
    }

    public class CompletePaymentDto
    {
        public string? ExternalTransactionId { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardBrand { get; set; }
        public string? ReceiptUrl { get; set; }
    }

    public class FailPaymentDto
    {
        public string? Reason { get; set; }
    }

    public class RefundPaymentDto
    {
        public double? Amount { get; set; }
        public string? Reason { get; set; }
    }
}
