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
        private const int NumberOfRetries = 3;
        private const int DelayOnRetry = 1000;
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

        //public async Task<IResult> WriteFile(MetadataObject metadataObject, IFormFile file)
        //{
        //    IOException errorBackup;
        //    for (int i = 1; i <= NumberOfRetries; ++i)
        //    {
        //        try
        //        {
        //            fileLock.AcquireWriterLock(milliSecTimeout);
        //            Console.WriteLine("File lock set");
        //            string path = DbHelper.GetFileSavePath(metadataObject);
        //            Console.WriteLine($"Attempting to write file for id {metadataObject.ResourceId} to path {path}");

        //            using var stream = File.OpenWrite(path);
        //            await file.CopyToAsync(stream);


        //            // If we read IFormFile into byte[] first:
        //            //var content = new MemoryStream(file);
        //            //CopyStream(content, path);

        //            // This can force the deserializing error.
        //            //await using (var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
        //            //{
        //            //    fileStream.SetLength(0); // truncate the file
        //            //    file.CopyTo(fileStream); // write to the file
        //            //}

        //            Console.WriteLine("Done, returning ok");
        //            return Results.Ok(metadataObject.ResourceId);
        //            //break; // When done we can break loop
        //        }
        //        catch (IOException e) when (i <= NumberOfRetries)
        //        {
        //            // You may check error code to filter some exceptions, not every error can be recovered.
        //            Console.WriteLine($"Failed to save file on attempt {i}, with exception {e}");
        //            errorBackup = e;
        //            Thread.Sleep(DelayOnRetry);
        //        }
        //        catch (IOException e) when (i > NumberOfRetries)
        //        {
        //            // You may check error code to filter some exceptions, not every error can be recovered.
        //            Console.WriteLine($"Failed to save file on last attempt {i}, with exception {e}");
        //            return Results.BadRequest(e);
        //        }
        //        finally
        //        {
        //            fileLock.ReleaseWriterLock();
        //            Console.WriteLine("File lock released");
        //        }
        //    }
        //    Console.WriteLine("Done, returning ok");
        //    return Results.Ok(metadataObject.ResourceId);
        //}
        public IResult WriteFile(MetadataObject metadataObject, IFormFile file)
        {
            string path = DbHelper.GetFileSavePath(metadataObject);
            file.Write(path);
            //try
            //{
            //    fileLock.AcquireWriterLock(milliSecTimeout);
            //    Console.WriteLine("File lock set");
            //    string path = DbHelper.GetFileSavePath(metadataObject);
            //    Console.WriteLine($"Attempting to write file for id {metadataObject.ResourceId} to path {path}");

            //    //CopyStream(file, path);
            //    //using var stream = File.OpenWrite(path);
            //    //await file.CopyToAsync(stream);

            //    // If we read IFormFile into byte[] first:
            //    //var content = new MemoryStream(file);
            //    //CopyStream(content, path);

            //    using (var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            //    {
            //        fileStream.SetLength(0); // truncate the file
            //        file.CopyTo(fileStream); // write to the file
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error while saving incoming file: " + e);
            //    return Results.BadRequest("Repository WriteFile error:" + e);
            //}
            //finally
            //{
            //    fileLock.ReleaseWriterLock();
            //    Console.WriteLine("File lock released");
            //}
            Console.WriteLine("Done, returning ok");
            return Results.Ok(metadataObject.ResourceId);
        }
    }
}
