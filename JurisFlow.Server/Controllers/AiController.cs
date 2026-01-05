using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlow.Server.Data;
using JurisFlow.Server.Models;
using System.Text.Json;
using System.Diagnostics;

namespace JurisFlow.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly JurisFlowDbContext _context;
        private readonly IConfiguration _configuration;

        public AiController(JurisFlowDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ========== LEGAL RESEARCH ==========

        // POST: api/ai/research
        [HttpPost("research")]
        public async System.Threading.Tasks.Task<ActionResult<ResearchSession>> StartResearch([FromBody] ResearchRequestDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? 
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            var session = new ResearchSession
            {
                UserId = userId,
                MatterId = dto.MatterId,
                Title = dto.Title ?? $"Research: {dto.Query.Substring(0, Math.Min(50, dto.Query.Length))}",
                Query = dto.Query,
                Jurisdiction = dto.Jurisdiction,
                PracticeArea = dto.PracticeArea,
                Status = "Processing"
            };

            _context.ResearchSessions.Add(session);
            await _context.SaveChangesAsync();

            // Process with Gemini AI
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await ProcessLegalResearchWithGemini(session);
                session.Response = result.Response;
                session.CitationsJson = JsonSerializer.Serialize(result.Citations);
                session.KeyPointsJson = JsonSerializer.Serialize(result.KeyPoints);
                session.RelatedCasesJson = JsonSerializer.Serialize(result.RelatedCases);
                session.Status = "Completed";
                session.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                session.Status = "Failed";
                session.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                session.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
            }

            await _context.SaveChangesAsync();
            return Ok(session);
        }

        // GET: api/ai/research
        [HttpGet("research")]
        public async System.Threading.Tasks.Task<ActionResult<IEnumerable<ResearchSession>>> GetResearchHistory(
            [FromQuery] string? matterId = null,
            [FromQuery] int limit = 20)
        {
            var userId = User.FindFirst("sub")?.Value ?? 
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var query = _context.ResearchSessions
                .Where(r => r.UserId == userId || userId == null);

            if (!string.IsNullOrEmpty(matterId))
            {
                query = query.Where(r => r.MatterId == matterId);
            }

            var sessions = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Query,
                    r.Status,
                    r.Jurisdiction,
                    r.PracticeArea,
                    r.ProcessingTimeMs,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(sessions);
        }

        // GET: api/ai/research/{id}
        [HttpGet("research/{id}")]
        public async System.Threading.Tasks.Task<ActionResult<ResearchSession>> GetResearch(string id)
        {
            var session = await _context.ResearchSessions.FindAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            return Ok(session);
        }

        // ========== CONTRACT ANALYSIS ==========

        // POST: api/ai/analyze-contract
        [HttpPost("analyze-contract")]
        public async System.Threading.Tasks.Task<ActionResult<ContractAnalysis>> AnalyzeContract([FromBody] ContractAnalysisDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? 
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            var analysis = new ContractAnalysis
            {
                DocumentId = dto.DocumentId,
                UserId = userId,
                MatterId = dto.MatterId,
                ContractType = dto.ContractType ?? "Unknown",
                Status = "Processing"
            };

            _context.ContractAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            try
            {
                var result = await AnalyzeContractWithGemini(dto.DocumentContent, dto.ContractType);
                
                analysis.Summary = result.Summary;
                analysis.KeyTermsJson = JsonSerializer.Serialize(result.KeyTerms);
                analysis.KeyDatesJson = JsonSerializer.Serialize(result.KeyDates);
                analysis.PartiesJson = JsonSerializer.Serialize(result.Parties);
                analysis.RisksJson = JsonSerializer.Serialize(result.Risks);
                analysis.RiskScore = result.RiskScore;
                analysis.UnusualClausesJson = JsonSerializer.Serialize(result.UnusualClauses);
                analysis.RecommendationsJson = JsonSerializer.Serialize(result.Recommendations);
                analysis.ContractType = result.DetectedType ?? analysis.ContractType;
                analysis.Status = "Completed";
                analysis.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                analysis.Status = "Failed";
                analysis.ErrorMessage = ex.Message;
            }

            await _context.SaveChangesAsync();
            return Ok(analysis);
        }

        // GET: api/ai/contract-analyses
        [HttpGet("contract-analyses")]
        public async System.Threading.Tasks.Task<ActionResult<IEnumerable<ContractAnalysis>>> GetContractAnalyses(
            [FromQuery] string? documentId = null,
            [FromQuery] string? matterId = null)
        {
            var query = _context.ContractAnalyses.AsQueryable();

            if (!string.IsNullOrEmpty(documentId))
            {
                query = query.Where(c => c.DocumentId == documentId);
            }

            if (!string.IsNullOrEmpty(matterId))
            {
                query = query.Where(c => c.MatterId == matterId);
            }

            var analyses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(20)
                .ToListAsync();

            return Ok(analyses);
        }

        // GET: api/ai/contract-analyses/{id}
        [HttpGet("contract-analyses/{id}")]
        public async System.Threading.Tasks.Task<ActionResult<ContractAnalysis>> GetContractAnalysis(string id)
        {
            var analysis = await _context.ContractAnalyses.FindAsync(id);
            if (analysis == null)
            {
                return NotFound();
            }

            return Ok(analysis);
        }

        // ========== CASE PREDICTION ==========

        // POST: api/ai/predict-case
        [HttpPost("predict-case")]
        public async System.Threading.Tasks.Task<ActionResult<CasePrediction>> PredictCase([FromBody] CasePredictionDto dto)
        {
            var userId = User.FindFirst("sub")?.Value ?? 
                         User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

            // Get matter details
            var matter = await _context.Matters.FindAsync(dto.MatterId);
            if (matter == null)
            {
                return NotFound(new { message = "Matter not found" });
            }

            var prediction = new CasePrediction
            {
                MatterId = dto.MatterId,
                UserId = userId,
                Status = "Processing"
            };

            _context.CasePredictions.Add(prediction);
            await _context.SaveChangesAsync();

            try
            {
                var result = await PredictCaseWithGemini(matter, dto.AdditionalContext);
                
                prediction.PredictedOutcome = result.Outcome;
                prediction.Confidence = result.Confidence;
                prediction.FactorsJson = JsonSerializer.Serialize(result.Factors);
                prediction.SimilarCasesJson = JsonSerializer.Serialize(result.SimilarCases);
                prediction.SettlementMin = result.SettlementMin;
                prediction.SettlementMax = result.SettlementMax;
                prediction.EstimatedTimeline = result.Timeline;
                prediction.RecommendationsJson = JsonSerializer.Serialize(result.Recommendations);
                prediction.Status = "Completed";
                prediction.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                prediction.Status = "Failed";
                prediction.ErrorMessage = ex.Message;
            }

            await _context.SaveChangesAsync();
            return Ok(prediction);
        }

        // GET: api/ai/predictions/{matterId}
        [HttpGet("predictions/{matterId}")]
        public async System.Threading.Tasks.Task<ActionResult<IEnumerable<CasePrediction>>> GetPredictions(string matterId)
        {
            var predictions = await _context.CasePredictions
                .Where(p => p.MatterId == matterId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(predictions);
        }

        // ========== AI HELPERS (Simulated - Replace with actual Gemini API) ==========

        private async System.Threading.Tasks.Task<LegalResearchResult> ProcessLegalResearchWithGemini(ResearchSession session)
        {
            // TODO: Replace with actual Gemini API call
            // var geminiClient = new GeminiClient(_configuration["Gemini:ApiKey"]);
            // var prompt = BuildLegalResearchPrompt(session);
            // var response = await geminiClient.GenerateAsync(prompt);

            await System.Threading.Tasks.Task.Delay(500); // Simulate API call

            // Simulated response
            return new LegalResearchResult
            {
                Response = GenerateSimulatedResearchResponse(session),
                Citations = new List<string>
                {
                    "Brown v. Board of Education, 347 U.S. 483 (1954)",
                    "Marbury v. Madison, 5 U.S. 137 (1803)",
                    "Gideon v. Wainwright, 372 U.S. 335 (1963)"
                },
                KeyPoints = new List<string>
                {
                    "The legal principle established in this area applies broadly",
                    "Courts have consistently held similar interpretations",
                    "Recent precedent supports this position"
                },
                RelatedCases = new List<string>
                {
                    "Smith v. Jones (2020) - Similar fact pattern",
                    "Johnson v. State (2019) - Relevant precedent"
                }
            };
        }

        private async System.Threading.Tasks.Task<ContractAnalysisResult> AnalyzeContractWithGemini(string content, string? contractType)
        {
            await System.Threading.Tasks.Task.Delay(500); // Simulate API call

            return new ContractAnalysisResult
            {
                Summary = "This contract establishes a commercial relationship between the parties with defined terms, obligations, and termination conditions.",
                DetectedType = contractType ?? "Service Agreement",
                KeyTerms = new List<KeyValuePair<string, string>>
                {
                    new("Term", "12 months with automatic renewal"),
                    new("Payment", "Net 30 days"),
                    new("Liability Cap", "$100,000")
                },
                KeyDates = new List<KeyValuePair<string, string>>
                {
                    new("Effective Date", DateTime.Now.ToString("yyyy-MM-dd")),
                    new("Renewal Date", DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"))
                },
                Parties = new List<string> { "Party A (Client)", "Party B (Service Provider)" },
                Risks = new List<RiskItem>
                {
                    new() { Level = "Medium", Description = "Broad indemnification clause" },
                    new() { Level = "Low", Description = "Standard limitation of liability" }
                },
                RiskScore = 4,
                UnusualClauses = new List<string>
                {
                    "Non-standard termination notice period (90 days)",
                    "Mandatory arbitration in specific jurisdiction"
                },
                Recommendations = new List<string>
                {
                    "Negotiate shorter termination notice period",
                    "Request cap on indemnification obligations",
                    "Add material breach cure period"
                }
            };
        }

        private async System.Threading.Tasks.Task<CasePredictionResult> PredictCaseWithGemini(Matter matter, string? additionalContext)
        {
            await System.Threading.Tasks.Task.Delay(500); // Simulate API call

            return new CasePredictionResult
            {
                Outcome = "Settlement",
                Confidence = 72.5,
                Factors = new List<string>
                {
                    "Similar cases in this jurisdiction settled 68% of the time",
                    "Strong liability evidence supports favorable outcome",
                    "Defendant's prior settlement history"
                },
                SimilarCases = new List<string>
                {
                    "Johnson v. ABC Corp (2022) - Settled, $45,000",
                    "Williams v. XYZ Inc (2021) - Settled, $62,000"
                },
                SettlementMin = 35000,
                SettlementMax = 75000,
                Timeline = "6-9 months to resolution",
                Recommendations = new List<string>
                {
                    "Consider early mediation to reduce costs",
                    "Gather additional documentation on damages",
                    "Prepare for discovery requests"
                }
            };
        }

        private string GenerateSimulatedResearchResponse(ResearchSession session)
        {
            return $@"## Legal Research Summary

**Query:** {session.Query}

**Jurisdiction:** {session.Jurisdiction ?? "Federal/General"}

### Analysis

Based on established legal precedent and current statutory framework, the following key points emerge:

1. **Foundational Principles**: The legal doctrine in this area is well-established, with courts consistently applying similar standards.

2. **Relevant Case Law**: Multiple precedents support the interpretation that [specific legal principle applies].

3. **Statutory Framework**: The applicable statutes provide clear guidance on this matter.

### Recommendations

- Review the cited cases for specific factual similarities
- Consider jurisdictional variations in application
- Document all relevant evidence supporting the legal position

*Note: This research is AI-generated and should be verified by legal counsel.*";
        }
    }

    // DTOs and Result Classes
    public class ResearchRequestDto
    {
        public string Query { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? MatterId { get; set; }
        public string? Jurisdiction { get; set; }
        public string? PracticeArea { get; set; }
    }

    public class ContractAnalysisDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentContent { get; set; } = string.Empty;
        public string? MatterId { get; set; }
        public string? ContractType { get; set; }
    }

    public class CasePredictionDto
    {
        public string MatterId { get; set; } = string.Empty;
        public string? AdditionalContext { get; set; }
    }

    internal class LegalResearchResult
    {
        public string Response { get; set; } = string.Empty;
        public List<string> Citations { get; set; } = new();
        public List<string> KeyPoints { get; set; } = new();
        public List<string> RelatedCases { get; set; } = new();
    }

    internal class ContractAnalysisResult
    {
        public string Summary { get; set; } = string.Empty;
        public string? DetectedType { get; set; }
        public List<KeyValuePair<string, string>> KeyTerms { get; set; } = new();
        public List<KeyValuePair<string, string>> KeyDates { get; set; } = new();
        public List<string> Parties { get; set; } = new();
        public List<RiskItem> Risks { get; set; } = new();
        public int RiskScore { get; set; }
        public List<string> UnusualClauses { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    internal class RiskItem
    {
        public string Level { get; set; } = "Low";
        public string Description { get; set; } = string.Empty;
    }

    internal class CasePredictionResult
    {
        public string Outcome { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Factors { get; set; } = new();
        public List<string> SimilarCases { get; set; } = new();
        public decimal? SettlementMin { get; set; }
        public decimal? SettlementMax { get; set; }
        public string? Timeline { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }
}
