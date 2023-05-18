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
        public static string CreateHistogram(MetadataObject logMetadataObject)
        {
            string path = DbHelper.GetFileSavePath(logMetadataObject);
            var histogramDict = GenerateHistogramDict(path);
            string jsonListStr = ConvertToJsonList(histogramDict);
            return jsonListStr;


            //File.WriteAllText(pathToSave, jsonList); // TODO: Consider if this should use the lock. It should never be possible to write the same histogram multiple times, so it might not be needed
            //return Results.Text(jsonListStr, contentType: "application/json");
            //return Results.File(pathToSave, GUID); // If we would want to return the file instead?
        }

        public static MetadataObject CreateHistogramMetadata(string logResourceId, string appUrl, MetadataObject? logMetadataObject)
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
            //Dictionary<string, string> metadataKeys = new()
            //{
            //    { "ResourceLabel", $"Histogram from log: {logMetadataObject.ResourceInfo.ResourceLabel}"},
            //    { "ResourceType", "Histogram"},
            //    { "Host", $"{appUrl}/resources/"},
            //    { "Description", $"Histogram generated from log with label {logMetadataObject.ResourceInfo.ResourceLabel} and ID: {logResourceId}"},
            //    { "FileExtension", "json"},
            //    { "Parents", parentsAsString},

            //};
            //var metadataObject = DbHelper.BuildMetadataObject(metadataKeys);
            FormObject formObject = new()
            {
                ResourceLabel =$"Histogram from log: {logMetadataObject.ResourceInfo.ResourceLabel}",
                ResourceType = "Histogram",
                Host = $"{appUrl}/resources/",
                Description = $"Histogram generated from log with label {logMetadataObject.ResourceInfo.ResourceLabel} and ID: {logResourceId}",
                FileExtension = "json",
                Parents = parentsAsString,
            };
            var metadataObject = DbHelper.BuildMetadataObject(formObject);
            return metadataObject;
            //metadataDb.MetadataWrite(metadataObject);
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
