using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JurisFlow.Server.Models
{
    public enum LedgerStatus
    {
        ACTIVE,
        CLOSED,
        FROZEN
    }

    public class ClientTrustLedger
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ClientId { get; set; }
        [ForeignKey("ClientId")]
        [JsonIgnore]
        public Client? Client { get; set; }

        public string? MatterId { get; set; }
        [ForeignKey("MatterId")]
        [JsonIgnore]
        public Matter? Matter { get; set; }

        public string TrustAccountId { get; set; }
        [ForeignKey("TrustAccountId")]
        [JsonIgnore]
        public TrustBankAccount? TrustAccount { get; set; }

        public string? EntityId { get; set; }

        public string? OfficeId { get; set; }

        public double RunningBalance { get; set; } = 0;
        public LedgerStatus Status { get; set; } = LedgerStatus.ACTIVE;
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
