using System;
using System.ComponentModel.DataAnnotations;

namespace JurisFlow.Server.Models
{
    /// <summary>
    /// AI-powered legal research session
    /// </summary>
    public class ResearchSession
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string UserId { get; set; } = string.Empty;

        public string? MatterId { get; set; }

        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Legal question or research topic
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Jurisdiction context (Federal, State, etc.)
        /// </summary>
        public string? Jurisdiction { get; set; }

        /// <summary>
        /// Practice area context
        /// </summary>
        public string? PracticeArea { get; set; }

        /// <summary>
        /// AI-generated research response
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// Extracted citations (JSON array)
        /// </summary>
        public string? CitationsJson { get; set; }

        /// <summary>
        /// Key points summary (JSON array)
        /// </summary>
        public string? KeyPointsJson { get; set; }

        /// <summary>
        /// Related cases found (JSON array)
        /// </summary>
        public string? RelatedCasesJson { get; set; }

        /// <summary>
        /// Pending, Completed, Failed
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public int? ProcessingTimeMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// AI contract analysis result
    /// </summary>
    public class ContractAnalysis
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DocumentId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string? MatterId { get; set; }

        /// <summary>
        /// Type of contract (Employment, NDA, Lease, etc.)
        /// </summary>
        public string ContractType { get; set; } = string.Empty;

        /// <summary>
        /// AI summary of the contract
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// Key terms extracted (JSON)
        /// </summary>
        public string? KeyTermsJson { get; set; }

        /// <summary>
        /// Key dates (start, end, renewal, etc.) (JSON)
        /// </summary>
        public string? KeyDatesJson { get; set; }

        /// <summary>
        /// Parties involved (JSON array)
        /// </summary>
        public string? PartiesJson { get; set; }

        /// <summary>
        /// Identified risks and concerns (JSON array)
        /// </summary>
        public string? RisksJson { get; set; }

        /// <summary>
        /// Overall risk score 1-10
        /// </summary>
        public int RiskScore { get; set; } = 0;

        /// <summary>
        /// Unusual or missing clauses (JSON array)
        /// </summary>
        public string? UnusualClausesJson { get; set; }

        /// <summary>
        /// Recommendations for negotiation (JSON array)
        /// </summary>
        public string? RecommendationsJson { get; set; }

        /// <summary>
        /// Pending, Completed, Failed
        /// </summary>
        public string Status { get; set; } = "Pending";

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// AI-generated case prediction
    /// </summary>
    public class CasePrediction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string MatterId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Predicted outcome (Win, Lose, Settle)
        /// </summary>
        public string PredictedOutcome { get; set; } = string.Empty;

        /// <summary>
        /// Confidence percentage 0-100
        /// </summary>
        public double Confidence { get; set; } = 0;

        /// <summary>
        /// Factors influencing prediction (JSON array)
        /// </summary>
        public string? FactorsJson { get; set; }

        /// <summary>
        /// Similar cases referenced (JSON array)
        /// </summary>
        public string? SimilarCasesJson { get; set; }

        /// <summary>
        /// Predicted settlement range
        /// </summary>
        public decimal? SettlementMin { get; set; }

        public decimal? SettlementMax { get; set; }

        /// <summary>
        /// Estimated timeline to resolution
        /// </summary>
        public string? EstimatedTimeline { get; set; }

        /// <summary>
        /// Strategic recommendations (JSON array)
        /// </summary>
        public string? RecommendationsJson { get; set; }

        public string Status { get; set; } = "Pending";

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }
}
