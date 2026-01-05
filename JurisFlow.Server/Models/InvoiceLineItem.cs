using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class InvoiceLineItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string InvoiceId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "time"; // time | expense | fixed

        [MaxLength(20)]
        public string? TaskCode { get; set; } // UTBMS task code

        [MaxLength(20)]
        public string? ExpenseCode { get; set; } // UTBMS expense code

        [MaxLength(20)]
        public string? ActivityCode { get; set; } // UTBMS activity code

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public double Quantity { get; set; } = 1;
        public double Rate { get; set; } = 0;
        public double Amount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Invoice Invoice { get; set; } = null!;
    }
}
