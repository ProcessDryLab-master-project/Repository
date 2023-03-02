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
            //StreamReader body = new StreamReader(request.Body);
            //Console.WriteLine(body);
            foreach (var file in request.Form.Files)
            {
                string fileExtension = Path.GetExtension(file.FileName).Replace(".", "").ToUpper();
                string pathToFileType = "";
                // TODO: Change to read file type from body of request. This is just a temporary solution.
                if (fileExtension == "XES") // if (fileType = "EventLog")
                {
                    pathToFileType = Path.Combine(pathToResources, "EventLog"); // Cannot be "Log" as C# will ignore it
                }
                else // if (fileType = "Visualization")
                {
                    pathToFileType = Path.Combine(pathToResources, "Visualization");
                }
                string pathToFileExtension = Path.Combine(pathToFileType, fileExtension);
                if(!File.Exists(pathToFileExtension))
                {
                    Console.WriteLine("No folder exists for this file type, creating " + pathToFileExtension);
                    Directory.CreateDirectory(pathToFileExtension);
                }
                string pathToFile = Path.Combine(pathToFileExtension, file.FileName);
                using var stream = new FileStream(pathToFile, FileMode.Create);
                file.CopyTo(stream);

                //DBManager.AddToMetadata(fileName, fileType, fileExtension);
            }
            return Results.Ok("File upload successful");
        }
        private async Task<string> StreamToStringAsync(HttpRequest request)
        {
            using (var sr = new StreamReader(request.Body))
            {
                return await sr.ReadToEndAsync();
            }
        }
    }
}
