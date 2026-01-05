using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JurisFlow.Server.Models
{
    public class ReconciliationRecord
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string TrustAccountId { get; set; }
        [ForeignKey("TrustAccountId")]
        [JsonIgnore]
        public TrustBankAccount? TrustAccount { get; set; }

        public DateTime PeriodEnd { get; set; }
        public double BankStatementBalance { get; set; }
        public double TrustLedgerBalance { get; set; }
        public double ClientLedgerSumBalance { get; set; }
        public bool IsReconciled { get; set; }
        public double DiscrepancyAmount { get; set; } = 0;
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
