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
        IMetadataDb metadataDb { get; set; }
        public HistogramGenerator(IMetadataDb dataInterface)
        {
            metadataDb = dataInterface;
        }
        //static DatabaseManager databaseManager = new DatabaseManager(new MetadataDb());
        //static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        //static readonly string pathToHistogram = Path.Combine(pathToResources, "Histogram");
        //static readonly string pathToEventLog = Path.Combine(pathToResources, "EventLog");
        public IResult GetHistogram(string resourceId, string appUrl)
        {
            MetadataObject? logMetadataObject = metadataDb.GetMetadataObjectById(resourceId);
            if (logMetadataObject == null || logMetadataObject.ResourceInfo?.FileExtension == null) return Results.BadRequest("Invalid resource ID. No reference to resource could be found.");


            string? requestedFileExtension = logMetadataObject.ResourceInfo.FileExtension;
            string nameOfFile = string.IsNullOrWhiteSpace(requestedFileExtension) ? resourceId : resourceId + "." + requestedFileExtension;
            string pathToRequestFile = Path.Combine(Globals.pathToEventLog, nameOfFile);
            if (!File.Exists(pathToRequestFile))
            {
                string badResponse = "No file of type EventLog exists for path " + pathToRequestFile; // TODO: Should not return the entire path, just easier like this for now
                return Results.BadRequest(badResponse);
            }

            List<string>? childrenIds = logMetadataObject.GenerationTree?.Children?.Select(child => child.ResourceId).ToList();
            foreach (var childId in childrenIds ?? Enumerable.Empty<string>())
            {
                var childMetadata = metadataDb.GetMetadataObjectById(childId);
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
            string histResourceId = Guid.NewGuid().ToString();
            var histogramDict = GenerateHistogramDict(pathToRequestFile);
            string jsonList = ConvertToJsonList(histogramDict);
            string pathToSave = Path.Combine(Globals.pathToHistogram, $"{histResourceId}.json");

            if (!Directory.Exists(Globals.pathToHistogram))
            {
                Console.WriteLine("No folder exists for JSON, creating " + Globals.pathToHistogram);
                Directory.CreateDirectory(Globals.pathToHistogram);
            }
            File.WriteAllText(pathToSave, jsonList); // TODO: Consider if this should use the lock. It should never be possible to write the same histogram multiple times, so it might not be needed
            AddHistogramToMetadata(resourceId, histResourceId, appUrl, logMetadataObject);
            return Results.Text(jsonList, contentType: "application/json");
            //return Results.File(pathToSave, GUID); // If we would want to return the file instead?
        }

        private void AddHistogramToMetadata(string logResourceId, string histResourceId, string appUrl, MetadataObject? logMetadataObject)
        {
            HashSet<Parent> parents = new()
            {
                new Parent()
                {
                    ResourceId = logResourceId,
                    UsedAs = "EventLog",
                }
            };
            string parentsAsString = JsonConvert.SerializeObject(parents);
            Dictionary<string, string> metadataKeys = new()
            {
                { "ResourceLabel", $"Histogram from log: {logMetadataObject.ResourceInfo.ResourceLabel}"},
                { "ResourceType", "Histogram"},
                { "Host", $"{appUrl}/resources/"},
                { "Description", $"Histogram generated from log with label {logMetadataObject.ResourceInfo.ResourceLabel} and ID: {logMetadataObject.ResourceId}"},
                { "FileExtension", "json"},
                { "Parents", parentsAsString},

            };
            var metadataObject = DbHelper.BuildMetadataObject(metadataKeys, histResourceId);
            metadataDb.MetadataWrite(metadataObject);
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
