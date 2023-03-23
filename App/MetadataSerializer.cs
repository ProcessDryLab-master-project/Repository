using Newtonsoft.Json;

namespace Repository.App
{
    public class MetadataObject
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ResourceId { get; set; }
        public string CreationDate { get; set; }
        public ResourceInfo ResourceInfo { get; set; }

        public GenerationTree GenerationTree { get; set; }
    }
    public class ResourceInfo
    {
        public string ResourceLabel { get; set; }
        public string ResourceType { get; set; } // EventLog, EventStream, Image, ProcessModel, PetriNet, Histogram, Graph, Alignment, etc.
        public string Host { get; set; }
        public string? FileExtension { get; set; }
        public string? StreamTopic { get; set; }
        public string Description { get; set; }
        public bool Dynamic { get; set; }
    }
    public class GenerationTree
    {
        public GeneratedFrom? GeneratedFrom { get; set; }
        public List<Parent>? Parents { get; set; }
        public List<Child>? Children { get; set; }
    }
    public class GeneratedFrom
    {
        public string MinerHost { get; set; }
        public string MinerId { get; set; }
        public string MinerLabel { get; set; }
    }
    public class Parent
    {
        public string ResourceId { get; set; }
        public string From { get; set; }
    }
    public class Child
    {
        public string ResourceId { get; set; }
    }
}
