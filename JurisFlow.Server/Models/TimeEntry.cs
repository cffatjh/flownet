using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JurisFlow.Server.Models
{
    public class TimeEntry
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? MatterId { get; set; }

        [ForeignKey("MatterId")]
        [JsonIgnore]
        public Matter? Matter { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public int Duration { get; set; } // minutes

        public double Rate { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        public bool Billed { get; set; } = false;

        public bool IsBillable { get; set; } = true;

        public string Type { get; set; } = "time";

        // UTBMS codes
        public string? ActivityCode { get; set; }
        public string? TaskCode { get; set; }

        // Approval workflow
        public string ApprovalStatus { get; set; } = "Pending";
        public string? SubmittedBy { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
