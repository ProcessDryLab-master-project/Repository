namespace Repository.App.Entities
{
    public class FormObject
    {
        public byte[]? File { get; set; }
        public string? Host { get; set; }
        public string? StreamTopic { get; set; }
        public string? FileExtension { get; set; }
        public string? ResourceLabel { get; set; }
        public string? ResourceType { get; set; }
        public string? Description { get; set; }
        public string? Parents { get; set; }
        public string? GeneratedFrom { get; set; }
        public bool Overwrite { get; set; }
        public bool Dynamic { get; set; }
    }
}
