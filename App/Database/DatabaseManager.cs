﻿using Newtonsoft.Json;

namespace Repository.App.Database
{
    public class DatabaseManager
    {
        IFileDatabase fileDatabase { get; set; }
        public DatabaseManager(IFileDatabase dataInterface)
        {
            fileDatabase = dataInterface;
        }

        public void UpdateMetadata(MetadataObject metadataObject)
        {
            fileDatabase.UpdateMetadata(metadataObject);
        }
        // Should only ever be called by HistogramGenerator and by the overload function below
        //public void AddToMetadata(string resourceLabel, string resourceType, string GUID, string host, GeneratedFrom? generatedFrom, List<Parent>? parents, string? description = null, string? fileExtension = null, string? streamTopic = null)
        //{
        //    var newMetadataObj = BuildResourceObject(resourceLabel, resourceType, host, description, fileExtension, streamTopic, generatedFrom, parents);


        //    DB.UpdateMetadata(newMetadataObj);
        //}
        //// Overload of function above that takes strings instead of objects.
        //public void AddToMetadata(string resourceLabel, string resourceType, string GUID, string host, string? generatedFrom = null, string? parents = null, string? description = null, string? fileExtension = null, string? streamTopic = null)
        //{
        //    bool providedParents = parents.TryParseJson(out List<Parent> parentsList);
        //    bool providedFromSource = generatedFrom.TryParseJson(out GeneratedFrom generatedFromObj);
        //    AddToMetadata(resourceLabel, resourceType, GUID, host, generatedFromObj, parentsList, description, fileExtension);
        //}


        public MetadataObject? GetMetadataObjectById(string resourceId)
        {
            Dictionary<string, MetadataObject> metadataDict = fileDatabase.GetMetadataDict();
            MetadataObject? metadataObj = metadataDict.GetValue(resourceId);
            if (metadataObj == null) return null;
            metadataObj.ResourceId = resourceId;
            return metadataObj;
        }

        public string GetMetadataObjectStringById(string resourceId)
        {
            MetadataObject metadataObject = GetMetadataObjectById(resourceId);
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataObject, Formatting.Indented);
            return updatedMetadataJsonString;
        }

        public IResult GetChildrenMetadataList(string resourceId)
        {
            var metadataObject = GetMetadataObjectById(resourceId);
            if (metadataObject == null) return Results.BadRequest($"No such resource for id: {resourceId}");
            List<MetadataObject> childrenMetadataList = new();
            List<string>? childrenIds = metadataObject.GenerationTree?.Children?.Select(child => child.ResourceId).ToList();
            foreach (var childId in childrenIds ?? Enumerable.Empty<string>())
            {
                Console.WriteLine("Child id: " + childId);
                var childMetadata = GetMetadataObjectById(childId);
                if (childMetadata != null) // TODO: Consider if this is needed? Children should always exist in same repo
                {
                    childMetadata.ResourceId = childId;
                    childrenMetadataList.Add(childMetadata);
                }
            }
            var jsonList = JsonConvert.SerializeObject(childrenMetadataList);
            return Results.Text(jsonList, contentType: "application/json");
        }
        public List<MetadataObject> GetMetadataAsList()
        {
            Dictionary<string, MetadataObject> metadataDict = fileDatabase.GetMetadataDict();
            List<MetadataObject> metadataAsList = new();
            foreach (var metadataObject in metadataDict)
            {
                Console.WriteLine($"{metadataObject.Key}: {metadataObject.Value}");
                metadataObject.Value.ResourceId = metadataObject.Key;
                metadataAsList.Add(metadataObject.Value);
            }
            return metadataAsList;
        }

        public IResult GetResourceList()
        {
            var resourceList = GetMetadataAsList();
            var json = JsonConvert.SerializeObject(resourceList);
            return Results.Text(json, contentType: "application/json");
        }
        public IResult UpdateMetadataObject(IDictionary<string, string> keyValuePairs, string resourceId)
        {
            var metadatObject = GetMetadataObjectById(resourceId);
            if (metadatObject == null) { return Results.BadRequest("No such metadata object exist"); }
            foreach (var keyValuePair in keyValuePairs)
            {
                Console.WriteLine($"Overwriting key {keyValuePair.Key} with value {keyValuePair.Value}");
                if (!UpdateSingleMetadataValue(keyValuePair, metadatObject))
                {
                    return Results.BadRequest($"Invalid Key {keyValuePair.Key} or Value {keyValuePair.Value}");
                }
            }
            metadatObject.ResourceId = resourceId;
            fileDatabase.UpdateMetadata(metadatObject);
            return Results.Ok(resourceId);
        }
        private static bool UpdateSingleMetadataValue(KeyValuePair<string, string> keyValuePair, MetadataObject metadataObject)
        {
            try
            {
                switch (keyValuePair.Key)
                {
                    case "ResourceLabel":
                        metadataObject.ResourceInfo.ResourceLabel = keyValuePair.Value;
                        break;
                    case "Description":
                        metadataObject.ResourceInfo.Description = keyValuePair.Value;
                        break;
                    case "Children":
                        metadataObject.GenerationTree.Children ??= new List<Child>();  // If children are null, initialize
                        metadataObject.GenerationTree.Children.Add(new Child
                        {
                            ResourceId = keyValuePair.Value,
                        });
                        break;
                    case "Dynamic":
                        metadataObject.ResourceInfo.Dynamic = Convert.ToBoolean(keyValuePair.Value);
                        break;
                    //case "GeneratedFrom":
                    //    break;
                    //case "Parents":
                    //    break;
                    default: return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid arguments exception.");
                return false;
            }
        }


        public void BuildAndAddMetadataObject(string resourceId, string resourceLabel, string resourceType, string host, string? description = null, string? fileExtension = null, string? streamTopic = null, GeneratedFrom? generatedFrom = null, List<Parent>? parents = null, bool isDynamic = false)
        {
            var dateInSeconds = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
            var metadataObject = new MetadataObject
            {
                ResourceId = resourceId,
                CreationDate = dateInSeconds.ToString(),// DateTimeOffset.Now.ToString(),
                ResourceInfo = new ResourceInfo
                {
                    ResourceLabel = resourceLabel,
                    ResourceType = resourceType,
                    Host = host,
                    Description = description,
                    FileExtension = fileExtension,
                    StreamTopic = streamTopic,
                    Dynamic = isDynamic,
                },
                GenerationTree = new GenerationTree
                {
                    GeneratedFrom = generatedFrom,
                    Parents = parents,
                }
            };
            fileDatabase.UpdateMetadata(metadataObject);
        }
        //private MetadataObject BuildResourceObject(string resourceLabel, string resourceType, string host, string? description = null, string? fileExtension = null, string? streamTopic = null, GeneratedFrom? generatedFrom = null, List<Parent>? parents = null)
        //{
        //    return new MetadataObject
        //    {
        //        CreationDate = DateTime.Now.ToString(),
        //        ResourceInfo = new ResourceInfo
        //        {
        //            ResourceLabel = resourceLabel,
        //            ResourceType = resourceType,
        //            Host = host,  // TODO: Should probably read this from somewhere to make it dynamic.
        //            Description = description,
        //            FileExtension = fileExtension,
        //            StreamTopic = streamTopic
        //        },
        //        GenerationTree = new GenerationTree
        //        {
        //            GeneratedFrom = generatedFrom,
        //            Parents = parents,
        //        }
        //    };
        //}

        private Dictionary<string, MetadataObject> GetMetadataDict()
        {
            return fileDatabase.GetMetadataDict();
        }
    }

    #region extensionMethods
    public static class ExtensionMethods
    {
        public static TV GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default)
        {
            TV value;
            return dict.TryGetValue(key, out value) ? value : defaultValue;
        }
        public static bool TryParseJson<T>(this string obj, out T result)
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Error;

                result = JsonConvert.DeserializeObject<T>(obj, settings);
                return true;
            }
            catch (Exception)
            {
                result = default;
                return false;
            }
        }
        public static IDictionary<string, string> ToDictionary(this IFormCollection col)
        {
            var dict = new Dictionary<string, string>();

            foreach (var key in col.Keys)
            {
                dict.Add(key, col[key]);
            }

            return dict;
        }
    }
    #endregion
}
