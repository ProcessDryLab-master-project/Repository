using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Repository.App.Database;
using Repository.App.Visualizers;

namespace Repository.App.API
{
    public class Endpoints
    {
        public Endpoints(WebApplication app)
        {
            var ratePolicy = "fixed";

            var _hostEnvironment = app.Environment;
            #region connection
            // To maintain connection
            app.MapGet("ping", (HttpContext httpContext) =>
            {
                return "pong";
            });

            // To retrieve configuration for the registrationprocess in ServiceRegistry
            app.MapGet("/configurations", (HttpContext httpContext) =>
            {
                Console.WriteLine("Received GET request for configurations");
                return Registration.GetConfiguration();
            });
            #endregion

            #region files
            // To retrieve file for given resourceId
            app.MapGet("/resources/{resourceId}", (string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received GET request for file on resource id: " + resourceId);
                return manager.GetFileById(resourceId);
            });

            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/resources", (HttpContext context, ResourceManager manager) => {
                Console.WriteLine("Received POST request to save file");
                var request = context.Request;
                //var appUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}"; // Full URL with path and everything
                var appUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                var formObject = context.Request.Form.ToFormObject(appUrl);

                if (formObject == null) return Results.BadRequest("Invalid FormData keys");
                if (formObject.File == null) return Results.BadRequest("Exactly one file is required");
                if (formObject.Host == null) formObject.Host = $"{appUrl}/resources/";
                return manager.PostFile(formObject);
            })
            .RequireRateLimiting(ratePolicy);

            app.MapPut("/resources/{resourceId}", (HttpContext context, string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received PUT request to update file with id: " + resourceId);
                var requestFiles = context.Request.Form.Files;
                if (requestFiles?.Count != 1) return Results.BadRequest("Exactly one file is required");
                var formFile = requestFiles.Single();
                byte[] file = DbHelper.FileToByteArr(formFile)!;
                return manager.UpdateFile(file, resourceId);
            })
            .RequireRateLimiting(ratePolicy);
            #endregion

            #region metadata
            // To retrieve/output a list of available resources (metadata list)
            app.MapGet("/resources/metadata", (HttpContext httpContext, ResourceManager manager) =>
            {
                Console.WriteLine("Received GET request for full metadata list");
                return manager.GetResourceList();
            });

            // To retrieve metadata object for given resourceId
            app.MapGet("/resources/metadata/{resourceId}", (string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received GET request for metadata object on resource id: " + resourceId);
                return manager.GetMetadataObjectStringById(resourceId);
            });

            // To retrieve children for given resourceId
            app.MapGet("/resources/metadata/{resourceId}/children", (string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received GET request for list of children metadata on resource id: " + resourceId);
                return manager.GetChildrenMetadataList(resourceId);
            });

            // To save metadata object without a file
            app.MapPost("/resources/metadata", (HttpRequest request, ResourceManager manager) =>
            {
                Console.WriteLine("Received POST request to create metadata object without a file");
                var appUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var formObject = request.Form.ToFormObject();
                if (formObject == null) return Results.BadRequest("Invalid FormData keys");
                return manager.PostMetadata(formObject, appUrl!);
            });

            // To update metadata object only
            app.MapPut("/resources/metadata/{resourceId}", (HttpRequest request, string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received PUT request to update metadata object without a file");
                var appUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                return manager.UpdateMetadataObject(request.Form, appUrl!, resourceId);
            });

            // To retrieve/output a list of available Visualization resources
            app.MapPost("/resources/metadata/filters", async (HttpRequest request, ResourceManager manager) =>
            {
                Console.WriteLine("Received POST request to get a filtered list of metadata objects");
                var body = new StreamReader(request.Body);
                string bodyString = await body.ReadToEndAsync();
                return manager.GetFilteredList(bodyString);
            });
            #endregion
            
            #region visualizers
            // To retrieve graph for given resourceId
            app.MapGet("/resources/graphs/{resourceId}", (HttpRequest request, string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received GET request for relation graph on resource id: " + resourceId);
                return manager.GetGraphForResource(resourceId);
            });

            // To create/retrieve a histogram for an EventLog.
            app.MapPost("/resources/histograms/{resourceId}", (HttpRequest request, string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received POST request for histogram on resource id: " + resourceId);
                //var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                var appUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
                return manager.GetHistogram(resourceId, appUrl);
            });
            #endregion
        }
    }
}
