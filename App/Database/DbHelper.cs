using Repository.App.Entities;

namespace Repository.App.Database
{
    public class DbHelper
    {
        public static string GetFileSavePath(MetadataObject metadataObj)
        {
            string nameToSaveFile = string.IsNullOrWhiteSpace(metadataObj.ResourceInfo.FileExtension) ? metadataObj.ResourceId! : metadataObj.ResourceId + "." + metadataObj.ResourceInfo.FileExtension;
            string pathToResourceType = Path.Combine(Globals.pathToResources, metadataObj.ResourceInfo.ResourceType);
            string pathToSaveFile = Path.Combine(pathToResourceType, nameToSaveFile);
            if (!Directory.Exists(pathToResourceType))
            {
                Console.WriteLine("No folder exists for this file type, creating " + pathToResourceType);
                Directory.CreateDirectory(pathToResourceType);
            }

            return pathToSaveFile;
        }
        public static MetadataObject BuildMetadataObject(IDictionary<string, string> metadataKeys, string? resourceId = null)
        {
            bool providedFromSource = metadataKeys.GetValue("GeneratedFrom").TryParseJson(out HashSet<Parent>? parentsList);
            bool providedParents = metadataKeys.GetValue("Parents").TryParseJson(out GeneratedFrom? generatedFromObj);

            var dateInMilliSeconds = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
            var metadataObject = new MetadataObject
            {
                ResourceId = resourceId ?? Guid.NewGuid().ToString(),
                CreationDate = dateInMilliSeconds.ToString(),// DateTimeOffset.Now.ToString(),
                ResourceInfo = new ResourceInfo
                {
                    ResourceLabel = metadataKeys.GetValue("ResourceLabel"),
                    ResourceType = metadataKeys.GetValue("ResourceType"),
                    Host = metadataKeys.GetValue("Host"),
                    Description = metadataKeys.GetValue("Description"),
                    FileExtension = metadataKeys.GetValue("FileExtension"),
                    StreamTopic = metadataKeys.GetValue("StreamTopic"),
                    Dynamic = string.Equals(metadataKeys.GetValue("Dynamic"), "true", StringComparison.OrdinalIgnoreCase),
                },
                GenerationTree = new GenerationTree
                {
                    GeneratedFrom = generatedFromObj,
                    Parents = parentsList,
                }
            };
            return metadataObject;
        }

        public static IDictionary<string, string>? ValidateFormData(IDictionary<string, string> metadataKeys, string? appUrl = null)
        {
            if (metadataKeys == null) { return null; }
            // TODO: Add different validations, such as verifying that certain keys exist and if X key is there, then Y key must also be there
            // if (metadataKeys.GetValue("ResourceLabel") == null) { return null; }
            // if (metadataKeys.GetValue("ResourceType") == null) { return null; }
            // if (metadataKeys.GetValue("ResourceType") == "EventStream" && (metadataKeys.GetValue("Host") == null || metadataKeys.GetValue("StreamTopic") == null)) { return null; }

            metadataKeys["Host"] = metadataKeys.GetValue("Host") ?? $"{appUrl}/resources/";
            //metadataKeys["Host"] = metadataKeys["Host"] ?? $"{appUrl}/resources/"; // Set "Host" key if it's not already set.

            return metadataKeys;
        }
        public static List<MetadataObject> MetadataDictToList(Dictionary<string, MetadataObject> metadataDict)
        {
            List<MetadataObject> metadataAsList = new();
            foreach (var metadataObject in metadataDict)
            {
                //Console.WriteLine($"{metadataObject.Key}: {metadataObject.Value}");
                metadataObject.Value.ResourceId = metadataObject.Key;
                metadataAsList.Add(metadataObject.Value);
            }
            return metadataAsList;
        }

        public static bool UpdateSingleMetadataValues(KeyValuePair<string, string> keyValuePair, MetadataObject metadataObject)
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
                        metadataObject.GenerationTree.Children ??= new HashSet<Child>();  // If children are null, initialize
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
                Console.WriteLine("Invalid arguments exception: " + e);
                return false;
            }
        }

        public static byte[] FileToByteArr(IFormFile file)
        {
            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}
