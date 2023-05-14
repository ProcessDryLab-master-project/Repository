using csdot.Attributes.Types;
using Microsoft.VisualBasic.FileIO;
using Repository.App.Entities;
using System.Security.AccessControl;

namespace Repository.App.Database
{
    public class FileDb : IFileDb
    {
        static readonly ReaderWriterLock fileLock = new ReaderWriterLock();
        static readonly int milliSecTimeout = 60000; // Timeout after 60 sec
        //private readonly DatabaseManager databaseManager;
        //public FileDb(DatabaseManager databaseManager)
        //{
        //    this.databaseManager = databaseManager;
        //}
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
                string nameOfFile = string.IsNullOrWhiteSpace(fileExtension) ? metadataObject.ResourceId : metadataObject.ResourceId + "." + fileExtension;

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
        /* To prevent the error where formdata has been read, maybe put IFormFile fileData into a byte[] like this:
                This could be called in an outer function where the request is made, so the file saving can be put on a queue
                using (var stream = new MemoryStream())
                {
                    fileData.CopyTo(stream);
                    fileByteArr = stream.ToArray();
                }
                
                Then from in here,, the fileByteArr could be sent instead of fileData and save like this:
                string pathToResourceType = Path.Combine(Globals.pathToResources, resourceType);
                var content = new MemoryStream(fileByteArr);
                await CopyStream(content, pathToResourceType);
        */

        public IResult WriteFile(MetadataObject metadataObject, byte[] fileData)
        {
            try
            {
                fileLock.AcquireWriterLock(milliSecTimeout);
                Console.WriteLine("File lock set");
                string pathToSaveFile = DbHelper.GetFileSavePath(metadataObject);
                Console.WriteLine($"Attempting to write file for id {metadataObject.ResourceId} to path {pathToSaveFile}");
                // If we read IFormFile into byte[] first:
                var content = new MemoryStream(fileData);
                CopyStream(content, pathToSaveFile);
                // If we use IFormFile "file" instead:
                //using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                //{
                //    fileStream.SetLength(0); // truncate the file
                //    file.CopyTo(fileStream); // write to the file
                //}
                //databaseManager.Add(metadataObject);
            }
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error while saving incoming file: " + e);
            //    return Results.BadRequest("Repository WriteFile error:" + e);
            //}
            finally
            {
                fileLock.ReleaseWriterLock();
                Console.WriteLine("File lock released");
            }
            Console.WriteLine("Done, returning ok");
            return Results.Ok(metadataObject.ResourceId);
        }


        //public bool SaveFile(IFormFile file, string fileName, string filePath)
        //{
        //    try
        //    {
        //        locker.AcquireWriterLock(milliSecTimeout);
        //        Console.WriteLine("Lock set");
        //        using (var fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        //        {
        //            fileStream.SetLength(0); // truncate the file
        //            file.CopyTo(fileStream); // write to the file
        //            Console.WriteLine($"Saved file: {fileName}");
        //            return true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error while saving incoming file: " + e);
        //        return false;
        //    }
        //    finally
        //    {
        //        locker.ReleaseWriterLock();
        //        Console.WriteLine("Lock released");
        //    }
        //}
    }
}
