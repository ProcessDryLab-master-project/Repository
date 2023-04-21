using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Repository.App.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace Repository.App.API
{
    public class ResourceReceiver
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static DatabaseManager databaseManager = new DatabaseManager(new FileDatabase());

        //TODO: fix error: 'Unexpected end of Stream, the content may have already been read by another component. '
        // Potential solution to this and to reading file type: https://github.com/dotnet/AspNetCore.Docs/blob/5f362035992cc3b997903dda521a01ed59058dec/aspnetcore/mvc/models/file-uploads/samples/5.x/LargeFilesSample/Controllers/FileUploadController.cs#L49
        // Another post with a lot of useful information: https://stackoverflow.com/questions/63403921/process-incoming-filestream-asynchronously
        public static IResult SaveFile(HttpRequest request, string appUrl)
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            if (request.ContentLength == 0) 
                return Results.BadRequest("Invalid request. Body must have form data.");

            string resourceId = Guid.NewGuid().ToString();
            string resourceLabel = request.Form["ResourceLabel"].ToString();
            string resourceType = request.Form["ResourceType"].ToString();
            string description = request.Form["Description"].ToString();
            string? fileExtension = request.Form["FileExtension"];
            fileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.ToString().Replace(".", "");
            string? generatedFrom = request.Form["GeneratedFrom"];
            string? parents = request.Form["Parents"];
            generatedFrom = string.IsNullOrWhiteSpace(generatedFrom) ? null : generatedFrom.ToString();
            parents = string.IsNullOrWhiteSpace(parents) ? null : parents.ToString();
            string? isDynamicString = request.Form["Dynamic"];
            bool isDynamic = string.Equals(isDynamicString, "true", StringComparison.OrdinalIgnoreCase);

            var requestFile = request.Form.Files;
            if (!requestFile.Any()) return Results.BadRequest("Exactly one file is required");
            var file = requestFile[0];

            if (isDynamic)
            {
                databaseManager.UpdateDynamicResource(resourceId);
            }

            string host = $"{appUrl}/resources/";
            string pathToFileExtension = DefaultFileMetadata(ref resourceLabel, ref resourceType, ref fileExtension, file);
            string nameToSaveFile = resourceId + "." + fileExtension;
            string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);
            if (File.Exists(pathToSaveFile)) return Results.BadRequest("File with that ID already exists. This should not be possible. Did you mean PUT?");


            bool providedParents = parents.TryParseJson(out List<Parent> parentsList);
            bool providedFromSource = generatedFrom.TryParseJson(out GeneratedFrom generatedFromObj);
            databaseManager.BuildAndAddMetadataObject(resourceId, resourceLabel, resourceType, host, description, fileExtension, null, generatedFromObj, parentsList, isDynamic);

            lock (Globals.FileAccessLock) // Lock added, which should queue writes from multiple threads.
            {
                //// Alternate way of saving file?
                //using var stream = new FileStream(pathToSaveFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite); // TODO: Consider if FileShare.ReadWrite makes sense
                //file.CopyTo(stream);
                using (var fileStream = File.Open(pathToSaveFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.SetLength(0); // truncate the file
                    file.CopyTo(fileStream); // write to the file
                    Console.WriteLine($"Saved file: {nameToSaveFile}");
                    return Results.Ok(resourceId); // TODO: Beware that we're returning resourceId, before we know that the metadata file has been updated. The update to metadata is being put on a queue, which we currently can't return anything from. 
                }
            }            
        }

        // TODO: Consider that people can potentially update files with a new file type, since no file extension is specified.
        // Consider implementing this to check file types: https://stackoverflow.com/questions/11547654/determine-the-file-type-using-c-sharp
        public static IResult UpdateFile(HttpRequest request, string resourceId)
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            if (request.ContentLength == 0) 
                return Results.BadRequest("Invalid request. Body must have form data.");

            MetadataObject? metadataObject = databaseManager.GetMetadataObjectById(resourceId);
            if (metadataObject == null) return Results.BadRequest("No metadata object exist for resourceId: " + resourceId);
            if (!metadataObject.ResourceInfo.Dynamic) return Results.BadRequest("You can only update dynamic resources. Invalid request for resourceId: " + resourceId);
            try
            {
                if (request == null) 
                    Console.WriteLine("Request is null somehow?");
                if (request.Form == null)
                    Console.WriteLine("Request.Form is null somehow?");

                var requestFile = request.Form.Files;
                if (!requestFile.Any()) return Results.BadRequest("Exactly one file is required");
                var file = requestFile[0];
                string fileExtension = metadataObject.ResourceInfo.FileExtension!;
                string pathToFileExtension = Path.Combine(pathToResources, fileExtension.ToUpper());
                string nameToSaveFile = resourceId + "." + fileExtension;
                string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);

                databaseManager.UpdateDynamicResource(resourceId);
                // TODO: error: The process cannot access the file because it is being used by another process.
                // Can sometimes write to this multiple times at the same time. It doesn't seem related to reading at the same time, as it can happen with POST requests alone.
                lock (Globals.FileAccessLock) // Lock added, which should queue writes from multiple threads.
                {
                    //using var stream = new FileStream(pathToSaveFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite); // TODO: Consider if FileShare.ReadWrite makes sense
                    //file.CopyTo(stream);
                    using (var fileStream = File.Open(pathToSaveFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        fileStream.SetLength(0); // truncate the file
                        file.CopyTo(fileStream); // write to the file
                    }
                }
                Console.WriteLine($"Updated file: {nameToSaveFile}");
                return Results.Ok(resourceId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Results.Problem(ex.ToString());
            }
        }

        // Assuming this is only relevant for streaming?
        public static IResult SaveMetadataOnly(HttpRequest request, string appUrl)
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            if (request.ContentLength == 0) 
                return Results.BadRequest("Invalid request. Body must have form data.");

            string resourceId = Guid.NewGuid().ToString();
            string resourceLabel = request.Form["ResourceLabel"].ToString();
            string resourceType = request.Form["ResourceType"].ToString();
            string description = request.Form["Description"].ToString();
            string? streamTopic = request.Form["StreamTopic"];
            streamTopic = string.IsNullOrWhiteSpace(streamTopic) ? null : streamTopic.ToString();
            string? fileExtension = request.Form["FileExtension"];
            fileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.ToString().Replace(".", "");
            string? host = request.Form["Host"];
            host = string.IsNullOrWhiteSpace(host) ? $"{appUrl}/resources/" : host.ToString(); // Host is only NOT null when adding streams. Otherwise it should always be null.
            string? generatedFrom = request.Form["GeneratedFrom"];
            generatedFrom = string.IsNullOrWhiteSpace(generatedFrom) ? null : generatedFrom.ToString();
            string? parents = request.Form["Parents"];
            parents = string.IsNullOrWhiteSpace(parents) ? null : parents.ToString();

            bool providedParents = parents.TryParseJson(out List<Parent> parentsList);
            bool providedFromSource = generatedFrom.TryParseJson(out GeneratedFrom generatedFromObj);
            databaseManager.BuildAndAddMetadataObject(resourceId, resourceLabel, resourceType, host, description, fileExtension, streamTopic, generatedFromObj, parentsList);

            return Results.Ok(resourceId);
        }

        public static IResult UpdateMetadata(HttpRequest request, string appUrl, string resourceId)
        {
            request.EnableBuffering();
            request.Body.Seek(0, SeekOrigin.Begin);
            if (request.ContentLength == 0) 
                return Results.BadRequest("Invalid request. Body must have form data.");

            var formAsDict = request.Form.ToDictionary();
            return databaseManager.UpdateMetadataObject(formAsDict, resourceId);
            //return Results.Ok(resourceId);
        }

        // This function is to write metadata based on the file that was sent, in case some metadata is missing.
        private static string DefaultFileMetadata(ref string fileName, ref string resourceType, ref string? fileExtension, IFormFile file)
        {
            string pathToResourceType;
            if (string.IsNullOrWhiteSpace(fileName)) fileName = file.FileName;
            if (string.IsNullOrWhiteSpace(fileExtension)) fileExtension = Path.GetExtension(file.FileName).Replace(".", "");
            string pathToFileExtension = Path.Combine(pathToResources, fileExtension.ToUpper());
            if (!Directory.Exists(pathToFileExtension))
            {
                Console.WriteLine("No folder exists for this file type, creating " + pathToFileExtension);
                Directory.CreateDirectory(pathToFileExtension);
            }

            return pathToFileExtension;
        }

    }
}
