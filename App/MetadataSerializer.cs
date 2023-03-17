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
        public string CreationDate { get; set; }
        public string ResourceLabel { get; set; }
        public string ResourceType { get; set; }
        public string Host { get; set; }
        public string Description { get; set; }
        public StreamInfo StreamInfo { get; set; }

        public FileInfo FileInfo { get; set; }

        public GenerationTree GenerationTree { get; set; }

    }
    public class StreamInfo
    {
        public string? StreamTopic { get; set; }
    }
    public class FileInfo
    {
        public string? FileExtension { get; set; }
    }
    public class GenerationTree
    {
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Parents { get; set; }
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Children { get; set; }
    }
}
