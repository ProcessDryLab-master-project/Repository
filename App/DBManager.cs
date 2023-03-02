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

        public static List<ListDictionary> GetMetadataAsList()
        {
            Dictionary<string, ListDictionary> metadataDict = GetMetadataDict();
            List<ListDictionary> metadataAsList = new();
            foreach (var metadataObject in metadataDict)
            {
                Console.WriteLine($"{metadataObject.Key}: {metadataObject.Value}");
                metadataObject.Value.Add("id", metadataObject.Key);
                metadataAsList.Add(metadataObject.Value);
            }
            return metadataAsList;
        }

        public static void AddToMetadata(string fileName, string fileType, string fileExtension)
        {
            var newMetadataObj = BuildResourceObject(fileName, fileType, fileExtension);

            Dictionary<string, ListDictionary> metadataDict = GetMetadataDict();

            metadataDict[Guid.NewGuid().ToString()] = newMetadataObj;
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);

            File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
        }

        private static ListDictionary BuildResourceObject(string fileName, string fileType, string fileExtension)
        {
            return new ListDictionary
            {
                { "name", fileName },                       // Puts file name without extension
                { "type", fileType },                       // EventLog or Visualization. Could make an Enum for this.
                { "extension", fileExtension },             // .xes, .bpmn etc.
                { "host", "https://localhost:4000" },       // TODO: Should probably read this from somewhere to make it dynamic.
                { "creationDate", DateTime.Now },
            };
        }

        private static Dictionary<string, ListDictionary> GetMetadataDict()
        {
            string metadataJsonString = File.ReadAllText(pathToMetadata);
            Dictionary<string, ListDictionary>? metadataDict = JsonConvert.DeserializeObject<Dictionary<string, ListDictionary>>(metadataJsonString);
            metadataDict ??= new Dictionary<string, ListDictionary>();
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
                string fileExtension = Path.GetExtension(file);
                string fileType;
                if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
                    fileType = "EventLog";
                else
                    fileType = "Visualization";

                AddToMetadata(fileName, fileType, fileExtension);
            }
        }
    }
}
