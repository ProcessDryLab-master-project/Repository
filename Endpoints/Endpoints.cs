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
                return ResourceReceiver.SaveResource(request);
            })
            //.Accepts<IFormFile>("multipart/form-data")
            .Produces(200);

            // To save a stream in Metadata:
            app.MapPost("/resources/streams", (HttpRequest request) =>
            {
                return ResourceReceiver.SaveResource(request);
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

            // To retrieve/output model representation (.bpmn, png etc) for the frontend
            app.MapGet("/resources/files/{resourceId}", (string resourceId) =>
            {
                return ResourceRetriever.GetResourceById(resourceId);
            });            
            // To retrieve metadata object for resource
            app.MapGet("/resources/info/{resourceId}", (string resourceId) =>
            {
                return DBManager.GetMetadataObjectById(resourceId);
            });
        }
    }
}
