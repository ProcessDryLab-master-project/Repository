using Newtonsoft.Json;

namespace Repository.App
{
    public class MetadataObject
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? FileId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string FileExtension { get; set; }
        public string RepositoryHost { get; set; }
        public string CreationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? BasedOnId { get; set; }
    }
}
