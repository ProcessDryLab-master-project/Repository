using csdot.Attributes.Types;
using Microsoft.VisualBasic.FileIO;
using Repository.App.Entities;
using System.IO;
using System.Security.AccessControl;

namespace Repository.App.Database
{
    public class FileDb : IFileDb
    {
        static readonly ReaderWriterLock fileLock = new ReaderWriterLock();
        static readonly int milliSecTimeout = 60000; // Timeout after 60 sec
        public void CopyStream(Stream stream, string downloadPath)
        {
            using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyToAsync(fileStream);
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
                return Results.BadRequest("Repository GetFileById error: " + e);
            }
        }
        public async Task<IResult> WriteFile(MetadataObject metadataObject, IFormFile file)
        {
            try
            {
                fileLock.AcquireWriterLock(milliSecTimeout);
                Console.WriteLine("File lock set");
                string path = DbHelper.GetFileSavePath(metadataObject);
                Console.WriteLine($"Attempting to write file for id {metadataObject.ResourceId} to path {path}");

                //using var stream = File.OpenWrite(path);
                //await file.CopyToAsync(stream);

                // If we read IFormFile into byte[] first:
                //var content = new MemoryStream(file);
                //CopyStream(content, path);

                // If we use IFormFile "file" instead:
                await using (var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    fileStream.SetLength(0); // truncate the file
                    file.CopyTo(fileStream); // write to the file
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while saving incoming file: " + e);
                return Results.BadRequest("Repository WriteFile error:" + e);
            }
            finally
            {
                fileLock.ReleaseWriterLock();
                Console.WriteLine("File lock released");
            }
            Console.WriteLine("Done, returning ok");
            return Results.Ok(metadataObject.ResourceId);
        }
    }
}
