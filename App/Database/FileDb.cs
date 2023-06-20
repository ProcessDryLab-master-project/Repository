using csdot.Attributes.Types;
using Microsoft.VisualBasic.FileIO;
using Repository.App.Entities;
using System.IO;
using System.Security.AccessControl;

namespace Repository.App.Database
{
    public class FileDb : IFileDb
    {
        public void CopyStream(IFormFile file, string downloadPath)
        {
            using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
            {
                file.CopyToAsync(fileStream);
            }
        }

        public IResult GetFile(MetadataObject metadataObject)
        {
            try
            {
                string? fileExtension = metadataObject.ResourceInfo.FileExtension;
                string nameOfFile = string.IsNullOrWhiteSpace(fileExtension) ? metadataObject.ResourceId! : metadataObject.ResourceId! + "." + fileExtension;

                string pathToResourceType = Path.Combine(Globals.pathToResources, metadataObject.ResourceInfo.ResourceType);
                string pathToFile = Path.Combine(pathToResourceType, nameOfFile);

                if (!File.Exists(pathToFile))
                {
                    string badResponse = "No such file exists for path " + pathToFile; // TODO: Should not return the entire path, just easier like this for now
                    return Results.BadRequest(badResponse);
                }

                return Results.File(pathToFile, metadataObject.ResourceId);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }

        public IResult WriteFile(MetadataObject metadataObject, byte[] file)
        {
            string path = DbHelper.GetFileSavePath(metadataObject);
            file.Write(path);
            Console.WriteLine("Done, returning resource id: " + metadataObject.ResourceId);
            return Results.Ok(metadataObject.ResourceId);
        }
    }
}
