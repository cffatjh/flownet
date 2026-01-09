using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace JurisFlow.Server.Models
{
    public class DocumentContentToken
    {
        [Required]
        [Column(Order = 0)]
        public string DocumentId { get; set; } = string.Empty;

        [Required]
        [Column(Order = 1)]
        [MaxLength(64)]
        public string Token { get; set; } = string.Empty;

        [ForeignKey("DocumentId")]
        [JsonIgnore]
        public Document? Document { get; set; }
    }
}
