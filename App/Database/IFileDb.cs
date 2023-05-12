using Microsoft.VisualBasic.FileIO;
using Repository.App.Entities;

namespace Repository.App.Database
{
    public interface IFileDb
    {
        //public Task<IResult> GetFileById(string resourceId);
        public IResult GetFile(MetadataObject metadataObject);
        //public Task<IResult> WriteFile(IFormCollection formData, string host);
        public IResult WriteFile(MetadataObject metadataObject, byte[] fileData);
        //public Task<IResult> PostFileAsync(byte[] fileData, string resourceType);

        //public Task PostMultiFileAsync(List<FileUploadModel> fileData);

    }
}
