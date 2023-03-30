using Newtonsoft.Json;
using Repository.App;
using System.Data.SqlTypes;
using System.Reflection.PortableExecutable;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Repository.Visualizers
{
    public class HistogramGenerator
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToJson = Path.Combine(pathToResources, "JSON");
        public static IResult GetHistogram(string resourceId, string appUrl)
        {
            Console.WriteLine("Creating histogram for requested object: " + resourceId);
            MetadataObject? metadataObject = DBManager.GetMetadataObjectById(resourceId);
            if (metadataObject == null || metadataObject.ResourceInfo?.FileExtension == null) return Results.BadRequest("Invalid resource ID. No reference to resource could be found.");
            
            string pathToFileExtension = Path.Combine(pathToResources, metadataObject.ResourceInfo.FileExtension.ToUpper());
            string pathToFile = Path.Combine(pathToFileExtension, resourceId + "." + metadataObject.ResourceInfo.FileExtension);
            if (!File.Exists(pathToFile) || metadataObject.ResourceInfo?.ResourceType != "EventLog")
            {
                string badResponse = "No file of type EventLog exists for path " + pathToFile; // TODO: Should not return the entire path, just easier like this for now
                return Results.BadRequest(badResponse);
            }

            List<string>? childrenIds = metadataObject.GenerationTree?.Children?.Select(child => child.ResourceId).ToList();
            foreach (var childId in childrenIds ?? Enumerable.Empty<string>())
            {
                var childMetadata = DBManager.GetMetadataObjectById(childId);
                if (childMetadata != null && childMetadata.ResourceInfo.ResourceType == "Histogram")
                {
                    Console.WriteLine("Histogram already exist, returning this");
                    return ResourceRetriever.GetResourceById(childId);
                }
            }

            Console.WriteLine("No Histogram exist for resource, generating new one");
            var histogramDict = GenerateHistogramDict(pathToFile);
            string jsonList = ConvertToJsonList(histogramDict);
            string pathToSave = AddHistogramToMetadata(resourceId, appUrl, metadataObject);

            File.WriteAllText(pathToSave, jsonList);
            return Results.Text(jsonList, contentType: "application/json");
            //return Results.File(pathToSave, GUID); // If we would want to return the file instead?
        }

        private static string AddHistogramToMetadata(string resourceId, string appUrl, MetadataObject? metadataObject)
        {
            string resourceLabel = $"Histogram from log: {metadataObject.ResourceInfo.ResourceLabel}";
            string GUID = Guid.NewGuid().ToString();
            string host = $"{appUrl}/resources/";
            string description = $"Histogram generated from log with label {metadataObject.ResourceInfo.ResourceLabel} and ID: {metadataObject.ResourceId}";
            //GeneratedFrom generatedFrom = new() { SourceHost = host };
            //List<Parent> parents = new()
            //{
            //    new Parent()
            //    {
            //        ResourceId = resourceId,
            //        UsedAs = "Log",
            //    }
            //};
            DBManager.AddToMetadata(resourceLabel, resourceType: "Histogram", GUID, host, description, fileExtension: "json");
            //DBManager.AddToMetadata(resourceLabel, resourceType: "Histogram", GUID, host, generatedFrom: generatedFrom, parents: parents, description, fileExtension: "json");
            string pathToSave = Path.Combine(pathToJson, $"{GUID}.json");
            return pathToSave;
        }

        // Convert dictionary to list in format that Frontend takes
        private static string ConvertToJsonList(Dictionary<string, int> histogramDict)
        {
            List<List<dynamic>> histogramList = new();
            foreach (var eventDict in histogramDict)
            {
                List<dynamic> tmpList = new()
                {
                    eventDict.Key,
                    eventDict.Value,
                };
                histogramList.Add(tmpList);
            }
            var jsonList = JsonConvert.SerializeObject(histogramList, Newtonsoft.Json.Formatting.Indented);
            return jsonList;
        }

        // Count number of events and save in histogram dictionary
        private static Dictionary<string, int> GenerateHistogramDict(string pathToFile)
        {
            Dictionary<string, int> histogramDict = new Dictionary<string, int>();
            XmlDocument doc = new XmlDocument();
            doc.Load(pathToFile);
            foreach (XmlNode traceNode in doc.DocumentElement.ChildNodes)
            {
                if (traceNode.Name == "trace")
                {
                    //Console.WriteLine($"\n\nNew Trace:");
                    foreach (XmlNode eventNode in traceNode.ChildNodes)
                    {
                        if (eventNode.Name == "event")
                        {
                            foreach (XmlNode eventAttribute in eventNode.ChildNodes)
                            {
                                string eventKey = eventAttribute.Attributes["key"].Value;
                                if (eventKey == "concept:name")
                                {
                                    string eventValue = eventAttribute.Attributes["value"].Value;
                                    //Console.WriteLine("EventKey: " + eventKey);
                                    //Console.WriteLine("Attribute value: " + eventValue);
                                    if (histogramDict.ContainsKey(eventValue))
                                    {
                                        histogramDict[eventValue] += 1;
                                    }
                                    else
                                    {
                                        histogramDict[eventValue] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return histogramDict;
        }
    }
}
