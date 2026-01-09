using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class RetentionPolicy
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string EntityName { get; set; } = string.Empty;

        public int RetentionDays { get; set; } = 365;

        public bool IsActive { get; set; } = true;

        public DateTime? LastAppliedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
