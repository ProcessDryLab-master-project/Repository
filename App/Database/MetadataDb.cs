using Newtonsoft.Json;
using Repository.App.Entities;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Repository.App.Database
{
    public class MetadataDb : IMetadataDb
    {
        static ConcurrentDictionary<string, long> dynamicFiles = new();
        // 1.000ms = 1s. 30.000ms = 30s. 300.000ms = 5min
        static readonly long millisecondsToWait = 30000; // 30 seconds without updates before Dynamic is set to false

        static Thread? DynamicThread;
        public MetadataDb()
        {
            #region dynamicthread
            if (DynamicThread == null)
            {
                Console.WriteLine("Creating DynamicThread");
                DynamicThread = new Thread(() =>
                {
                    while (true)
                    {
                        foreach (var dynamicFile in dynamicFiles)
                        {
                            var currentTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                            var newTime = currentTime - millisecondsToWait;
                            if (dynamicFile.Value < newTime)
                            {
                                Console.WriteLine($"No update to dynamic in {(dynamicFile.Value - currentTime) * 1000} seconds. Setting Dynamic to false and removing from dynamicFiles.");
                                SetDynamicToFalse(dynamicFile.Key);
                                dynamicFiles.TryRemove(dynamicFile);
                            }
                        }
                        Thread.Sleep((int)millisecondsToWait); // Sleep here to reduce processing requirements
                    }
                });
                DynamicThread.Name = Guid.NewGuid().ToString();
                DynamicThread.Start();
            }
            #endregion
        }
        public bool ContainsKey(string key)
        {
            return GetMetadataDict().ContainsKey(key);
        }

        public MetadataObject? GetMetadataObjectById(string resourceId)
        {
            var metadataDict = GetMetadataDict();
            MetadataObject? metadataObj = metadataDict.GetValue(resourceId);
            if (metadataObj == null) return null;
            metadataObj.ResourceId = resourceId;
            return metadataObj;
        }

        public void MetadataWrite(MetadataObject metadataObject)
        {
            try
            {
                string? resourceId = metadataObject?.ResourceId;
                if (string.IsNullOrWhiteSpace(resourceId))
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
                    metadataObject.ResourceId = resourceId;
                    return;
                }

                metadataDict[resourceId] = metadataObject;
                UpdateParentResource(metadataDict, resourceId);

                string updatedMetadataJsonString = JsonConvert.SerializeObject(metadataDict, Formatting.Indented);
                Console.WriteLine("Writing to metadata for resource id " + resourceId);

                updatedMetadataJsonString.Write(Globals.pathToMetadata);
                metadataObject.ResourceId = resourceId; // We only remove it before printing to metadata to make it cleaner. We add it here again so it can be used when the object is passed around.
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

        public void UpdateDynamicResourceTime(string resourceId)
        {
            dynamicFiles[resourceId] = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private void SetDynamicToFalse(string resourceId)
        {
            var metadataObj = GetMetadataDict()!.GetValue(resourceId);
            if(metadataObj == null ) { return; }
            metadataObj.ResourceId = resourceId;
            metadataObj.ResourceInfo.Dynamic = false;
            MetadataWrite(metadataObj);
        }

        public Dictionary<string, MetadataObject> GetMetadataDict()
        {
            string metadataJsonString;
            using (var fileStream = File.Open(Globals.pathToMetadata, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamReader rdr = new StreamReader(fileStream))
                {
                    metadataJsonString = rdr.ReadToEnd();
                }
            }
            
            Dictionary<string, MetadataObject>? metadataDict = JsonConvert.DeserializeObject<Dictionary<string, MetadataObject>>(metadataJsonString);
            metadataDict ??= new Dictionary<string, MetadataObject>();
            return metadataDict;
        }

        private static void UpdateParentResource(Dictionary<string, MetadataObject> metadataDict, string resourceId)
        {
            HashSet<Parent> parents = metadataDict.GetValue(resourceId)?.GenerationTree?.Parents;
            // Add own ID as child to parent resource
            foreach (var parent in parents ?? Enumerable.Empty<Parent>())
            {
                string parentId = parent.ResourceId;
                var parentObj = metadataDict.GetValue(parentId);
                if (parentObj == null) return;              // If we can't find parentObj in metadata, do nothing (likely means it exists in another repo or has been deleted).
                parentObj.GenerationTree.Children ??= new HashSet<Child>();  // If children are null, initialize
                parentObj.GenerationTree.Children.Add(new Child
                {
                    ResourceId = resourceId,
                });
                metadataDict[parentId] = parentObj;           // Overwrite with updated parentObj
            }
        }
    }
}
