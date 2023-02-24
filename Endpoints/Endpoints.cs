using Microsoft.AspNetCore.Mvc;
using Repository.App;
using System.Net;
using Repository.App;


namespace Repository.Endpoints
{
    public class Endpoints
    {
        public Endpoints(WebApplication app)
        {
            // ----------------- CONNECTION ----------------- //
            // To maintain connection
            app.MapGet("/ping", (HttpContext httpContext) =>
            {
                return "pong";
            })
            .WithName("Ping");

            // To retrieve configuration for the registrationprocess in ServiceRegistry
            app.MapGet("/configurations", (HttpContext httpContext) =>
            {
                return "Here is my stuff";
            })
            .WithName("GetConfiguration");


            // ----------------- DATA ----------------- //
            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/resources", (HttpRequest request) =>
            {
                return ResourceReceiver.SaveResource(request);
            })
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(200);
            //.WithName("SaveFile");

            // Alternate approach. You can 
            //app.MapPost("/resources", async (HttpRequest request) =>
            //{
            //    using (var reader = new StreamReader(request.Body, System.Text.Encoding.UTF8))
            //    {
            //        // Read the raw file as a `string`.
            //        string fileContent = await reader.ReadToEndAsync();
            //        // Do something with `fileContent`...
            //        return "File Was Processed Sucessfully!";
            //    }
            //}).Accepts<IFormFile>("text/plain");

            //[HttpGet]
            //public IActionResult Get()
            //{
            //    Byte[] b = System.IO.File.ReadAllBytes(@"E:\\Test.jpg");   // You can use your own method over here.         
            //    return File(b, "image/jpeg");
            //}

            // To retrieve a list of available resources
            app.MapGet("/resources", (HttpContext httpContext) =>
            {
                return "These resources are available:";
            })
            .WithName("GetAvailableResources");

            // To retrieve model representation (.bpmn, png etc) for the frontend
            app.MapGet("/resources/{resourceName}", (HttpContext httpContext, string resourceName) =>
            {
                string resource = ResourceRetriever.GetResource(resourceName);
                return resource;
            })
            .WithName("GetResourceByName");

            // Alternative way? For streaming?
            app.MapGet("/resources/stream/{resourceName}", HttpResponseMessage (string resourceName) =>
            {
                string localFilePath = ResourceRetriever.GetFilePath(resourceName);
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(new FileStream(localFilePath, FileMode.Open, FileAccess.Read));
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
                response.Content.Headers.ContentDisposition.FileName = resourceName;
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                return response;
            }).WithName("StreamResource");
        }
    }
}
