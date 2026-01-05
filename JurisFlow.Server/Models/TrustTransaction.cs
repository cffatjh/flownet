using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JurisFlow.Server.Models
{
    public class TrustTransaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string? MatterId { get; set; }

        [ForeignKey("MatterId")]
        [JsonIgnore]
        public Matter? Matter { get; set; }

        [Required]
        public string Type { get; set; } // Deposit, Withdrawal, etc.

        public double Amount { get; set; }
        public string Description { get; set; }
        public string? Reference { get; set; }

        public bool IsEarned { get; set; } = false;
        public DateTime? EarnedDate { get; set; }

        public double BalanceBefore { get; set; } = 0;
        public double BalanceAfter { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
