using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Repository.App.Database
{
    // Consider making a singleton: https://csharpindepth.com/articles/singleton
    /* From JOHN:
     * I kunne lave en ConcurrentQueue som gemmer et par af værdier som en tuple (string id, string metadata) og så har i en tråd som læser fra den der kø og skriver til filerne, det kunne måske virke. I har en tråd som bare lytter på køen og skriver når der kommer noget nyt og der kan ikke skrives til den samme fil samtidig
     * https://briancaos.wordpress.com/2021/01/12/write-to-file-from-multiple-threads-async-with-c-and-net-core/
     * Ja, måske have en statisk tråd som lytter på en concurrentqueue hvor i lægger det data der skal skrives
     */
    public class FileDatabase : IFileDatabase
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToMetadata = Path.Combine(pathToResources, "resourceMetadata.json");

        // Shouldn't take an object? Rework.
        public static IResult UpdateSingleMetadata(IDictionary<string, string> keyValuePairs, string resourceId)
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
            UpdateMetadataFile(metadatObject, resourceId);
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
        // ONLY place that should ever write to the metadata file
        private static void UpdateMetadataFile(MetadataObject metadataObject, string resourceId)
        {
            metadataObject.ResourceId = null; // Set to null before writing to file
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            MetadataObject? currentMetadataObject = metadataDict.GetValue(resourceId);
            if (metadataObject.Equals(currentMetadataObject))
            {
                Console.WriteLine($"Metadata object already exist with the same values. Leaving metadata as is.");
                return;
            }

            metadataDict[resourceId] = metadataObject;
            UpdateParentResource(metadataDict, resourceId);

            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);
            Console.WriteLine("Writing to metadata for resource id " + resourceId);
            File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
        }
        /* John: Jeg tror jeg ville forsøge at pakke den her ind i et DTO, det er måske lidt pænere og nemmere at tilføje argumenter til. I kan også gøre det at i laver alle jeres nullchecks ind i objektets konstruktør måske
         * Og så ville jeg utrække et interface hos db manager og se om det er muligt at bruge Program.cs app builder til at sende implementeringen med?
         * Måske lave et simpelt interface der kun har UpdateMetadata som både opdater/tilføjer hvis ikke findes, getKeys() som returnerer navnene på alle filer, en containsKey() og GetMetadata(string key), og så give den med til jeres DBManager, som får en konstruktør der tager IDatabase, som er implementeret af FileDatabase, som kun har de 4 metoder?
         * Og så kan jeres DBManager have alle de der hjælpemetoder, men kun kommunikere med filsystemet gennem FileDatabase, hvis I, i fremtiden vil bruge SQL så kunne i lave en SQLDatabase fil, som implementerer samme interface og sende det med
         * Omkring ZIP filen: Har ikke testet om det virker, men hvis alt fil kommunikation kan flyttes til FileDatabase, så er i rimelig good to go, I kan forsøge med bare at have en dictionary InMemoryDatabase
         * 
         * 
         * 
         */

        private static void UpdateParentResource(Dictionary<string, MetadataObject> metadataDict, string resourceId)
        {
            List<Parent> parents = metadataDict.GetValue(resourceId)?.GenerationTree?.Parents;
            // Add own ID as child to parent resource
            foreach (var parent in parents ?? Enumerable.Empty<Parent>())
            {
                string parentId = parent.ResourceId;
                var parentObj = metadataDict.GetValue(parentId);
                if (parentObj == null) return;              // If we can't find parentObj in metadata, do nothing (likely means it exists in another repo or has been deleted).
                parentObj.GenerationTree.Children ??= new List<Child>();  // If children are null, initialize
                parentObj.GenerationTree.Children.Add(new Child
                {
                    ResourceId = resourceId,
                });
                metadataDict[parentId] = parentObj;           // Overwrite with updated parentObj
            }

        }

        public static MetadataObject? GetMetadataObjectById(string resourceId)
        {
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            MetadataObject? metadataObj = metadataDict.GetValue(resourceId);
            if (metadataObj == null) return null;
            metadataObj.ResourceId = resourceId;
            return metadataObj;
        }



        public static IResult GetChildrenMetadataList(string resourceId)
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




        public static void BuildAndAddMetadataObject(string resourceId, string resourceLabel, string resourceType, string host, string? description = null, string? fileExtension = null, string? streamTopic = null, GeneratedFrom? generatedFrom = null, List<Parent>? parents = null, bool isDynamic = false)
        {
            var dateInSeconds = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
            var metadataObject = new MetadataObject
            {
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
            UpdateMetadataFile(metadataObject, resourceId);
        }

        private static Dictionary<string, MetadataObject> GetMetadataDict()
        {
            string metadataJsonString = File.ReadAllText(pathToMetadata);
            Dictionary<string, MetadataObject>? metadataDict = JsonConvert.DeserializeObject<Dictionary<string, MetadataObject>>(metadataJsonString);
            metadataDict ??= new Dictionary<string, MetadataObject>();
            return metadataDict;
        }

        //// Helper function to fill metadata file with all resources in the Resources folder:
        //public static void FillMetadata()
        //{
        //    // TODO: Maybe add a safe way to recreate the metadata file before running this
        //    int counter = 0;
        //    string[] files = Directory.GetFiles(pathToResources, "*.*", SearchOption.AllDirectories);
        //    foreach (string file in files)
        //    {
        //        string fileName = Path.GetFileNameWithoutExtension(file);
        //        bool isValidGuid = Guid.TryParse(fileName, out var fileNameGuid);

        //        string fileExtension = Path.GetExtension(file).Replace(".", ""); // e.g. save "xes", not ".xes". Can also do ToUpper() to save with upper case like the folders

        //        string resourceType;
        //        if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "EventLog";
        //        else if (fileExtension.Equals("BPMN", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "ProcessModel";
        //        else if (fileExtension.Equals("DOT", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "Graph";
        //        else if (fileExtension.Equals("JPG", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "Image";
        //        else if (fileExtension.Equals("PNG", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "Image";
        //        else if (fileExtension.Equals("JSON", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "Histogram";
        //        else if (fileExtension.Equals("PNML", StringComparison.OrdinalIgnoreCase))
        //            resourceType = "PetriNet";
        //        else
        //            resourceType = "Misc";



        //        //if(!fileName.Contains("metadata", StringComparison.OrdinalIgnoreCase))
        //        if(isValidGuid)
        //        {
        //            counter++;
        //            //string fileId = fileName;
        //            //fileId = ChangeFileNames(file, fileName, fileExtension); // Should not be called unless you want to change all file names to include the extension
        //            AddToMetadata(resourceLabel: $"Some label {counter}", resourceType: resourceType, GUID: fileNameGuid.ToString(), host: "https://localhost:4000/resources", description: "Some file description", fileExtension: fileExtension);
        //        }
        //    }
        //}


        //// Should ONLY be called if we want to change all the file names to include the extension
        //private static string ChangeFileNames(string file, string fileName, string fileExtension)
        //{
        //    string fileId = fileName + fileExtension.ToUpper();
        //    string newFilePath = Path.Combine(Path.GetDirectoryName(file), fileId + Path.GetExtension(file));
        //    File.Move(file, newFilePath);
        //    Console.WriteLine("New file name: " + fileId);
        //    return fileId;
        //}
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
