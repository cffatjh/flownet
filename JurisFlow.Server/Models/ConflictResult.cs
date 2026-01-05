using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class ConflictResult
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ConflictCheckId { get; set; } = string.Empty;

        [Required]
        public string MatchedEntityType { get; set; } = string.Empty; // Client, Matter, OpposingParty

        [Required]
        public string MatchedEntityId { get; set; } = string.Empty;

        public string MatchedEntityName { get; set; } = string.Empty;

        public string MatchType { get; set; } = "Exact"; // Exact, Fuzzy, Phonetic, Email, Phone

        public double MatchScore { get; set; } = 100.0; // 0-100 confidence score

        public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High

        public string? Details { get; set; } // Additional match details

        public string? RelatedMatterId { get; set; } // If client match, show related matter

        public string? RelatedMatterName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
