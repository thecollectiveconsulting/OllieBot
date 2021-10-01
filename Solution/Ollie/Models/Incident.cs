using System.Text.Json.Serialization;

namespace Ollie.Models
{
    public class Properties
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("incidentUrl")]
        public string IncidentUrl { get; set; }
    }

    public class Incident
    {
        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }
    }
}
