using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// Represents the opposing party in a legal matter
    /// Used for conflict of interest checking per ABA Model Rules
    /// </summary>
    public class OpposingParty
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string MatterId { get; set; } = string.Empty;
        
        [ForeignKey("MatterId")]
        public Matter? Matter { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Individual, Corporation, LLC, Partnership, Government, Other
        /// </summary>
        public string Type { get; set; } = "Individual";
        
        /// <summary>
        /// Company name if Type is Corporate
        /// </summary>
        public string? Company { get; set; }
        
        /// <summary>
        /// Tax ID / EIN for corporations
        /// </summary>
        public string? TaxId { get; set; }
        
        /// <summary>
        /// State of incorporation for corporations
        /// </summary>
        public string? IncorporationState { get; set; }
        
        // Opposing Counsel Information
        public string? CounselName { get; set; }
        public string? CounselFirm { get; set; }
        public string? CounselEmail { get; set; }
        public string? CounselPhone { get; set; }
        public string? CounselAddress { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
