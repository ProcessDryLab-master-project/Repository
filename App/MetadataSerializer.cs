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
        public string? ResourceId { get; set; }
        public string ResourceLabel { get; set; }
        public string ResourceType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FileExtension { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? StreamTopic { get; set; }
        public string Host { get; set; }
        public string CreationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Parents { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Children { get; set; }
    }
}
