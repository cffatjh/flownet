using JurisFlow.Server.Data;
using JurisFlow.Server.Enums;
using JurisFlow.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace JurisFlow.Server.Services
{
    public class PaymentPlanService
    {
        private readonly JurisFlowDbContext _context;

        public PaymentPlanService(JurisFlowDbContext context)
        {
            _context = context;
        }

        public DateTime GetNextRunDate(DateTime from, string frequency)
        {
            var normalized = (frequency ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "weekly" => from.AddDays(7),
                "biweekly" => from.AddDays(14),
                "monthly" => from.AddMonths(1),
                "quarterly" => from.AddMonths(3),
                _ => from.AddMonths(1)
            };
        }

        public async Task<PaymentTransaction?> RunPlanAsync(PaymentPlan plan, string? processedBy, string? payerEmail, string? payerName, DateTime? runAt = null)
        {
            if (!string.Equals(plan.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (plan.RemainingAmount <= 0)
            {
                plan.Status = "Completed";
                plan.AutoPayEnabled = false;
                plan.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return null;
            }

            var amount = Math.Min(plan.InstallmentAmount, plan.RemainingAmount);
            if (amount <= 0)
            {
                return null;
            }

            Invoice? invoice = null;
            if (!string.IsNullOrWhiteSpace(plan.InvoiceId))
            {
                invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == plan.InvoiceId);
            }

            var now = runAt ?? DateTime.UtcNow;
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceId = plan.InvoiceId,
                ClientId = plan.ClientId,
                Amount = amount,
                Currency = "USD",
                PaymentMethod = plan.AutoPayEnabled ? "AutoPay (Simulated)" : "Payment Plan",
                Status = "Succeeded",
                PayerEmail = payerEmail,
                PayerName = payerName,
                ProcessedBy = processedBy,
                ProcessedAt = now,
                ScheduledFor = plan.NextRunDate,
                PaymentPlanId = plan.Id,
                Source = plan.AutoPayEnabled ? "AutoPay" : "Plan",
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.PaymentTransactions.Add(transaction);

            if (invoice != null)
            {
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

                invoice.UpdatedAt = now;
            }

            plan.RemainingAmount -= amount;
            plan.UpdatedAt = now;
            if (plan.RemainingAmount <= 0)
            {
                plan.RemainingAmount = 0;
                plan.Status = "Completed";
                plan.AutoPayEnabled = false;
            }
            else
            {
                plan.NextRunDate = GetNextRunDate(plan.NextRunDate, plan.Frequency);
            }

            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}
