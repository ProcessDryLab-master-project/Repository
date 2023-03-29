using Microsoft.AspNetCore.Mvc;
using Repository.App;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Repository.Visualizers;

namespace Repository.Endpoints
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
                return Registration.GetConfiguration();
            });


            // ----------------- DATA ----------------- //
            // To save incomming files (.png, .xes, .bpmn, .pnml etc)
            app.MapPost("/resources", (HttpRequest request) =>
            {
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.SaveFile(request, appUrl);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            app.MapPost("/resources/metadata", (HttpRequest request) =>
            {
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.SaveMetadataOnly(request, appUrl);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            // To retrieve/output a list of available resources (metadata list)
            app.MapGet("/resources/metadata", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetResourceList();
            });
            // To retrieve metadata object for given resourceId
            app.MapGet("/resources/metadata/{resourceId}", (string resourceId) =>
            {
                return DBManager.GetMetadataObjectStringById(resourceId);
            });

            // To retrieve/output a list of available Visualization resources
            app.MapPost("/resources/metadata/filters", (HttpRequest request) =>
            {
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
                return ResourceRetriever.GetResourceById(resourceId);
            });
            // To retrieve graph for given resourceId
            app.MapGet("/resources/graphs/{resourceId}", (string resourceId) =>
            {
                return ResourceConnector.GetGraphForResource(resourceId);
            });
            // To retrieve histogram for given resourceId
            app.MapGet("/resources/histograms/{resourceId}", (string resourceId) =>
            {
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return HistogramGenerator.GetHistogram(resourceId, appUrl);
            });

        }
    }
}
