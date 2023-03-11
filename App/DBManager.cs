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

        public static void AddToMetadata(string resourceLabel, string resourceType, string GUID, string? fileExtension = null, string? streamBroker = null, string? streamTopic = null, string? parents = null, string? children = null)
        {
            bool providedParents = parents.TryParseJson(out List<string> parentsList);
            bool providedChildren = children.TryParseJson(out List<string> childrenList);
            var newMetadataObj = BuildResourceObject(resourceLabel, resourceType, fileExtension, streamBroker, streamTopic, parentsList, childrenList);

            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            UpdateParentResource(GUID, providedParents, parentsList, metadataDict);

            metadataDict[GUID] = newMetadataObj;
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);

            File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
        }

        private static void UpdateParentResource(string GUID, bool providedParents, List<string> parentsList, Dictionary<string, MetadataObject> metadataDict)
        {
            if (providedParents)
            {   // Add own ID as child to parent resource
                foreach (var parent in parentsList)
                {
                    var parentObj = metadataDict.GetValue(parent);
                    if (parentObj == null) return;              // If we can't find parentObj in metadata, do nothing (likely means it exists in another repo or has been deleted).
                    parentObj.Children ??= new List<string>();  // If children are null, initialize
                    parentObj.Children.Add(GUID);
                    metadataDict[parent] = parentObj;           // Overwrite with updated parentObj
                }
            }
        }

        public static MetadataObject? GetMetadataObjectById(string resourceId)
        {
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            return metadataDict.GetValue(resourceId);
        }

        
        private static MetadataObject BuildResourceObject(string resourceLabel, string resourceType, string? fileExtension = null, string? streamBroker = null, string? streamTopic = null,  List<string>? parents = null, List<string>? children = null)
        {
            return new MetadataObject
            {
                ResourceLabel = resourceLabel,
                ResourceType = resourceType,                // EventLog or Visualization. Could make an Enum for this.
                FileExtension = fileExtension, //string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension,  // .xes, .bpmn etc. Streams don't have FileExtension
                StreamBroker = streamBroker, // string.IsNullOrWhiteSpace(streamBroker) ? null : streamBroker,
                StreamTopic = streamTopic, // string.IsNullOrWhiteSpace(streamTopic) ? null : streamTopic,
                RepositoryHost = "https://localhost:4000",  // TODO: Should probably read this from somewhere to make it dynamic.
                CreationDate = DateTime.Now.ToString(),
                Parents = parents,
                Children = children,
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

            string[] files = Directory.GetFiles(pathToResources, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string fileExtension = Path.GetExtension(file).Replace(".", ""); // e.g. save "xes", not ".xes". Can also do ToUpper() to save with upper case like the folders

                string resourceType;
                if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
                    resourceType = "EventLog";
                else
                    resourceType = "Visualization";

                

                if(!fileExtension.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    string fileId = fileName;
                    //fileId = ChangeFileNames(file, fileName, fileExtension); // Should not be called unless you want to change all file names to include the extension
                    AddToMetadata(fileName, resourceType, fileId, fileExtension);
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
