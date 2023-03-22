using Microsoft.AspNetCore.Mvc;
using Repository.App;
using System.Net;
using Repository.App;
using System.Reflection;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;

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
            app.MapPost("/resources/files", (HttpRequest request) =>
            {
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.SaveFile(request, appUrl);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            app.MapPost("/resources/info", (HttpRequest request) =>
            {
                var appUrl = app.Urls.FirstOrDefault(); // TODO: This isn't the cleanest way to get our own URL. Maybe change at some point.
                return ResourceReceiver.SaveMetadataOnly(request, appUrl);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            // To retrieve/output a list of available resources
            app.MapGet("/resources", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetResourceList();
            });

            // To retrieve/output a list of available Visualization resources
            app.MapGet("/resources/visualizations", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetVisualizationList();
            });

            // To retrieve/output a list of available EventLog resources
            app.MapGet("/resources/eventlogs", (HttpContext httpContext) =>
            {
                return ResourceRetriever.GetEventLogList();
            });

            // To retrieve file for given resourceId
            app.MapGet("/resources/files/{resourceId}", (string resourceId) =>
            {
                return ResourceRetriever.GetResourceById(resourceId);
            });
            // To retrieve metadata object for given resourceId
            app.MapGet("/resources/info/{resourceId}", (string resourceId) =>
            {
                return DBManager.GetMetadataObjectStringById(resourceId);
            });
            // To retrieve graph for given resourceId
            app.MapGet("/resources/graph/{resourceId}", (string resourceId) =>
            {
                return ResourceConnector.GetGraphForResource(resourceId);
            });

        }
    }
}
