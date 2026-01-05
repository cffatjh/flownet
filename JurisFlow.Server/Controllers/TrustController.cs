using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using JurisFlow.Server.Services;
using Task = System.Threading.Tasks.Task;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrustController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly AuditLogger _auditLogger;

        public TrustController(JurisFlowDbContext context, AuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        private async Task<bool> IsPeriodLocked(DateTime date)
        {
            var key = date.ToString("yyyy-MM-dd");
            return await _context.BillingLocks.AnyAsync(b => string.Compare(key, b.PeriodStart) >= 0 && string.Compare(key, b.PeriodEnd) <= 0);
        }

        // --- ACCOUNTS ---

        [HttpGet("accounts")]
        public async Task<ActionResult<IEnumerable<TrustBankAccount>>> GetTrustAccounts()
        {
            return await _context.TrustBankAccounts.ToListAsync();
        }

        [HttpPost("accounts")]
        public async Task<ActionResult<TrustBankAccount>> CreateTrustAccount(TrustBankAccount account)
        {
            account.Id = Guid.NewGuid().ToString();
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            _context.TrustBankAccounts.Add(account);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "trust.account.create", "TrustBankAccount", account.Id, $"Name={account.Name}, Balance={account.CurrentBalance}");
            return CreatedAtAction(nameof(GetTrustAccounts), new { id = account.Id }, account);
        }

        // --- LEDGERS ---

        [HttpGet("ledgers")]
        public async Task<ActionResult<IEnumerable<ClientTrustLedger>>> GetLedgers()
        {
            return await _context.ClientTrustLedgers
                .Include(l => l.Client)
                .Include(l => l.TrustAccount)
                .ToListAsync();
        }

        [HttpPost("ledgers")]
        public async Task<ActionResult<ClientTrustLedger>> CreateLedger(ClientTrustLedger ledger)
        {
            ledger.Id = Guid.NewGuid().ToString();
            ledger.CreatedAt = DateTime.UtcNow;
            ledger.UpdatedAt = DateTime.UtcNow;
            _context.ClientTrustLedgers.Add(ledger);
            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "trust.ledger.create", "ClientTrustLedger", ledger.Id, $"ClientId={ledger.ClientId}, Account={ledger.TrustAccountId}");
            return CreatedAtAction(nameof(GetLedgers), new { id = ledger.Id }, ledger);
        }

        // --- TRANSACTIONS ---

        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<TrustTransaction>>> GetTransactions([FromQuery] int limit = 50)
        {
            return await _context.TrustTransactions
                .Include(t => t.Matter)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        [HttpPost("deposit")]
        public async Task<ActionResult<TrustTransaction>> Deposit(DepositRequest request)
        {
            // 1. Update Trust Account Balance
            var account = await _context.TrustBankAccounts.FindAsync(request.TrustAccountId);
            if (account == null) return NotFound("Trust account not found");
            
            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest("Billing period is locked. Cannot post deposit.");
            }

            if (request.Amount <= 0) return BadRequest("Deposit amount must be positive");
            
            account.CurrentBalance += request.Amount;
            
            // 2. Create Transaction Record
            var tx = new TrustTransaction
            {
                Id = Guid.NewGuid().ToString(),
                MatterId = "N/A", // This should be handled better, maybe null allowed
                Type = "DEPOSIT",
                Amount = request.Amount,
                Description = request.Description,
                BalanceAfter = account.CurrentBalance,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.TrustTransactions.Add(tx);

            // 3. Update Client Ledgers
            if (request.Allocations != null) {
                foreach (var alloc in request.Allocations)
                {
                    var ledger = await _context.ClientTrustLedgers.FindAsync(alloc.LedgerId);
                    if (ledger != null)
                    {
                        ledger.RunningBalance += alloc.Amount;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "trust.deposit", "TrustTransaction", tx.Id, $"Amount={request.Amount}, Account={account.Id}");
            return Ok(tx);
        }

        [HttpPost("withdrawal")]
        public async Task<ActionResult<TrustTransaction>> Withdrawal(WithdrawalRequest request)
        {
             var account = await _context.TrustBankAccounts.FindAsync(request.TrustAccountId);
            if (account == null) return NotFound("Trust account not found");
            
            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest("Billing period is locked. Cannot post withdrawal.");
            }

            if (request.Amount <= 0) return BadRequest("Withdrawal amount must be positive");
            if (account.CurrentBalance - request.Amount < 0)
            {
                return BadRequest("Insufficient trust account balance");
            }

            account.CurrentBalance -= request.Amount;

            var tx = new TrustTransaction
            {
                Id = Guid.NewGuid().ToString(),
                MatterId = "N/A",
                Type = "WITHDRAWAL",
                Amount = request.Amount,
                Description = request.Description,
                BalanceAfter = account.CurrentBalance,
                CreatedAt = DateTime.UtcNow
            };
             _context.TrustTransactions.Add(tx);

             if (!string.IsNullOrEmpty(request.LedgerId))
             {
                 var ledger = await _context.ClientTrustLedgers.FindAsync(request.LedgerId);
                 if (ledger != null)
                 {
                     if (ledger.RunningBalance - request.Amount < 0)
                     {
                         return BadRequest("Insufficient client ledger balance");
                     }
                     ledger.RunningBalance -= request.Amount;
                 }
             }

            await _context.SaveChangesAsync();
            await _auditLogger.LogAsync(HttpContext, "trust.withdrawal", "TrustTransaction", tx.Id, $"Amount={request.Amount}, Account={account.Id}, Ledger={request.LedgerId}");
            return Ok(tx);
        }

        // --- RECONCILIATIONS ---

        [HttpGet("reconciliations")]
        public async Task<ActionResult<IEnumerable<ReconciliationRecord>>> GetReconciliations()
        {
            return await _context.ReconciliationRecords.Include(r => r.TrustAccount).OrderByDescending(r => r.PeriodEnd).ToListAsync();
        }

        [HttpPost("reconcile")]
        public async Task<ActionResult<ReconciliationRecord>> Reconcile(ReconcileRequest request)
        {
            var account = await _context.TrustBankAccounts.FindAsync(request.TrustAccountId);
            if (account == null) return NotFound("Trust account not found");

            if (await IsPeriodLocked(DateTime.UtcNow))
            {
                return BadRequest("Billing period is locked. Cannot reconcile in locked period.");
            }

            var clientLedgerSum = await _context.ClientTrustLedgers
                .Where(l => l.TrustAccountId == request.TrustAccountId)
                .SumAsync(l => l.RunningBalance);

            var discrepancy = Math.Abs(account.CurrentBalance - request.BankStatementBalance);
            bool isReconciled = discrepancy < 0.01 && Math.Abs(clientLedgerSum - account.CurrentBalance) < 0.01;

            var rec = new ReconciliationRecord
            {
                Id = Guid.NewGuid().ToString(),
                TrustAccountId = request.TrustAccountId,
                PeriodEnd = DateTime.Parse(request.PeriodEnd),
                BankStatementBalance = request.BankStatementBalance,
                TrustLedgerBalance = account.CurrentBalance,
                ClientLedgerSumBalance = clientLedgerSum,
                IsReconciled = isReconciled,
                DiscrepancyAmount = discrepancy,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReconciliationRecords.Add(rec);
            await _context.SaveChangesAsync();

            await _auditLogger.LogAsync(HttpContext, "trust.reconcile", "ReconciliationRecord", rec.Id, $"IsReconciled={isReconciled}, Discrepancy={discrepancy}");

            return Ok(rec);
        }
    }

    // DTOs
    public class DepositRequest
    {
        public string TrustAccountId { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }
        public string PayorPayee { get; set; }
        public string? CheckNumber { get; set; }
        public List<AllocationDto> Allocations { get; set; }
    }

    public class AllocationDto
    {
        public string LedgerId { get; set; }
        public double Amount { get; set; }
    }

    public class WithdrawalRequest
    {
        public string TrustAccountId { get; set; }
        public string LedgerId { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }
        public string PayorPayee { get; set; }
        public string? CheckNumber { get; set; }
    }

    public class ReconcileRequest
    {
        public string TrustAccountId { get; set; }
        public string PeriodEnd { get; set; }
        public double BankStatementBalance { get; set; }
        public string Notes { get; set; }
    }
}
