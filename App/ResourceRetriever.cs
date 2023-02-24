using System.Net;
using System.Reflection;

namespace Repository.App
{
    public class ResourceRetriever
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        public static IResult GetResourceByName(string resourceName)
        {
            string fileType = Path.GetExtension(resourceName).Replace(".", "").ToUpper();
            string pathToFileType = Path.Combine(pathToResources, fileType);
            string pathToFile = Path.Combine(pathToFileType, resourceName);
            if (!File.Exists(pathToFile))
            {
                string badResponse = "No such file exists for path " + pathToFileType;
                return Results.BadRequest(badResponse);
            }
            return Results.File(pathToFile, resourceName);
        }

        // Alternative way. Might be better for streaming a file
        public static HttpResponseMessage StreamResponse(string resourceName)
        {
            string fileType = Path.GetExtension(resourceName).Replace(".", "").ToUpper();
            string pathToFileType = Path.Combine(pathToResources, fileType);
            string pathToFile = Path.Combine(pathToFileType, resourceName);
            if (!File.Exists(pathToFile))
            {
                string badResponse = "No such file exists for path " + pathToFileType;
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(new FileStream(pathToFile, FileMode.Open, FileAccess.Read));
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = resourceName;
            //response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xes");

            return response;
        }
    }
}
