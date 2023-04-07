﻿using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Repository.App.Database;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;

namespace Repository.App.API
{
    public class ResourceRetriever
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static DatabaseManager databaseManager = new DatabaseManager(new FileDatabase());
        public static async Task<IResult> GetFilteredList(HttpRequest request)
        {
            var body = new StreamReader(request.Body);
            string bodyString = await body.ReadToEndAsync();
            Console.WriteLine(bodyString);

            bool validRequest = bodyString.TryParseJson(out List<string> filters);
            if (!validRequest || filters == null || filters.Count == 0) return Results.BadRequest($"Request body: {bodyString} is not a valid list");
            var resourceList = databaseManager.GetMetadataAsList();
            var filteredList = resourceList.Where(resource => filters.Any(filter => resource.ResourceInfo.ResourceType.Equals(filter, StringComparison.OrdinalIgnoreCase)));
            //var eventLogList = resourceList.Where(resource => !resource.ResourceInfo.ResourceType.Equals("EventLog", StringComparison.OrdinalIgnoreCase) && !!resource.ResourceInfo.ResourceType.Equals("EventStream", StringComparison.OrdinalIgnoreCase));
            var json = JsonConvert.SerializeObject(filteredList);
            return Results.Text(json, contentType: "application/json");
        }
        //public static IResult GetEventLogList()
        //{
        //    var resourceList = DBManager.GetMetadataAsList();
        //    var eventLogList = resourceList.Where(resource => resource.ResourceInfo.ResourceType.Equals("EventLog", StringComparison.OrdinalIgnoreCase));
        //    var json = JsonConvert.SerializeObject(eventLogList);
        //    return Results.Text(json, contentType: "application/json");
        //}
        public static IResult GetResourceList()
        {
            var resourceList = databaseManager.GetMetadataAsList();
            var json = JsonConvert.SerializeObject(resourceList);
            return Results.Text(json, contentType: "application/json");
        }
        public static IResult GetMetadataObjectStringById(string resourceId)
        {
            MetadataObject? metadataObject = databaseManager.GetMetadataObjectById(resourceId);
            if (metadataObject == null) return Results.BadRequest("No such object");
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataObject, Formatting.Indented);
            return Results.Text(updatedMetadataJsonString);
        }
        public static IResult GetResourceById(string resourceId)
        {
            MetadataObject? metadataObject = databaseManager.GetMetadataObjectById(resourceId);
            if (metadataObject == null) return Results.BadRequest("Invalid resource ID.");
            string pathToFileExtension = Path.Combine(pathToResources, metadataObject.ResourceInfo.FileExtension.ToUpper()); // TODO: Add null check or try/catch
            string pathToFile = Path.Combine(pathToFileExtension, resourceId + "." + metadataObject.ResourceInfo.FileExtension);
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
            string resourceType = Path.GetExtension(resourceName).Replace(".", "").ToUpper();
            string pathToResourceType = Path.Combine(pathToResources, resourceType);
            string pathToFile = Path.Combine(pathToResourceType, resourceName);
            if (!File.Exists(pathToFile))
            {
                string badResponse = "No such file exists for path " + pathToResourceType;
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