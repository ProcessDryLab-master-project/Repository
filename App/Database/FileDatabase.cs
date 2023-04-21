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
        static ConcurrentQueue<MetadataObject> metadataQueue = new ConcurrentQueue<MetadataObject>();

        static ConcurrentDictionary<string, long> dynamicFiles = new();
        // 1.000ms = 1s. 30.000ms = 30s. 300.000ms = 5min
        static readonly long millisecondsToWait = 30000; // 30 seconds before dynamic resources are changed

        static Thread? MetadataThread;
        static Thread? FileThread;

        //static Thread? FileThread;
        public FileDatabase()
        {
            if (MetadataThread == null)
            {
                Console.WriteLine("Creating thread");
                MetadataThread = new Thread(() =>
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
                MetadataThread.Name = Guid.NewGuid().ToString();
                MetadataThread.Start();
            }
            if (FileThread == null)
            {
                Console.WriteLine("Creating FileThread");
                FileThread = new Thread(() =>
                {
                    Console.WriteLine(dynamicFiles.Count);
                    while (true)
                    {
                        foreach (var dynamicFile in dynamicFiles)
                        {
                            var newTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds - millisecondsToWait;
                            //Console.WriteLine($"Dynamicfile key: {dynamicFile.Key} and Dynamicfile value: {dynamicFile.Value} and New time: {newTime}");
                            if (dynamicFile.Value < newTime)
                            {
                                Console.WriteLine($"No update to dynamic in {millisecondsToWait * 1000} seconds. Setting Dynamic to false and removing from dynamicFiles.");
                                // TODO: Consider that we can technically queue the update to Dynamic in metadata, remove it from "dynamicFiles" and then receive an update in the meantime. Very unlikely, but possible. And does it matter?
                                SetDynamicToFalse(dynamicFile.Key); 
                                dynamicFiles.TryRemove(dynamicFile);
                            }
                        }
                        Thread.Sleep(2000); // TODO: We don't really have to sleep here, it's just to make prints more managable.
                    }
                });
                FileThread.Name = Guid.NewGuid().ToString();
                FileThread.Start();
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
            Console.WriteLine($"Adding metadata object to queue with length {metadataQueue.Count()} on Thread: {MetadataThread.Name}");      
        }

        public void UpdateDynamicResourceTime(string resourceId)
        {
            //fileQueue.Enqueue(new(resourceId, file)); // Maybe use this to queue updates to dynamic resources like with metadata.
            dynamicFiles[resourceId] = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private void SetDynamicToFalse(string resourceId)
        {
            var metadataObj = GetMetadataDict()!.GetValue(resourceId);
            metadataObj.ResourceId = resourceId;
            metadataObj.ResourceInfo.Dynamic = false;
            UpdateMetadataObject(metadataObj);
        }

        public Dictionary<string, MetadataObject> GetMetadataDict()
        {
            string metadataJsonString;
            lock (Globals.FileAccessLock)
            {
                metadataJsonString = File.ReadAllText(pathToMetadata);
            }
            Dictionary<string, MetadataObject>? metadataDict = JsonConvert.DeserializeObject<Dictionary<string, MetadataObject>>(metadataJsonString);
            metadataDict ??= new Dictionary<string, MetadataObject>();
            return metadataDict;
        }

        // ONLY function that should ever write to the metadata file
        private void UpdateMetadataFile(MetadataObject? metadataObject)
        {
            Console.WriteLine("Dequeuing, updating metadata file with object for ID: " + metadataObject?.ResourceId);
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

                // TODO: (Lock might have fixed this) Following error can still occur despite queue: The process cannot access the file because it is being used by another process
                //File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
                lock (Globals.FileAccessLock)
                {
                    File.WriteAllText(pathToMetadata, updatedMetadataJsonString);
                }
                
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
