using Newtonsoft.Json;

namespace Repository.App
{
    //public enum ResourceType // Maybe add later to check for valid type.
    //{
    //    Visualization,
    //    EventLog,
    //    EventStream
    //}
    public class MetadataObject
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FileId { get; set; } // TODO: Change to ResourceId across all projects to better reflect that it can also be a Stream
        public string ResourceLabel { get; set; } // TODO: Change to ResourceLabel across all projects to better reflect that it can also be a Stream
        public string ResourceType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FileExtension { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? StreamBroker { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? StreamTopic { get; set; }
        public string RepositoryHost { get; set; }
        public string CreationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Parents { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Children { get; set; }
    }
}
