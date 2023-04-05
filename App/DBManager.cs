using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Repository.App
{
    // Consider making a singleton: https://csharpindepth.com/articles/singleton
    /* From JOHN:
     * I kunne lave en ConcurrentQueue som gemmer et par af værdier som en tuple (string id, string metadata) og så har i en tråd som læser fra den der kø og skriver til filerne, det kunne måske virke. I har en tråd som bare lytter på køen og skriver når der kommer noget nyt og der kan ikke skrives til den samme fil samtidig
     * https://briancaos.wordpress.com/2021/01/12/write-to-file-from-multiple-threads-async-with-c-and-net-core/
     * Ja, måske have en statisk tråd som lytter på en concurrentqueue hvor i lægger det data der skal skrives
     */
    public class DBManager
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToMetadata = Path.Combine(pathToResources, "resourceMetadata.json");
        
        public static void UpdateMetadata(MetadataObject metadataObject)
        {
            string? id = metadataObject.ResourceId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine("UpdateMetadata called on invalid metadata object. Doing nothing");
                return;
            }
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            metadataDict[id] = metadataObject;
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);
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
        // Should only ever be called by HistogramGenerator and by the overload function below
        public static void AddToMetadata(string resourceLabel, string resourceType, string GUID, string host, GeneratedFrom? generatedFrom, List<Parent>? parents, string? description = null, string? fileExtension = null, string? streamTopic = null, bool isDynamic = false)
        {
            var newMetadataObj = BuildResourceObject(resourceLabel, resourceType, host, description, fileExtension, streamTopic, generatedFrom, parents, isDynamic);

            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            UpdateParentResource(GUID, false, parents, metadataDict);

            metadataDict[GUID] = newMetadataObj;
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);

            //using (var stream = new FileStream(pathToMetadata, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            //{
            //    // writing data in string
            //    byte[] info = new UTF8Encoding(true).GetBytes(updatedMetadataJsonString);
            //    stream.Write(info, 0, info.Length);
            //    // writing data in bytes already
            //    byte[] data = new byte[] { 0x0 };
            //    stream.Write(data, 0, data.Length);
            //};

            File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
        }
        // Overload of function above that takes strings instead of objects.
        public static void AddToMetadata(string resourceLabel, string resourceType, string GUID, string host, string? generatedFrom = null, string? parents = null, string? description = null, string? fileExtension = null, string? streamTopic = null, bool isDynamic = false)
        {
            bool providedParents = parents.TryParseJson(out List<Parent> parentsList);
            bool providedFromSource = generatedFrom.TryParseJson(out GeneratedFrom generatedFromObj);
            AddToMetadata(resourceLabel, resourceType, GUID, host, generatedFromObj, parentsList, description, fileExtension, streamTopic, isDynamic);
        }
        private static void UpdateParentResource(string GUID, bool providedParents, List<Parent>? parentsList, Dictionary<string, MetadataObject> metadataDict)
        {
            if(parentsList != null)
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

        public static IResult GetChildrenMetadataList(string resourceId)
        {
            var metadataObject = GetMetadataObjectById(resourceId);
            if(metadataObject == null) return Results.BadRequest($"No such resource for id: {resourceId}");
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

        public static IResult GetResourceList()
        {
            var resourceList = GetMetadataAsList();
            var json = JsonConvert.SerializeObject(resourceList);
            return Results.Text(json, contentType: "application/json");
        }


        private static MetadataObject BuildResourceObject(string resourceLabel, string resourceType, string host, string? description = null, string? fileExtension = null, string? streamTopic = null, GeneratedFrom? generatedFrom = null, List<Parent>? parents = null, bool isDynamic = false)
        {
            return new MetadataObject
            {
                CreationDate = DateTime.Now.ToString(),
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
        }

        private static Dictionary<string, MetadataObject> GetMetadataDict()
        {
            //string metadataJsonString = "";
            //using (var stream = new FileStream(pathToMetadata, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    byte[] b = new byte[1024];
            //    UTF8Encoding temp = new UTF8Encoding(true);
            //    int readLen;
            //    while ((readLen = stream.Read(b, 0, b.Length)) > 0)
            //    {
            //        metadataJsonString += temp.GetString(b, 0, readLen);
            //        //Console.WriteLine(temp.GetString(b, 0, readLen));
            //    }
            //}
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
