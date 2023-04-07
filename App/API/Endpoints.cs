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
            var _hostEnvironment = app.Environment;
            // ----------------- CONNECTION ----------------- //
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


            // ----------------- DATA ----------------- //
            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/resources", (HttpRequest request) =>
            {
                Console.WriteLine("Received POST request to save file");
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.SaveFile(request, appUrl);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            app.MapPost("/resources/metadata", (HttpRequest request) =>
            {
                Console.WriteLine("Received POST request to create metadata object without a file");
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.SaveMetadataOnly(request, appUrl);
            })
            .Produces(200);

            app.MapPut("/resources/metadata/{resourceId}", (HttpRequest request, string resourceId) =>
            {
                Console.WriteLine("Received PUT request to update metadata object without a file");
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.UpdateMetadata(request, appUrl, resourceId);
            })
            .Produces(200);

            // To retrieve/output a list of available resources (metadata list)
            app.MapGet("/resources/metadata", (HttpContext httpContext) =>
            {
                Console.WriteLine("Received GET request for full metadata list");
                return ResourceRetriever.GetResourceList();
            });
            // To retrieve metadata object for given resourceId
            app.MapGet("/resources/metadata/{resourceId}", (string resourceId) =>
            {
                Console.WriteLine("Received GET request for metadata object on resource id: " + resourceId);
                return ResourceRetriever.GetMetadataObjectStringById(resourceId);
            });

            // To retrieve children for given resourceId
            app.MapGet("/resources/metadata/{resourceId}/children", (string resourceId) =>
            {
                Console.WriteLine("Received GET request for list of children metadata on resource id: " + resourceId);
                return FileDatabase.GetChildrenMetadataList(resourceId);
            });

            // To retrieve/output a list of available Visualization resources
            app.MapPost("/resources/metadata/filters", (HttpRequest request) =>
            {
                Console.WriteLine("Received POST request to get a filtered list of metadata objects");
                return ResourceRetriever.GetFilteredList(request);
            });

            //// To retrieve/output a list of available EventLog resources
            //app.MapGet("/resources/eventlogs", (HttpContext httpContext) =>
            //{
            //    return ResourceRetriever.GetEventLogList();
            //});

            // To retrieve file for given resourceId
            app.MapGet("/resources/{resourceId}", (string resourceId) =>
            {
                Console.WriteLine("Received GET request for file on resource id: " + resourceId);
                return ResourceRetriever.GetResourceById(resourceId);
            });

            // To retrieve graph for given resourceId
            app.MapGet("/resources/graphs/{resourceId}", (string resourceId) =>
            {
                Console.WriteLine("Received GET request for relation graph on resource id: " + resourceId);
                return ResourceConnector.GetGraphForResource(resourceId);
            });

            // To retrieve histogram for given resourceId
            app.MapPost("/resources/histograms/{resourceId}", (string resourceId) =>
            {
                Console.WriteLine("Received POST request for histogram on resource id: " + resourceId);
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return HistogramGenerator.GetHistogram(resourceId, appUrl);
            });

        }
    }
}
