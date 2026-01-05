using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// Intake form definition
    /// </summary>
    public class IntakeForm
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Practice area this form is for
        /// </summary>
        public string? PracticeArea { get; set; }

        /// <summary>
        /// JSON schema defining form fields
        /// </summary>
        public string FieldsJson { get; set; } = "[]";

        /// <summary>
        /// Styling settings (colors, logo, etc.)
        /// </summary>
        public string? StyleJson { get; set; }

        /// <summary>
        /// Thank you message after submission
        /// </summary>
        public string? ThankYouMessage { get; set; }

        /// <summary>
        /// Redirect URL after submission
        /// </summary>
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// Email to notify on submission
        /// </summary>
        public string? NotifyEmail { get; set; }

        /// <summary>
        /// Auto-assign to this employee
        /// </summary>
        public string? AssignToEmployeeId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Unique slug for public URL
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        public int SubmissionCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Submitted intake form data
    /// </summary>
    public class IntakeSubmission
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string IntakeFormId { get; set; } = string.Empty;

        /// <summary>
        /// JSON containing submitted form data
        /// </summary>
        public string DataJson { get; set; } = "{}";

        /// <summary>
        /// Submitter's IP address
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// New, Reviewed, Converted, Rejected
        /// </summary>
        public string Status { get; set; } = "New";

        /// <summary>
        /// If converted to a lead
        /// </summary>
        public string? LeadId { get; set; }

        /// <summary>
        /// If converted to a client
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Notes from reviewer
        /// </summary>
        public string? ReviewNotes { get; set; }

        public string? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Form field definition (stored in FieldsJson)
    /// </summary>
    public class IntakeFormField
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// text, email, phone, textarea, select, checkbox, radio, date, file
        /// </summary>
        public string Type { get; set; } = "text";

        public bool Required { get; set; } = false;

        public string? Placeholder { get; set; }

        public string? HelpText { get; set; }

        /// <summary>
        /// Options for select/radio (JSON array)
        /// </summary>
        public string? Options { get; set; }

        /// <summary>
        /// Default value
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Validation pattern (regex)
        /// </summary>
        public string? ValidationPattern { get; set; }

        public string? ValidationMessage { get; set; }

        /// <summary>
        /// Conditional display rules (JSON)
        /// </summary>
        public string? ConditionalLogic { get; set; }

        public int Order { get; set; } = 0;
    }
}
