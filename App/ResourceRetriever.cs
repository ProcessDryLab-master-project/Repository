using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;

namespace Repository.App
{
    public class ResourceRetriever
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        public static IResult GetResourceList()
        {
            var resourceList = DBManager.GetMetadataAsList();
            var json = JsonConvert.SerializeObject(resourceList);
            return Results.Text(json, contentType: "application/json");
        }

        public static IResult GetResourceById(string resourceId)
        {
            MetadataObject metadataObject = DBManager.GetMetadataObjectById(resourceId);
            string pathToFileType = Path.Combine(pathToResources, metadataObject.FileType);
            //string fileExtension = Path.GetExtension(resourceId).Replace(".", "").ToUpper();
            string pathToFileExtension = Path.Combine(pathToFileType, metadataObject.FileExtension.ToUpper());
            string pathToFile = Path.Combine(pathToFileExtension, resourceId);
            if (!File.Exists(pathToFile))
            {
                string badResponse = "No such file exists for path " + pathToFile; // TODO: Should not return the entire path, just easier like this for now
                return Results.BadRequest(badResponse);
            }
            return Results.File(pathToFile, resourceId);
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


        #region OldFrontend
        // --------- FUNCTIONS FOR OLD FRONTEND --------- //
        public static IResult GetResourceListOld()
        {
            string[] files = Directory.GetFiles(pathToResources, "*.*", System.IO.SearchOption.AllDirectories);

            List<Dictionary<string, object>> resourceList = new();
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file); // To remove path, and only get file name
                BuildResourceObject(resourceList, fileName);
            }
            var json = JsonConvert.SerializeObject(resourceList);
            return Results.Text(json, contentType: "application/json");

            static void BuildResourceObject(List<Dictionary<string, object>> resourceList, string fileName)
            {
                resourceList.Add(new Dictionary<string, object>
                {
                    { "id", "id1" },
                    { "name", fileName },
                    { "type",  new Dictionary<string, object>
                        {
                            {
                                "name", Path.GetExtension(fileName).Replace(".", "")
                            },
                            {
                                "description", "Some file"
                            },
                            {
                                "visualizations", new List<Dictionary<string, string>>()
                                {
                                    new Dictionary<string, string>()
                                    {
                                        {"id","v1"},
                                        {"name","Vis 1"}
                                    },
                                    new Dictionary<string, string>()
                                    {
                                        {"id","v2"},
                                        {"name","Vis 2"}
                                    },

                                }
                            }
                        }
                    },
                    { "host", "https://localhost:4000" },
                    { "creationDate", "02-03-2023 10:26:29" },
                });
            }
        }


        // This is also an example of an async response and how to read body. Keep for inspiration even if we delete the endpoint.
        public static async Task<IResult> GetVisualizationById(HttpRequest request, string resourceId, string visualizationId)
        {
            // read  body from request:
            var body = new StreamReader(request.Body);
            string postData = await body.ReadToEndAsync();
            Console.WriteLine(postData);


            string fileType = Path.GetExtension(visualizationId).Replace(".", "").ToUpper();
            string pathToFileType = Path.Combine(pathToResources, fileType);
            string pathToFile = Path.Combine(pathToFileType, visualizationId);
            if (!File.Exists(pathToFile))
            {
                string badResponse = "No such file exists for path " + pathToFileType;
                return Results.BadRequest(badResponse);
            }
            return Results.File(pathToFile, visualizationId);
        }
        #endregion
    }
}
