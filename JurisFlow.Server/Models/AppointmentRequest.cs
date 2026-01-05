using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class AppointmentRequest
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public string? MatterId { get; set; }

        [Required]
        public DateTime RequestedDate { get; set; }

        public int Duration { get; set; } = 30;

        [Required]
        public string Type { get; set; } = "consultation";

        public string? Notes { get; set; }

        [Required]
        public string Status { get; set; } = "pending";

        public string? AssignedTo { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
