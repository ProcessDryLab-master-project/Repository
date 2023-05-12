namespace Repository.App.Entities
{
    public class FileUploadModel
    {
        public IFormFile FileContents { get; set; }
        public FileDetails FileDetails { get; set; }
    }
    public class FileDetails
    {
        public byte[] FileData { get; set; }
        public string ResourceLabel { get; set; }
        public string ResourceType { get; set; }
        public string Description { get; set; }
        public string FileExtension { get; set; }
        public GeneratedFrom GeneratedFrom { get; set; }
        public Parent Parents { get; set; }
        public bool IsDynamic { get; set; }
    }
}
