using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// Payment transaction record for online payments
    /// </summary>
    public class PaymentTransaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Related invoice ID
        /// </summary>
        public string? InvoiceId { get; set; }

        public string? MatterId { get; set; }

        public string? ClientId { get; set; }

        [Required]
        public double Amount { get; set; }

        // UTBMS/LEDES task/expense codes (optional)
        public string? TaskCode { get; set; }
        public string? ExpenseCode { get; set; }
        public string? ActivityCode { get; set; }

        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Stripe, LawPay, PayPal, Check, Wire, Cash
        /// </summary>
        public string PaymentMethod { get; set; } = "Stripe";

        /// <summary>
        /// External transaction ID from payment provider
        /// </summary>
        public string? ExternalTransactionId { get; set; }

        /// <summary>
        /// Pending, Processing, Succeeded, Failed, Refunded, Partially Refunded
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string? FailureReason { get; set; }

        public double? RefundAmount { get; set; }

        public string? RefundReason { get; set; }

        public DateTime? RefundedAt { get; set; }

        public string? ReceiptUrl { get; set; }

        public string? PayerEmail { get; set; }

        public string? PayerName { get; set; }

        /// <summary>
        /// Last 4 digits of card
        /// </summary>
        public string? CardLast4 { get; set; }

        public string? CardBrand { get; set; } // Visa, Mastercard, Amex, etc.

        public string? ProcessedBy { get; set; } // User ID

        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
