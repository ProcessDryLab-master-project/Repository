using csdot.Attributes.Types;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Repository.App.Entities;
using Repository.App.Visualizers;
using System.Collections.Generic;
using System.Collections.Specialized;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using csdot;

namespace Repository.App.Database
{
    public class ResourceManager
    {
        IFileDb fileDb { get; set; }
        IMetadataDb metadataDb { get; set; }
        public ResourceManager(IFileDb fileInterface, IMetadataDb dataInterface)
        {
            fileDb = fileInterface;
            metadataDb = dataInterface;
        }
        #region SETTERS
        // Files
        public IResult PostFile(FormObject formObject)
        {
            try
            {
                //var requestFiles = formData.Files;
                //if (requestFiles.Count != 1) return Results.BadRequest("Exactly one file is required");
                //var formFile = requestFiles.Single();

                //var formDataObj = formData.ToDictionary();
                //formDataObj = DbHelper.ValidateFormData(formDataObj, appUrl);
                var metadataObject = DbHelper.BuildMetadataObject(formObject);
                metadataDb.MetadataWrite(metadataObject);
                if(metadataObject.ResourceInfo.Dynamic) metadataDb.UpdateDynamicResourceTime(metadataObject.ResourceId);
                //byte[] file = DbHelper.FileToByteArr(formFile);
                return fileDb.WriteFile(metadataObject, formObject.File!);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        public IResult UpdateFile(byte[] file, string resourceId)
        {
            try
            {
                MetadataObject metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) 
                    return Results.BadRequest("No resource with that ID");
                metadataDb.UpdateDynamicResourceTime(resourceId); // TODO: Make MetadataWrite async and write await?

                //byte[] file = DbHelper.FileToByteArr(formFile);
                return fileDb.WriteFile(metadataObject, file);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        // Metadata specific
        //public IResult PostMetadata(IFormCollection formData, string appUrl)
        //{
        //    try
        //    {
        //        var formDataObj = formData.ToDictionary();
        //        formDataObj = DbHelper.ValidateFormData(formDataObj, appUrl);
        //        if (formDataObj == null) return Results.BadRequest("Invalid FormData keys");
        //        string? resourceId = null;
        //        MetadataObject? metadataObject;
        //        if (formDataObj["ResourceType"] == "EventStream")
        //        {
        //            bool overwrite = !string.Equals(formDataObj["Overwrite"], "true", StringComparison.OrdinalIgnoreCase);
        //            var metadataList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
        //            var existingMetadata = metadataList.Find(metadata => metadata.ResourceInfo.Host == formDataObj["Host"] && metadata.ResourceInfo.StreamTopic == formDataObj["StreamTopic"]);
        //            if (existingMetadata != null)
        //            {
        //                if(!overwrite) // If a similar metadata object exists and no Overwrite key has been sent, don't add a new one.
        //                    return Results.BadRequest("An EventStream for that StreamTopic and Host already exist. Use the Overwrite key if you wish to change it.");

        //                resourceId = existingMetadata.ResourceId!; // If we're overwriting a metadata object, save that key for when it's built
        //            }
        //        }

        //        metadataObject = DbHelper.BuildMetadataObject(formDataObj, resourceId);
        //        resourceId = metadataObject.ResourceId!; // Saving generated resource ID so we can return it, since it's removed before writing to metadata file

        //        metadataDb.MetadataWrite(metadataObject); // TODO: Make MetadataWrite async and write await?
        //        return Results.Ok(resourceId);
        //    }
        //    catch (Exception e)
        //    {
        //        return Results.BadRequest(e);
        //    }
        //}
        public IResult PostMetadata(FormObject formObject, string appUrl)
        {
            try
            {
                string? resourceId = null;
                if (formObject.ResourceType == "EventStream")
                {
                    bool overwrite = formObject.Overwrite;
                    var metadataList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
                    var existingMetadata = metadataList.Find(metadata => metadata.ResourceInfo.Host == formObject.Host && metadata.ResourceInfo.StreamTopic == formObject.StreamTopic);
                    if (existingMetadata != null)
                    {
                        if (!overwrite) // If a similar metadata object exists and no Overwrite key has been sent, don't add a new one.
                            return Results.BadRequest("An EventStream for that StreamTopic and Host already exist. Use the Overwrite key if you wish to change it.");

                        resourceId = existingMetadata.ResourceId!; // If we're overwriting a metadata object, save that key for when it's built
                    }
                }

                MetadataObject metadataObject = DbHelper.BuildMetadataObject(formObject, resourceId);
                metadataDb.MetadataWrite(metadataObject); // TODO: Make MetadataWrite async and write await?
                return Results.Ok(metadataObject.ResourceId);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        // Takes IFormCollection instead of FormObject, to make it easier to loop through. Otherwise we'd have to use reflection.
        public IResult UpdateMetadataObject(IFormCollection formData, string appUrl, string resourceId)
        {
            try
            {
                var formDataObj = formData.ToDictionary();
                if (formDataObj == null) return Results.BadRequest("Invalid FormData keys");

                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null)
                    return Results.BadRequest("No resource with that ID");

                foreach (var keyValuePair in formDataObj)
                {
                    Console.WriteLine($"Overwriting key {keyValuePair.Key} with value {keyValuePair.Value}");
                    if (!DbHelper.UpdateSingleMetadataValues(keyValuePair, metadataObject))
                    {
                        return Results.BadRequest($"Invalid Key \"{keyValuePair.Key}\" or Value \"{keyValuePair.Value}\"");
                    }
                }
                metadataObject.ResourceId = resourceId;
                metadataDb.MetadataWrite(metadataObject); // TODO: Make MetadataWrite async and write await?
                return Results.Ok(resourceId);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        #endregion

        #region GETTERS
        // Files
        public IResult GetFileById(string resourceId)
        {
            try
            {
                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) 
                    return Results.BadRequest("No resource with that ID");
                return fileDb.GetFile(metadataObject);
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }

        // Metadata
        public IResult GetResourceList()
        {
            try
            {
                var resourceList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
                var json = JsonConvert.SerializeObject(resourceList);
                return Results.Text(json, contentType: "application/json");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        public IResult GetFilteredList(string body)
        {
            try
            {
                bool validRequest = body.TryParseJson(out List<string> filters);
                if (!validRequest || filters == null || filters.Count == 0) return Results.BadRequest($"Request body: {body} is not a valid list");
                var resourceList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
                var filteredList = resourceList.Where(resource => filters.Any(filter => resource.ResourceInfo.ResourceType.Equals(filter, StringComparison.OrdinalIgnoreCase)));
                var json = JsonConvert.SerializeObject(filteredList);
                return Results.Text(json, contentType: "application/json");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        public IResult GetChildrenMetadataList(string resourceId)
        {
            try
            {
                var metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) return Results.BadRequest($"No such resource for id: {resourceId}");

                List<MetadataObject> childrenMetadataList = new();
                List<string>? childrenIds = metadataObject.GenerationTree?.Children?.Select(child => child.ResourceId).ToList();
                foreach (var childId in childrenIds ?? Enumerable.Empty<string>())
                {
                    Console.WriteLine("Child id: " + childId);
                    var childMetadata = metadataDb.GetMetadataObjectById(childId);
                    if (childMetadata != null) // TODO: Consider if this is needed? Children should always exist in same repo
                    {
                        childMetadata.ResourceId = childId;
                        childrenMetadataList.Add(childMetadata);
                    }
                }
                var jsonList = JsonConvert.SerializeObject(childrenMetadataList);
                return Results.Text(jsonList, contentType: "application/json");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        public IResult GetMetadataObjectStringById(string resourceId)
        {
            try
            {
                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) 
                    return Results.BadRequest("No resource with that ID");
                string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataObject, Formatting.Indented);
                return Results.Text(updatedMetadataJsonString, contentType: "application/json");
            }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
        }
        #endregion

        #region VISUALIZERS
        public IResult GetGraphForResource(string resourceId)
        {

            Console.WriteLine("Getting graph for requested object: " + resourceId);
            Dictionary<string, MetadataObject> metadataDict = metadataDb.GetMetadataDict();
            MetadataObject? requestedMdObject = metadataDict.GetMetadataObjWithId(resourceId);
            if (requestedMdObject == null) return Results.BadRequest("No resource exist for that ID");
            var graphString = ResourceConnector.CreateGraph(requestedMdObject, metadataDict);
            return Results.Text(graphString);
        }
        public IResult GetHistogram(string resourceId, string appUrl)
        {
            if (!Directory.Exists(Globals.pathToHistogram))
            {
                Console.WriteLine("No folder exists for JSON, creating " + Globals.pathToHistogram);
                Directory.CreateDirectory(Globals.pathToHistogram);
            }
            MetadataObject? logMetadataObject = metadataDb.GetMetadataObjectById(resourceId);
            if (logMetadataObject == null || logMetadataObject.ResourceInfo?.FileExtension == null) return Results.BadRequest("Invalid resource ID. No reference to resource could be found.");


            string? requestedFileExtension = logMetadataObject.ResourceInfo.FileExtension;
            string nameOfFile = string.IsNullOrWhiteSpace(requestedFileExtension) ? resourceId : resourceId + "." + requestedFileExtension;
            string pathToRequestFile = Path.Combine(Globals.pathToEventLog, nameOfFile);
            if (!File.Exists(pathToRequestFile))
            {
                string badResponse = "No file of type EventLog exists for path " + pathToRequestFile; // TODO: Delete at some point. Should not return the entire path, just easier like this for now
                return Results.BadRequest(badResponse);
            }

            List<string>? childrenIds = logMetadataObject.GenerationTree?.Children?.Select(child => child.ResourceId).ToList();
            foreach (var childId in childrenIds ?? Enumerable.Empty<string>())
            {
                var childMetadata = metadataDb.GetMetadataObjectById(childId);
                if (childMetadata != null && childMetadata.ResourceInfo.ResourceType == "Histogram")
                {
                    var result = fileDb.GetFile(childMetadata);
                    if (!result.GetType().IsInstanceOfType(Results.BadRequest()))
                    {
                        Console.WriteLine("Histogram already exist, returning this");
                        return result;
                    }
                    return Results.BadRequest("Resource has child Histogram that does not exist in the repository. This should not happen, consider removing as child and run again");
                }
            }
            Console.WriteLine("No Histogram exist for resource, creating a new one");
            var histMetadata = HistogramGenerator.CreateHistogramMetadata(resourceId, appUrl, logMetadataObject);
            string histogramString = HistogramGenerator.CreateHistogram(logMetadataObject);
            byte[] file = new UTF8Encoding(true).GetBytes(histogramString);
            metadataDb.MetadataWrite(histMetadata);
            fileDb.WriteFile(histMetadata, file);
            return Results.Text(histogramString, contentType: "application/json");
        }
        #endregion
    }
}
