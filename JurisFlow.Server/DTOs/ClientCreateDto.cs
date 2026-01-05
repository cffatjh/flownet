using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.DTOs
{
    public class ClientCreateDto
    {
        public string? ClientNumber { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Company { get; set; }
        
        [Required]
        public string Type { get; set; } = "Individual"; // Individual | Corporate
        
        [Required]
        public string Status { get; set; } = "Active"; // Active | Inactive

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? TaxId { get; set; }
        
        // Corporate-specific fields
        public string? IncorporationState { get; set; }
        public string? RegisteredAgent { get; set; }
        public string? AuthorizedRepresentatives { get; set; }
        
        public string? Notes { get; set; }

        // Optional password for portal access
        public string? Password { get; set; }
    }
}
