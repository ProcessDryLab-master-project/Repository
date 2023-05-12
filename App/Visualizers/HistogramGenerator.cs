using Newtonsoft.Json;
using Repository.App;
using Repository.App.API;
using Repository.App.Database;
using Repository.App.Entities;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.AccessControl;
using System.Text;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Repository.App.Visualizers
{
    public class HistogramGenerator
    {
        static DatabaseManager databaseManager = new DatabaseManager(new MetadataDb());
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToHistogram = Path.Combine(pathToResources, "Histogram");
        static readonly string pathToEventLog = Path.Combine(pathToResources, "EventLog");
        public static IResult GetHistogram(string resourceId, string appUrl)
        {
            MetadataObject? logMetadataObject = databaseManager.GetMetadataObjectById(resourceId);
            if (logMetadataObject == null || logMetadataObject.ResourceInfo?.FileExtension == null) return Results.BadRequest("Invalid resource ID. No reference to resource could be found.");


            string? requestedFileExtension = logMetadataObject.ResourceInfo.FileExtension;
            string nameOfFile = string.IsNullOrWhiteSpace(requestedFileExtension) ? resourceId : resourceId + "." + requestedFileExtension;
            string pathToRequestFile = Path.Combine(pathToEventLog, nameOfFile);
            if (!File.Exists(pathToRequestFile))
            {
                string badResponse = "No file of type EventLog exists for path " + pathToRequestFile; // TODO: Should not return the entire path, just easier like this for now
                return Results.BadRequest(badResponse);
            }

            List<string>? childrenIds = logMetadataObject.GenerationTree?.Children?.Select(child => child.ResourceId).ToList();
            foreach (var childId in childrenIds ?? Enumerable.Empty<string>())
            {
                var childMetadata = databaseManager.GetMetadataObjectById(childId);
                if (childMetadata != null && childMetadata.ResourceInfo.ResourceType == "Histogram")
                {
                    var result = ResourceRetriever.GetResourceById(childId);
                    if (!result.GetType().IsInstanceOfType(Results.BadRequest()))
                    {
                        Console.WriteLine("Histogram already exist, returning this");
                        return ResourceRetriever.GetResourceById(childId);
                    }
                    return Results.BadRequest("Resource has child Histogram that does not exist in the repository. This should not happen, consider removing as child and run again");
                    //List<Child>? mdChildren = logMetadataObject.GenerationTree?.Children;
                    //mdChildren?.Remove(mdChildren.First(child => child.ResourceId == childId));
                    //DBManager.UpdateMetadataFile(logMetadataObject, childId);
                }
            }

            Console.WriteLine("No Histogram exist for resource, generating new one");
            var histogramDict = GenerateHistogramDict(pathToRequestFile);
            string jsonList = ConvertToJsonList(histogramDict);
            string pathToSave = AddHistogramToMetadata(resourceId, appUrl, logMetadataObject);

            if (!Directory.Exists(pathToHistogram))
            {
                Console.WriteLine("No folder exists for JSON, creating " + pathToHistogram);
                Directory.CreateDirectory(pathToHistogram);
            }
            File.WriteAllText(pathToSave, jsonList);
            return Results.Text(jsonList, contentType: "application/json");
            //return Results.File(pathToSave, GUID); // If we would want to return the file instead?
        }

        private static string AddHistogramToMetadata(string logResourceId, string appUrl, MetadataObject? logMetadataObject)
        {
            string histResourceLabel = $"Histogram from log: {logMetadataObject.ResourceInfo.ResourceLabel}";
            string histResourceId = Guid.NewGuid().ToString();
            string host = $"{appUrl}/resources/";
            string histDescription = $"Histogram generated from log with label {logMetadataObject.ResourceInfo.ResourceLabel} and ID: {logMetadataObject.ResourceId}";
            HashSet<Parent> parents = new()
            {
                new Parent()
                {
                    ResourceId = logResourceId,
                    UsedAs = "Log",
                }
            };
            var metadataObject = databaseManager.BuildMetadataObject(histResourceId, histResourceLabel, resourceType: "Histogram", host, histDescription, fileExtension: "json", parents: parents);
            databaseManager.Add(metadataObject);
            string pathToSave = Path.Combine(pathToHistogram, $"{histResourceId}.json");
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
