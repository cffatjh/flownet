using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JurisFlow.Server.Models
{
    public class DocumentContentIndex
    {
        [Key]
        public string DocumentId { get; set; } = string.Empty;

        [ForeignKey("DocumentId")]
        [JsonIgnore]
        public Document? Document { get; set; }

        public string? Content { get; set; }
        public string? NormalizedContent { get; set; }
        public string? ContentHash { get; set; }
        public int ContentLength { get; set; }
        public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
    }
}
