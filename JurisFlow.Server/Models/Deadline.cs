using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// Calculated deadline for a matter
    /// </summary>
    public class Deadline
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string MatterId { get; set; } = string.Empty;

        /// <summary>
        /// Reference to the court rule used for calculation
        /// </summary>
        public string? CourtRuleId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// The calculated deadline date
        /// </summary>
        [Required]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// The trigger date used for calculation
        /// </summary>
        public DateTime? TriggerDate { get; set; }

        /// <summary>
        /// Pending, Completed, Missed, Extended
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// High, Medium, Low
        /// </summary>
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// Filing, Hearing, Response, Discovery, Trial, Other
        /// </summary>
        public string DeadlineType { get; set; } = "Filing";

        /// <summary>
        /// Days before deadline to send reminder
        /// </summary>
        public int ReminderDays { get; set; } = 3;

        public bool ReminderSent { get; set; } = false;

        public string? AssignedTo { get; set; }

        public string? Notes { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? CompletedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
