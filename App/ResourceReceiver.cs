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
            string fileType = request.Form["fileType"].ToString(); 
            string fileExtension = request.Form["fileExtension"].ToString().Replace(".", "");
            string GUID = Guid.NewGuid().ToString();
            string? basedOnId = request.Form["basedOnId"];
            basedOnId = string.IsNullOrWhiteSpace(basedOnId) ? null : basedOnId.ToString();

            foreach (var file in request.Form.Files)
            {
                string pathToFileExtension = DefaultFileMetadata(ref fileName, ref fileType, ref fileExtension, file);
                string nameToSaveFile = GUID + "." + fileExtension;

                string pathToSaveFile = Path.Combine(pathToFileExtension, nameToSaveFile);
                using var stream = new FileStream(pathToSaveFile, FileMode.Create);
                file.CopyTo(stream);

                DBManager.AddToMetadata(fileName, fileType, fileExtension, GUID, basedOnId);
            }
            // Return ID
            return Results.Ok(GUID);
        }

        // This function is to write metadata based on the file that was sent, in case some metadata is missing.
        private static string DefaultFileMetadata(ref string fileName, ref string fileType, ref string fileExtension, IFormFile file)
        {
            string pathToFileType;
            if (string.IsNullOrWhiteSpace(fileName)) fileName = file.FileName;
            if (string.IsNullOrWhiteSpace(fileExtension)) fileExtension = Path.GetExtension(file.FileName).Replace(".", "");
            if (string.IsNullOrWhiteSpace(fileType))
            {
                if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
                {
                    pathToFileType = Path.Combine(pathToResources, "EventLog"); // Cannot be "Log" as C# will ignore it
                }
                else
                {
                    pathToFileType = Path.Combine(pathToResources, "Visualization");
                }
            }
            else
            {
                pathToFileType = Path.Combine(pathToResources, fileType);
            }
            string pathToFileExtension = Path.Combine(pathToFileType, fileExtension.ToUpper());
            if (!File.Exists(pathToFileExtension))
            {
                Console.WriteLine("No folder exists for this file type, creating " + pathToFileExtension);
                Directory.CreateDirectory(pathToFileExtension);
            }

            return pathToFileExtension;
        }

    }
}
