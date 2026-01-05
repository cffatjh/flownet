using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// Email message synced from external provider
    /// </summary>
    public class EmailMessage
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// External message ID from provider (Outlook/Gmail)
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// Outlook, Gmail
        /// </summary>
        public string Provider { get; set; } = "Outlook";

        /// <summary>
        /// Email account ID this message belongs to
        /// </summary>
        public string? EmailAccountId { get; set; }

        /// <summary>
        /// Matter this email is linked to
        /// </summary>
        public string? MatterId { get; set; }

        /// <summary>
        /// Client this email is linked to
        /// </summary>
        public string? ClientId { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated list of recipients
        /// </summary>
        public string ToAddresses { get; set; } = string.Empty;

        public string? CcAddresses { get; set; }

        public string? BccAddresses { get; set; }

        /// <summary>
        /// Plain text body
        /// </summary>
        public string? BodyText { get; set; }

        /// <summary>
        /// HTML body
        /// </summary>
        public string? BodyHtml { get; set; }

        /// <summary>
        /// Inbox, Sent, Draft
        /// </summary>
        public string Folder { get; set; } = "Inbox";

        public bool IsRead { get; set; } = false;

        public bool HasAttachments { get; set; } = false;

        public int AttachmentCount { get; set; } = 0;

        /// <summary>
        /// Importance level: Low, Normal, High
        /// </summary>
        public string Importance { get; set; } = "Normal";

        public DateTime ReceivedAt { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Email account configuration for sync
    /// </summary>
    public class EmailAccount
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Outlook, Gmail
        /// </summary>
        public string Provider { get; set; } = "Outlook";

        public string EmailAddress { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        /// <summary>
        /// Encrypted access token
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Encrypted refresh token
        /// </summary>
        public string? RefreshToken { get; set; }

        public DateTime? TokenExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool SyncEnabled { get; set; } = true;

        /// <summary>
        /// Last successful sync timestamp
        /// </summary>
        public DateTime? LastSyncAt { get; set; }

        public string? SyncError { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
