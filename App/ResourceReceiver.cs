using Microsoft.VisualBasic.FileIO;

namespace Repository.App
{
    public class ResourceReceiver
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        public static IResult SaveResource(HttpRequest request)
        {
            if (!request.Form.Files.Any())
            {
                return Results.BadRequest("At least one file is needed");
            }
            string fileName = request.Form["fileName"].ToString();
            string resourceType = request.Form["ResourceType"].ToString(); 
            string fileExtension = request.Form["fileExtension"].ToString().Replace(".", "");
            string GUID = Guid.NewGuid().ToString();
            string? basedOnId = request.Form["basedOnId"];
            basedOnId = string.IsNullOrWhiteSpace(basedOnId) ? null : basedOnId.ToString();
            string? overwriteId = request.Form["overwriteId"];
            //overwriteId = string.IsNullOrWhiteSpace(overwriteId) ? null : overwriteId.ToString();

            foreach (var file in request.Form.Files) // TODO: Should only ever be one file. Maybe change code to better represent that.
            {
                string pathToFileExtension = DefaultFileMetadata(ref fileName, ref resourceType, ref fileExtension, file);

                if(!string.IsNullOrWhiteSpace(overwriteId)) GUID = overwriteId.ToString(); // If overwriteId is provided, save file as that.

                string nameToSaveFile = GUID + "." + fileExtension;

                string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);
                using var stream = new FileStream(pathToSaveFile, FileMode.Create);
                file.CopyTo(stream);

                DBManager.AddToMetadata(fileName, resourceType, fileExtension, GUID, basedOnId);
            }
            // Return ID
            return Results.Ok(GUID);
        }

        // This function is to write metadata based on the file that was sent, in case some metadata is missing.
        private static string DefaultFileMetadata(ref string fileName, ref string resourceType, ref string fileExtension, IFormFile file)
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
