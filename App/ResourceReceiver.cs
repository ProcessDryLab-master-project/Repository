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
            foreach (var file in request.Form.Files)
            {
                string fileType = Path.GetExtension(file.FileName).Replace(".", "").ToUpper();
                string pathToFileType = Path.Combine(pathToResources, fileType);
                if(!File.Exists(pathToFileType))
                {
                    Console.WriteLine("No folder exists for this file type, creating " + pathToFileType);
                    Directory.CreateDirectory(pathToFileType);
                }
                string pathToFile = Path.Combine(pathToFileType, file.FileName);
                using var stream = new FileStream(pathToFile, FileMode.Create);
                file.CopyTo(stream);
            }
            return Results.Ok("File upload successful");
        }
    }
}
