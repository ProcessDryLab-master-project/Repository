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
                metadataObject.Value.FileId = metadataObject.Key;
                metadataAsList.Add(metadataObject.Value);
            }
            return metadataAsList;
        }

        public static void AddToMetadata(string fileLabel, string fileType, string fileExtension, string GUID, string? basedOnId = null)
        {
            var newMetadataObj = BuildResourceObject(fileLabel, fileType, fileExtension, basedOnId);

            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();

            metadataDict[GUID] = newMetadataObj;
            string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);

            File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
        }

        public static MetadataObject GetMetadataObjectById(string resourceId)
        {
            Dictionary<string, MetadataObject> metadataDict = GetMetadataDict();
            return metadataDict[resourceId];
        }

        
        private static MetadataObject BuildResourceObject(string fileLabel, string fileType, string fileExtension, string? basedOnId = null)
        {
            return new MetadataObject
            {
                FileLabel = fileLabel,                      // Puts file name without extension
                FileType = fileType,                        // EventLog or Visualization. Could make an Enum for this.
                FileExtension = fileExtension,              // .xes, .bpmn etc.
                RepositoryHost = "https://localhost:4000",  // TODO: Should probably read this from somewhere to make it dynamic.
                CreationDate = DateTime.Now.ToString(),
                BasedOnId = string.IsNullOrWhiteSpace(basedOnId) ? null : basedOnId,
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

                string fileType;
                if (fileExtension.Equals("XES", StringComparison.OrdinalIgnoreCase))
                    fileType = "EventLog";
                else
                    fileType = "Visualization";

                

                if(!fileExtension.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    string fileId = fileName;
                    //fileId = ChangeFileNames(file, fileName, fileExtension); // Should not be called unless you want to change all file names to include the extension
                    AddToMetadata(fileName, fileType, fileExtension, fileId);
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


        // If we don't want to serialize into an object, but just want to use ListDictionary:
        //private static ListDictionary BuildResourceObject(string fileName, string fileType, string fileExtension)
        //{
        //    return new ListDictionary
        //    {
        //        { "FileName", fileName },                       // Puts file name without extension
        //        { "FileType", fileType },                       // EventLog or Visualization. Could make an Enum for this.
        //        { "FileExtension", fileExtension },             // .xes, .bpmn etc.
        //        { "RepositoryHost", "https://localhost:4000" }, // TODO: Should probably read this from somewhere to make it dynamic.
        //        { "CreationDate", DateTime.Now },
        //    };
        //}
    }
}
