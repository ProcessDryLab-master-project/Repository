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
        // TODO: Delete this dict and its uses before hand-in
        static Dictionary<string, int> numUpdates = new Dictionary<string, int>();
        //static DatabaseManager databaseManager = new DatabaseManager(new MetadataDb());
        //static ResourceManager resourceManager = new ResourceManager(new FileDb(), new MetadataDb());
        public Endpoints(WebApplication app)
        {
            //databaseManager.TrackAllDynamicResources();

            // TODO: Delete if we don't end up using MultiThreadFileWriter
            //var fileWriter = app.Services.GetRequiredService<MultiThreadFileWriter>();
            //fileWriter.WriteLine("yo", "c:\\myfile.txt");

            // Rate limiting is added to some endpoints only, as it will keep the program stable.
            // The endpoints without rate limiting, such as ping, are endpoints we don't expect to break anything from a large amount of requests, and rate limiting would only be to prevent ddos attacks.
            // If we add rate limiting to these endpoints, we need to add another policy that allows a higher request rate than the one used for files.
            // Metadata already has an internal queue for writing updates, which means rate limiting isn't required.
            // TODO: metadata queue only applies when writing to the file, not reading from. This hasn't been an issue so far, but maybe rate limiting on metadata read requests could be useful.
            var ratePolicy = "fixed";

            var _hostEnvironment = app.Environment;
            #region connection
            // To maintain connection
            app.MapGet("ping", (HttpContext httpContext) =>
            {
                //var appUrl = app.Urls.FirstOrDefault();
                //Console.WriteLine("Repo URL: " + appUrl);
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
            //.RequireRateLimiting(ratePolicy); // TODO: Find out if retrieving files without rate limiter can be an issue (especially with streaming)

            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/resources", (HttpContext context, ResourceManager manager) => {
                Console.WriteLine("Received POST request to save file");
                var appUrl = app.Urls.FirstOrDefault();
                var formObject = context.Request.Form.ToFormObject();

                if (formObject == null) return Results.BadRequest("Invalid FormData keys");
                if (formObject.File == null) return Results.BadRequest("Exactly one file is required");
                if (formObject.Host == null) formObject.Host = $"{appUrl}/resources/";
                return manager.PostFile(formObject, appUrl!);
            })
            .RequireRateLimiting(ratePolicy);

            app.MapPut("/resources/{resourceId}", (HttpContext context, string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received PUT request to update file with id: " + resourceId);

                // TODO: Delete the following section. Just to track number of updates and to print headers:
                if (numUpdates.ContainsKey(resourceId)) numUpdates[resourceId] += 1;
                else numUpdates[resourceId] = 1;
                Console.WriteLine($"Num updates for {resourceId} = {numUpdates[resourceId]}");

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

            app.MapPost("/resources/metadata", (HttpRequest request, ResourceManager manager) =>
            {
                Console.WriteLine("Received POST request to create metadata object without a file");
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.

                //request.EnableBuffering();
                //request.Body.Seek(0, SeekOrigin.Begin);
                //if (request.ContentLength == 0)
                //    return Results.BadRequest("Invalid request. Body must have form data.");

                var formObject = request.Form.ToFormObject();
                if (formObject == null) return Results.BadRequest("Invalid FormData keys");
                return manager.PostMetadata(formObject, appUrl!);
                //return manager.PostMetadata(request.Form, appUrl!);
            });

            app.MapPut("/resources/metadata/{resourceId}", (HttpRequest request, string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received PUT request to update metadata object without a file");
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.

                //request.EnableBuffering();
                //request.Body.Seek(0, SeekOrigin.Begin);
                //if (request.ContentLength == 0)
                //    return Results.BadRequest("Invalid request. Body must have form data.");

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
            app.MapGet("/resources/graphs/{resourceId}", (string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received GET request for relation graph on resource id: " + resourceId);
                return manager.GetGraphForResource(resourceId);
            });

            // To create/retrieve a histogram for an EventLog.
            app.MapPost("/resources/histograms/{resourceId}", (string resourceId, ResourceManager manager) =>
            {
                Console.WriteLine("Received POST request for histogram on resource id: " + resourceId);
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return manager.GetHistogram(resourceId, appUrl);
            });
            #endregion
        }
    }
}
