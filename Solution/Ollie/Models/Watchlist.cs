using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ollie.Models
{
    public class CreatedBy
    {
        [JsonPropertyName("objectId")]
        public string ObjectId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class UpdatedBy
    {
        [JsonPropertyName("objectId")]
        public string ObjectId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class WatchListProperties
    {
        [JsonPropertyName("watchlistId")]
        public string WatchlistId { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }

        [JsonPropertyName("createdBy")]
        public CreatedBy CreatedBy { get; set; }

        [JsonPropertyName("updatedBy")]
        public UpdatedBy UpdatedBy { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("watchlistType")]
        public string WatchlistType { get; set; }

        [JsonPropertyName("watchlistAlias")]
        public string WatchlistAlias { get; set; }

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("labels")]
        public List<object> Labels { get; set; }

        [JsonPropertyName("tenantId")]
        public string TenantId { get; set; }

        [JsonPropertyName("numberOfLinesToSkip")]
        public int NumberOfLinesToSkip { get; set; }

        [JsonPropertyName("uploadStatus")]
        public string UploadStatus { get; set; }
    }

    public class WatchList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("properties")]
        public WatchListProperties Properties { get; set; }
    }




}
