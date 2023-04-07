using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Repository.App.Database
{
    // https://briancaos.wordpress.com/2021/01/12/write-to-file-from-multiple-threads-async-with-c-and-net-core/

    public class FileDatabase : IFileDatabase
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToMetadata = Path.Combine(pathToResources, "resourceMetadata.json");
        ConcurrentQueue<MetadataObject> metadataQueue = new ConcurrentQueue<MetadataObject>();
        static Thread? Thread;
        public FileDatabase()
        {
            if (Thread == null)
            {
                Thread = new Thread(() =>
                {
                    while (true)
                    {
                        if (!metadataQueue.IsEmpty)
                        {
                            metadataQueue.TryDequeue(out MetadataObject? metadataObject);
                            UpdateMetadataFile(metadataObject);
                        }
                    }
                });
                Thread.Start();
            }
        }
        public bool ContainsKey(string key)
        {
            return GetMetadataDict().ContainsKey(key);
        }

        public MetadataObject? GetMetadataObjectById(string key)
        {
            if (ContainsKey(key))
            {
                return GetMetadataDict()[key];
            }
            else return null;
        }

        public void UpdateMetadataObject(MetadataObject metadataObject)
        {
            metadataQueue.Enqueue(metadataObject);
        }

        public Dictionary<string, MetadataObject> GetMetadataDict()
        {
            string metadataJsonString = File.ReadAllText(pathToMetadata);
            Dictionary<string, MetadataObject>? metadataDict = JsonConvert.DeserializeObject<Dictionary<string, MetadataObject>>(metadataJsonString);
            metadataDict ??= new Dictionary<string, MetadataObject>();
            return metadataDict;
        }

        // ONLY function that should ever write to the metadata file
        private void UpdateMetadataFile(MetadataObject? metadataObject)
        {
            Console.WriteLine("Updating data file with metadata for ID: " + metadataObject?.ResourceId);
            try
            {
                string? resourceId = metadataObject?.ResourceId;
                if(string.IsNullOrWhiteSpace(resourceId))
                {
                    Console.WriteLine("Error, missing resourceId in metadata object. Returning nothing");
                    return;
                }
                metadataObject.ResourceId = null; // Set to null if it isn't already, before writing to file
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
            catch (IOException ioe)
            {
                Console.WriteLine("IOException: " + ioe);
                throw new Exception("IOException: " + ioe);
            }
            catch (JsonException je)
            {
                Console.WriteLine("JsonException: " + je);
                throw new Exception("Could not serialize object");
            }
        }

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
    }
}
