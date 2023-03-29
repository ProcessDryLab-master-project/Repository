using Microsoft.VisualBasic.FileIO;

namespace Repository.App
{
    public class ResourceReceiver
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        public static IResult SaveFile(HttpRequest request, string appUrl)
        {
            string resourceLabel = request.Form["ResourceLabel"].ToString();
            string resourceType = request.Form["ResourceType"].ToString();
            string description = request.Form["Description"].ToString();
            string? fileExtension = request.Form["FileExtension"];
            fileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.ToString().Replace(".", "");
            string GUID = Guid.NewGuid().ToString();
            string? generatedFrom = request.Form["GeneratedFrom"];
            string? parents = request.Form["Parents"];
            string? children = request.Form["Children"];
            generatedFrom = string.IsNullOrWhiteSpace(generatedFrom) ? null : generatedFrom.ToString();
            parents = string.IsNullOrWhiteSpace(parents) ? null : parents.ToString();
            children = string.IsNullOrWhiteSpace(children) ? null : children.ToString();
            string? overwriteId = request.Form["OverwriteId"];
            if (!string.IsNullOrWhiteSpace(overwriteId)) GUID = overwriteId.ToString(); // If overwriteId is provided, save file as that.

            if (!request.Form.Files.Any())
            {
                return Results.BadRequest("Exactly one file is required");
            }
            var file = request.Form.Files[0];
            string host = $"{appUrl}/resources/";
            string pathToFileExtension = DefaultFileMetadata(ref resourceLabel, ref resourceType, ref fileExtension, file);
            string nameToSaveFile = GUID + "." + fileExtension;
            string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);
            using var stream = new FileStream(pathToSaveFile, FileMode.Create);
            file.CopyTo(stream);

            DBManager.AddToMetadata(resourceLabel, resourceType, GUID, host, description, fileExtension, generatedFrom: generatedFrom, parents: parents, children: children);

            Console.WriteLine($"Saved file: {nameToSaveFile}");
            return Results.Ok(GUID);
        }

        // Assuming this is only relevant for streaming?
        public static IResult SaveMetadataOnly(HttpRequest request, string appUrl)
        {
            string resourceLabel = request.Form["ResourceLabel"].ToString();
            string resourceType = request.Form["ResourceType"].ToString();
            //if (resourceType != "EventStream") return Results.BadRequest("On-ly ResourceType: EventStream can be added to metadata like this");

            string description = request.Form["Description"].ToString();
            string GUID = Guid.NewGuid().ToString();
            string? streamTopic = request.Form["StreamTopic"];
            streamTopic = string.IsNullOrWhiteSpace(streamTopic) ? null : streamTopic.ToString();
            string? fileExtension = request.Form["FileExtension"];
            fileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.ToString().Replace(".", "");
            string? host = request.Form["Host"];
            host = string.IsNullOrWhiteSpace(host) ? $"{appUrl}/resources/" : host.ToString(); // Host is only NOT null when adding streams. Otherwise it should always be null.
            string? parents = request.Form["Parents"];
            parents = string.IsNullOrWhiteSpace(parents) ? null : parents.ToString();
            string? children = request.Form["Children"];
            children = string.IsNullOrWhiteSpace(children) ? null : children.ToString();
            string? overwriteId = request.Form["OverwriteId"];
            if (!string.IsNullOrWhiteSpace(overwriteId)) GUID = overwriteId.ToString(); // If overwriteId is provided, save file as that.

            DBManager.AddToMetadata(resourceLabel, resourceType, GUID, host, description, fileExtension: fileExtension, streamTopic: streamTopic, parents: parents, children: children);
            return Results.Ok(GUID);
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
