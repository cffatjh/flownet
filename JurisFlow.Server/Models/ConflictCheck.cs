using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class ConflictCheck
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string SearchQuery { get; set; } = string.Empty;

        public string? CheckType { get; set; } // NewClient, NewMatter, OpposingParty, Manual

        public string? EntityType { get; set; } // Client, Matter, OpposingParty

        public string? EntityId { get; set; } // ID of the entity being checked

        public string? CheckedBy { get; set; } // User ID who initiated the check

        public string Status { get; set; } = "Pending"; // Pending, Clear, Conflict, Waived

        public int MatchCount { get; set; } = 0;

        public string? WaivedBy { get; set; }

        public string? WaiverReason { get; set; }

        public DateTime? WaivedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
