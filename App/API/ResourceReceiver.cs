using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Repository.App.Database;
using System;
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
        public static IResult SaveFile(HttpRequest request, string appUrl)
        {
            string resourceLabel = request.Form["ResourceLabel"].ToString();
            string resourceType = request.Form["ResourceType"].ToString();
            string description = request.Form["Description"].ToString();
            string? fileExtension = request.Form["FileExtension"];
            fileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.ToString().Replace(".", "");

            string? overwriteId = request.Form["OverwriteId"];
            string GUID = string.IsNullOrWhiteSpace(overwriteId) ? Guid.NewGuid().ToString() : overwriteId.ToString();
            //string GUID = Guid.NewGuid().ToString();
            //if (!string.IsNullOrWhiteSpace(overwriteId)) GUID = overwriteId.ToString(); // If overwriteId is provided, save file as that.

            string? generatedFrom = request.Form["GeneratedFrom"];
            string? parents = request.Form["Parents"];
            generatedFrom = string.IsNullOrWhiteSpace(generatedFrom) ? null : generatedFrom.ToString();
            parents = string.IsNullOrWhiteSpace(parents) ? null : parents.ToString();
            string? isDynamicString = request.Form["Dynamic"];
            bool isDynamic = string.Equals(isDynamicString, "true", StringComparison.OrdinalIgnoreCase);

            if (!request.Form.Files.Any())
            {
                return Results.BadRequest("Exactly one file is required");
            }
            var file = request.Form.Files[0];
            string host = $"{appUrl}/resources/";
            string pathToFileExtension = DefaultFileMetadata(ref resourceLabel, ref resourceType, ref fileExtension, file);
            string nameToSaveFile = GUID + "." + fileExtension;
            string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);

            //databaseManager.WriteToFile(pathToSaveFile, file);
            // TODO: error: The process cannot access the file because it is being used by another process.
            // Can sometimes write to this multiple times at the same time. It doesn't seem related to reading at the same time, as it can happen with POST requests alone.
            using (var fileStream = File.Open(pathToSaveFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                // read from the file ????
                fileStream.SetLength(0); // truncate the file
                // write to the file
                file.CopyTo(fileStream);
            }

            //using var stream = new FileStream(pathToSaveFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite); // TODO: Consider if FileShare.ReadWrite makes sense
            //file.CopyTo(stream);

            bool providedParents = parents.TryParseJson(out List<Parent> parentsList);
            bool providedFromSource = generatedFrom.TryParseJson(out GeneratedFrom generatedFromObj);
            databaseManager.BuildAndAddMetadataObject(GUID, resourceLabel, resourceType, host, description, fileExtension, null, generatedFromObj, parentsList, isDynamic);

            Console.WriteLine($"Saved file: {nameToSaveFile}");
            return Results.Ok(GUID);
        }

        // Assuming this is only relevant for streaming?
        public static IResult SaveMetadataOnly(HttpRequest request, string appUrl)
        {
            string? overwriteId = request.Form["OverwriteId"];
            string GUID = string.IsNullOrWhiteSpace(overwriteId) ? Guid.NewGuid().ToString() : overwriteId.ToString();
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
            databaseManager.BuildAndAddMetadataObject(GUID, resourceLabel, resourceType, host, description, fileExtension, streamTopic, generatedFromObj, parentsList);

            return Results.Ok(GUID);
        }

        public static IResult UpdateMetadata(HttpRequest request, string appUrl, string resourceId)
        {
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
