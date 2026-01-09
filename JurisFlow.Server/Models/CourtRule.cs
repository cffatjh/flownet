using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// Court rule for deadline calculation
    /// Based on US jurisdictional rules (Federal, State, Local)
    /// </summary>
    public class CourtRule
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Federal, State, Local
        /// </summary>
        public string RuleType { get; set; } = "State";

        /// <summary>
        /// Jurisdiction code (e.g., "CA", "NY", "USDC-SDNY")
        /// </summary>
        public string Jurisdiction { get; set; } = string.Empty;

        /// <summary>
        /// Court type (e.g., "Superior", "District", "Bankruptcy")
        /// </summary>
        public string? CourtType { get; set; }

        /// <summary>
        /// Rule citation (e.g., "CCP Sec. 1005(b)")
        /// </summary>
        public string? Citation { get; set; }

        /// <summary>
        /// Event that triggers the deadline (e.g., "Motion Filing", "Service of Summons")
        /// </summary>
        public string TriggerEvent { get; set; } = string.Empty;

        /// <summary>
        /// Number of days for the deadline
        /// </summary>
        public int DaysCount { get; set; }

        /// <summary>
        /// Calendar or Court days
        /// </summary>
        public string DayType { get; set; } = "Calendar";

        /// <summary>
        /// Before or After the trigger event
        /// </summary>
        public string Direction { get; set; } = "After";

        /// <summary>
        /// Add days for service method (e.g., +5 for mail)
        /// </summary>
        public int ServiceDaysAdd { get; set; } = 0;

        /// <summary>
        /// Description of the rule
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// If deadline falls on weekend/holiday, adjust to next business day
        /// </summary>
        public bool ExtendIfWeekend { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
