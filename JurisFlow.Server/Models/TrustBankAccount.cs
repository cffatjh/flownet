using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public enum TrustAccountStatus
    {
        ACTIVE,
        INACTIVE,
        CLOSED
    }

    public class TrustBankAccount
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string BankName { get; set; }
        public string AccountNumberEnc { get; set; } // Encrypted in theory, storing raw for now as per demo
        public string RoutingNumber { get; set; }
        public string Jurisdiction { get; set; }
        public double CurrentBalance { get; set; } = 0;
        public TrustAccountStatus Status { get; set; } = TrustAccountStatus.ACTIVE;
        public string? EntityId { get; set; }
        public string? OfficeId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
