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
        public string? FileId { get; set; }
        public string FileLabel { get; set; }
        public string ResourceType { get; set; }
        public string FileExtension { get; set; }
        public string RepositoryHost { get; set; }
        public string CreationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? BasedOnId { get; set; }
    }
}
