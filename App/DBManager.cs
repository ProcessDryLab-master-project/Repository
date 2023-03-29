using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Linq;

namespace Repository.App
{
    public class DBManager
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToMetadata = Path.Combine(pathToResources, "resourceMetadata.json");

        public static List<MetadataObject> GetMetadataAsList()
        {
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            List<MetadataObject> metadataAsList = new();
            foreach (var metadataObject in metadataDict)
            {
                Console.WriteLine($"{metadataObject.Key}: {metadataObject.Value}");
                metadataObject.Value.ResourceId = metadataObject.Key;
                metadataAsList.Add(metadataObject.Value);
            }
            return metadataAsList;
        }
        
        // Should only ever be called by HistogramGenerator and by the overload function below
        public static void AddToMetadata(string resourceLabel, string resourceType, string GUID, string host, GeneratedFrom generatedFrom, List<Parent> parents, string? description = null, string? fileExtension = null, string? streamTopic = null)
        {
            var newMetadataObj = BuildResourceObject(resourceLabel, resourceType, host, description, fileExtension, streamTopic, generatedFrom, parents);

            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            UpdateParentResource(GUID, false, parents, metadataDict);

            metadataDict[GUID] = newMetadataObj;
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);

            File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
        }
        // Overload of function above that takes strings instead of objects.
        public static void AddToMetadata(string resourceLabel, string resourceType, string GUID, string host, string? generatedFrom = null, string? parents = null, string? description = null, string? fileExtension = null, string? streamTopic = null)
        {
            bool providedParents = parents.TryParseJson(out List<Parent> parentsList);
            bool providedFromSource = generatedFrom.TryParseJson(out GeneratedFrom generatedFromObj);
            AddToMetadata(resourceLabel, resourceType, GUID, host, generatedFromObj, parentsList, description, fileExtension);
        }
        private static void UpdateParentResource(string GUID, bool providedParents, List<Parent> parentsList, Dictionary<string, MetadataObject> metadataDict)
        {
            if(parentsList != null)
            //if (providedParents)
            {   // Add own ID as child to parent resource
                foreach (var parent in parentsList)
                {
                    string parentId = parent.ResourceId;
                    var parentObj = metadataDict.GetValue(parentId);
                    if (parentObj == null) return;              // If we can't find parentObj in metadata, do nothing (likely means it exists in another repo or has been deleted).
                    parentObj.GenerationTree.Children ??= new List<Child>();  // If children are null, initialize
                    parentObj.GenerationTree.Children.Add(new Child
                    {
                        ResourceId = GUID,
                    });
                    metadataDict[parentId] = parentObj;           // Overwrite with updated parentObj
                }
            }
        }

        public static MetadataObject? GetMetadataObjectById(string resourceId)
        {
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            MetadataObject? metadataObj = metadataDict.GetValue(resourceId);
            if(metadataObj == null) return null;
            metadataObj.ResourceId = resourceId;
            return metadataObj;
        }

        public static string GetMetadataObjectStringById(string resourceId)
        {
            MetadataObject metadataObject = GetMetadataObjectById(resourceId);
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataObject, Formatting.Indented);
            return updatedMetadataJsonString;
        }

        
        private static MetadataObject BuildResourceObject(string resourceLabel, string resourceType, string host, string? description = null, string? fileExtension = null, string? streamTopic = null, GeneratedFrom? generatedFrom = null, List<Parent>? parents = null)
        {
            return new MetadataObject
            {
                CreationDate = DateTime.Now.ToString(),
                ResourceInfo = new ResourceInfo
                {
                    ResourceLabel = resourceLabel,
                    ResourceType = resourceType,
                    Host = host,  // TODO: Should probably read this from somewhere to make it dynamic.
                    Description = description,
                    FileExtension = fileExtension,
                    StreamTopic = streamTopic
                },
                GenerationTree = new GenerationTree
                {
                    GeneratedFrom = generatedFrom,
                    Parents = parents,
                }
            };
        }

        private static Dictionary<string, MetadataObject> GetMetadataDict()
        {
            string metadataJsonString = File.ReadAllText(pathToMetadata);
            Dictionary<string, MetadataObject>? metadataDict = JsonConvert.DeserializeObject<Dictionary<string, MetadataObject>>(metadataJsonString);
            metadataDict ??= new Dictionary<string, MetadataObject>();
            return metadataDict;
        }

        // Helper function to fill metadata file with all resources in the Resources folder:
        public static void FillMetadata()
        {
            // TODO: Maybe add a safe way to recreate the metadata file before running this
            int counter = 0;
            string[] files = Directory.GetFiles(pathToResources, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bool isValidGuid = Guid.TryParse(fileName, out var fileNameGuid);

                string fileExtension = Path.GetExtension(file).Replace(".", ""); // e.g. save "xes", not ".xes". Can also do ToUpper() to save with upper case like the folders

                string resourceType;
                if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
                    resourceType = "EventLog";
                else if (fileExtension.Equals("BPMN", StringComparison.OrdinalIgnoreCase))
                    resourceType = "ProcessModel";
                else if (fileExtension.Equals("DOT", StringComparison.OrdinalIgnoreCase))
                    resourceType = "Graph";
                else if (fileExtension.Equals("JPG", StringComparison.OrdinalIgnoreCase))
                    resourceType = "Image";
                else if (fileExtension.Equals("PNG", StringComparison.OrdinalIgnoreCase))
                    resourceType = "Image";
                else if (fileExtension.Equals("JSON", StringComparison.OrdinalIgnoreCase))
                    resourceType = "Histogram";
                else if (fileExtension.Equals("PNML", StringComparison.OrdinalIgnoreCase))
                    resourceType = "PetriNet";
                else
                    resourceType = "Misc";

                

                //if(!fileName.Contains("metadata", StringComparison.OrdinalIgnoreCase))
                if(isValidGuid)
                {
                    counter++;
                    //string fileId = fileName;
                    //fileId = ChangeFileNames(file, fileName, fileExtension); // Should not be called unless you want to change all file names to include the extension
                    AddToMetadata(resourceLabel: $"Some label {counter}", resourceType: resourceType, GUID: fileNameGuid.ToString(), host: "https://localhost:4000/resources", description: "Some file description", fileExtension: fileExtension);
                }
            }
        }


        // Should ONLY be called if we want to change all the file names to include the extension
        private static string ChangeFileNames(string file, string fileName, string fileExtension)
        {
            string fileId = fileName + fileExtension.ToUpper();
            string newFilePath = Path.Combine(Path.GetDirectoryName(file), fileId + Path.GetExtension(file));
            File.Move(file, newFilePath);
            Console.WriteLine("New file name: " + fileId);
            return fileId;
        }
    }

    #region extensionMethods
    public static class ExtensionMethods
    {
        public static TV GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default(TV))
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
                result = default(T);
                return false;
            }
        }
    }
    #endregion
}
