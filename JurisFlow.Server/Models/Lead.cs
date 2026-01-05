using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JurisFlow.Server.Models
{
    public class Lead
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Email { get; set; }
        
        public string? Phone { get; set; }
        
        public string? Source { get; set; } = "Referral";
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedValue { get; set; }
        
        public string Status { get; set; } = "New"; // New, Contacted, Consultation, Retained, Lost
        
        public string? PracticeArea { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
