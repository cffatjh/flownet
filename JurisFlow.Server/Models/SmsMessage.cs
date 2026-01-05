using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// SMS message record
    /// </summary>
    public class SmsMessage
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// External message SID from Twilio
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// Sender phone number
        /// </summary>
        public string FromNumber { get; set; } = string.Empty;

        /// <summary>
        /// Recipient phone number
        /// </summary>
        public string ToNumber { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Inbound or Outbound
        /// </summary>
        public string Direction { get; set; } = "Outbound";

        /// <summary>
        /// Queued, Sent, Delivered, Failed, Received
        /// </summary>
        public string Status { get; set; } = "Queued";

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Related matter
        /// </summary>
        public string? MatterId { get; set; }

        /// <summary>
        /// Related client
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// User who sent the message
        /// </summary>
        public string? SentBy { get; set; }

        /// <summary>
        /// Template used (if any)
        /// </summary>
        public string? TemplateId { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// SMS template for quick messaging
    /// </summary>
    public class SmsTemplate
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Category: Appointment, Reminder, Follow-up, Custom
        /// </summary>
        public string Category { get; set; } = "Custom";

        /// <summary>
        /// Variable placeholders: {{client_name}}, {{matter_name}}, {{date}}, etc.
        /// </summary>
        public string? Variables { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Automated reminder configuration
    /// </summary>
    public class SmsReminder
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Appointment, Deadline, Payment, Custom
        /// </summary>
        public string ReminderType { get; set; } = "Appointment";

        /// <summary>
        /// Related entity ID (CalendarEvent, Deadline, Invoice)
        /// </summary>
        public string? EntityId { get; set; }

        public string? EntityType { get; set; }

        public string? ClientId { get; set; }

        public string ToNumber { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// When to send the reminder
        /// </summary>
        public DateTime ScheduledFor { get; set; }

        /// <summary>
        /// Pending, Sent, Failed, Cancelled
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string? SmsMessageId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
