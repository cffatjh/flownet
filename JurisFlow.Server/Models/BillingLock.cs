using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class BillingLock
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string PeriodStart { get; set; } = string.Empty; // yyyy-MM-dd

        [Required]
        public string PeriodEnd { get; set; } = string.Empty;   // yyyy-MM-dd

        public string? LockedByUserId { get; set; }
        public DateTime LockedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
