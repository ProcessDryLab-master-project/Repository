using Microsoft.AspNetCore.Mvc;
using Repository.App;
using System.Net;
using Repository.App;
using System.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;

namespace Repository.Endpoints
{
    public class Endpoints
    {
        public Endpoints(WebApplication app)
        {
            // ----------------- CONNECTION ----------------- //
            // To maintain connection
            app.MapGet("/api/v1/system/ping", (HttpContext httpContext) =>
            {
                return "pong";
            })
            .WithName("Ping");

            // To retrieve configuration for the registrationprocess in ServiceRegistry
            app.MapGet("/configurations", (HttpContext httpContext) =>
            {
                return Registration.GetConfiguration();
            })
            .WithName("GetConfiguration");


            // ----------------- DATA ----------------- //
            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/api/v1/resources", (HttpRequest request) =>
            {
                return ResourceReceiver.SaveResource(request);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            // Alternate approach to save incomming files. You can send any content type as string 
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


            // To retrieve/output a list of available resources
            app.MapGet("/api/v1/resources", (HttpContext httpContext) =>
            {
                //return "These resources are available:";
                return new JArray();
            })
            .WithName("GetAvailableResources");

            // To retrieve/output model representation (.bpmn, png etc) for the frontend
            app.MapGet("/api/v1/resources/{resourceName}", (string resourceName) =>
            {
                return ResourceRetriever.GetResourceByName(resourceName);
            })
            .WithName("GetResourceByName");

            // Alternative way? For streaming?
            app.MapGet("/api/v1/resources/stream/{resourceName}", HttpResponseMessage (string resourceName) =>
            {
                return ResourceRetriever.StreamResponse(resourceName);
            }).WithName("StreamResource");
        }
    }
}
