using Microsoft.VisualBasic.FileIO;

namespace Repository.App
{
    public class ResourceReceiver
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        public static IResult SaveResource(HttpRequest request)
        {
            string resourceLabel = request.Form["resourceLabel"].ToString();
            string resourceType = request.Form["resourceType"].ToString(); 
            string? fileExtension = request.Form["fileExtension"];
            fileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.ToString().Replace(".", "");
            string GUID = Guid.NewGuid().ToString();
            string? parents = request.Form["parents"];
            string? children = request.Form["children"];
            parents = string.IsNullOrWhiteSpace(parents) ? null : parents.ToString();
            children = string.IsNullOrWhiteSpace(children) ? null : children.ToString();
            string? overwriteId = request.Form["overwriteId"];
            if (!string.IsNullOrWhiteSpace(overwriteId)) GUID = overwriteId.ToString(); // If overwriteId is provided, save file as that.
            // Stream specific
            string? streamBroker = request.Form["streamBroker"];
            string? streamTopic = request.Form["streamTopic"];
            streamBroker = string.IsNullOrWhiteSpace(streamBroker) ? null : streamBroker.ToString();
            streamTopic = string.IsNullOrWhiteSpace(streamTopic) ? null : streamTopic.ToString();

            if(resourceType != "EventStream")
            {
                if (!request.Form.Files.Any())
                {
                    return Results.BadRequest("Exactly one file is required");
                }
                var file = request.Form.Files[0];
                string pathToFileExtension = DefaultFileMetadata(ref resourceLabel, ref resourceType, ref fileExtension, file);
                string nameToSaveFile = GUID + "." + fileExtension;
                string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);
                using var stream = new FileStream(pathToSaveFile, FileMode.Create);
                file.CopyTo(stream);
            }

            DBManager.AddToMetadata(resourceLabel, resourceType, GUID, fileExtension, streamBroker, streamTopic, parents, children);
            return Results.Ok(GUID);
        }

        // This function is to write metadata based on the file that was sent, in case some metadata is missing.
        private static string DefaultFileMetadata(ref string fileName, ref string resourceType, ref string? fileExtension, IFormFile file)
        {
            string pathToResourceType;
            if (string.IsNullOrWhiteSpace(fileName)) fileName = file.FileName;
            if (string.IsNullOrWhiteSpace(fileExtension)) fileExtension = Path.GetExtension(file.FileName).Replace(".", "");
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
                {
                    pathToResourceType = Path.Combine(pathToResources, "EventLog"); // Cannot be "Log" as C# will ignore it
                }
                else
                {
                    pathToResourceType = Path.Combine(pathToResources, "Visualization");
                }
            }
            else
            {
                pathToResourceType = Path.Combine(pathToResources, resourceType);
            }
            string pathToFileExtension = Path.Combine(pathToResourceType, fileExtension.ToUpper());
            if (!File.Exists(pathToFileExtension))
            {
                Console.WriteLine("No folder exists for this file type, creating " + pathToFileExtension);
                Directory.CreateDirectory(pathToFileExtension);
            }

            return pathToFileExtension;
        }

    }
}
