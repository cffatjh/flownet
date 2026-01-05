using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Role { get; set; } // Admin | Partner | Associate | Employee

        [Required]
        public string PasswordHash { get; set; }

        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? BarNumber { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public string? Preferences { get; set; } // JSON string
        public string? NotificationPreferences { get; set; } // JSON string
        public string? EmployeeRole { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
