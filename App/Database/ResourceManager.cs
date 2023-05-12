using csdot.Attributes.Types;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Repository.App.Entities;
using System.Collections.Generic;
using System.Collections.Specialized;

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
        public IResult PostFile(IFormCollection formData, string appUrl)
        {
            try
            {
                var requestFile = formData.Files;
                if (!requestFile.Any()) return Results.BadRequest("Exactly one file is required");
                var file = requestFile.Single();
                // TODO: Consider if we should send byte[] or IFormFile: Reading IFormFile to a byte array in the hopes that it makes it more stable. Might not be needed.
                byte[] fileData = DbHelper.FileToByteArr(file);

                var formDataObj = formData.ToDictionary();
                formDataObj = DbHelper.ValidateFormData(formDataObj, appUrl);
                if(formDataObj == null) return Results.BadRequest("Invalid FormData keys");
                var metadataObject = DbHelper.BuildMetadataObject(formDataObj);
                string resourceId = metadataObject.ResourceId!; // Saving generated resource ID so we can return it, since it's removed before writing to metadata file

                var writeResult = fileDb.WriteFile(metadataObject, fileData);
                if (writeResult.GetType() == typeof(BadRequest)) return writeResult;
                metadataDb.MetadataWrite(metadataObject);
                if(metadataObject.ResourceInfo.Dynamic) metadataDb.UpdateDynamicResourceTime(resourceId);
                return Results.Ok(resourceId);
        }
            catch (Exception e)
            {
                return Results.BadRequest(e);
            }
}
        public async Task<IResult> UpdateFile(IFormCollection formData, string resourceId)
        {
            try
            {
                var requestFile = formData.Files;
                if (!requestFile.Any()) return Results.BadRequest("Exactly one file is required");
                var file = requestFile.Single();
                byte[] fileData = DbHelper.FileToByteArr(file);

                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) return Results.BadRequest("No resource with that ID");
                metadataDb.UpdateDynamicResourceTime(resourceId); // TODO: Make MetadataWrite async and write await?

                return fileDb.WriteFile(metadataObject, fileData);
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository UpdateFile error: " + e);
            }
        }
        // Metadata specific
        public async Task<IResult> PostMetadata(IFormCollection formData, string appUrl)
        {
            try
            {
                var formDataObj = formData.ToDictionary();
                formDataObj = DbHelper.ValidateFormData(formDataObj, appUrl);
                if (formDataObj == null) return Results.BadRequest("Invalid FormData keys");
                string? resourceId = null;
                MetadataObject? metadataObject;
                if (formDataObj["ResourceType"] == "EventStream")
                {
                    bool overwrite = !string.Equals(formDataObj["Overwrite"], "true", StringComparison.OrdinalIgnoreCase);
                    var metadataList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
                    var existingMetadata = metadataList.Find(metadata => metadata.ResourceInfo.Host == formDataObj["Host"] && metadata.ResourceInfo.StreamTopic == formDataObj["StreamTopic"]);
                    if (existingMetadata != null)
                    {
                        if(!overwrite) // If a similar metadata object exists and no Overwrite key has been sent, don't add a new one.
                            return Results.BadRequest("An EventStream for that StreamTopic and Host already exist. Use the Overwrite key if you wish to change it.");
                        
                        resourceId = existingMetadata.ResourceId!; // If we're overwriting a metadata object, save that key for when it's built
                    }
                }

                metadataObject = DbHelper.BuildMetadataObject(formDataObj, resourceId);
                resourceId = metadataObject.ResourceId!; // Saving generated resource ID so we can return it, since it's removed before writing to metadata file

                metadataDb.MetadataWrite(metadataObject); // TODO: Make MetadataWrite async and write await?
                return Results.Ok(resourceId);
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository PostMetadata error: " + e);
            }
        }
        public async Task<IResult> UpdateMetadataObject(IFormCollection formData, string appUrl, string resourceId)
        {
            try
            {
                var formDataObj = formData.ToDictionary();
                formDataObj = DbHelper.ValidateFormData(formDataObj, appUrl);
                if (formDataObj == null) return Results.BadRequest("Invalid FormData keys");

                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) return Results.BadRequest("No resource with that ID");

                foreach (var keyValuePair in formDataObj)
                {
                    Console.WriteLine($"Overwriting key {keyValuePair.Key} with value {keyValuePair.Value}");
                    if (!DbHelper.UpdateSingleMetadataValues(keyValuePair, metadataObject))
                    {
                        return Results.BadRequest($"Invalid Key {keyValuePair.Key} or Value {keyValuePair.Value}");
                    }
                }
                metadataObject.ResourceId = resourceId;
                metadataDb.MetadataWrite(metadataObject); // TODO: Make MetadataWrite async and write await?
                return Results.Ok(resourceId);
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository UpdateMetadataObject error: " + e);
            }
        }
        #endregion

        #region GETTERS
        // Files
        public async Task<IResult> GetFileById(string resourceId)
        {
            try
            {
                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) return Results.BadRequest("No resource with that ID");
                return fileDb.GetFile(metadataObject);
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository GetFileById error: " + e);
            }
        }

        // Metadata
        public async Task<IResult> GetResourceList(HttpRequest request)
        {
            try
            {
                var resourceList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
                var json = JsonConvert.SerializeObject(resourceList);
                return Results.Text(json, contentType: "application/json");
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository GetFilteredList error: " + e);
            }
        }
        public async Task<IResult> GetFilteredList(HttpRequest request)
        {
            try
            {
                var body = new StreamReader(request.Body);
                string bodyString = body.ReadToEnd();
                //string bodyString = await body.ReadToEndAsync();
                Console.WriteLine("Filters: " + bodyString);

                bool validRequest = bodyString.TryParseJson(out List<string> filters);
                if (!validRequest || filters == null || filters.Count == 0) return Results.BadRequest($"Request body: {bodyString} is not a valid list");
                var resourceList = DbHelper.MetadataDictToList(metadataDb.GetMetadataDict());
                var filteredList = resourceList.Where(resource => filters.Any(filter => resource.ResourceInfo.ResourceType.Equals(filter, StringComparison.OrdinalIgnoreCase)));
                var json = JsonConvert.SerializeObject(filteredList);
                return Results.Text(json, contentType: "application/json");
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository GetFilteredList error: " + e);
            }
        }
        public async Task<IResult> GetChildrenMetadataList(string resourceId)
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
                return Results.BadRequest("Repository GetChildrenMetadataList error: " + e);
            }
        }
        public async Task<IResult> GetMetadataObjectStringById(string resourceId)
        {
            try
            {
                MetadataObject? metadataObject = metadataDb.GetMetadataObjectById(resourceId);
                if (metadataObject == null) return Results.BadRequest("No resource with that ID");
                string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataObject, Formatting.Indented);
                return Results.Text(updatedMetadataJsonString, contentType: "application/json"); 
                //return Results.Text(updatedMetadataJsonString);
                //return Results.Ok(metadataObject);
            }
            catch (Exception e)
            {
                return Results.BadRequest("Repository GetFilteredList error: " + e);
            }
        }
        #endregion
    }
}
