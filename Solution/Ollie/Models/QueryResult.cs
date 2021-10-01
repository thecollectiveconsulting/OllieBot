using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ollie.Models
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class Column
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class Table
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("columns")]
        public List<Column> Columns { get; set; }

        [JsonPropertyName("rows")]
        public List<List<int>> Rows { get; set; }
    }

    public class QueryResult
    {
        [JsonPropertyName("tables")]
        public List<Table> Tables { get; set; }
    }


}
