using Microsoft.VisualBasic.FileIO;
using Repository.App.Entities;

namespace Repository.App.Database
{
    public interface IFileDb
    {
        public IResult GetFile(MetadataObject metadataObject);
        public IResult WriteFile(MetadataObject metadataObject, byte[] file);
    }
}
