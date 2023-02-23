using System.Net;
using System.Reflection;

namespace Repository.App
{
    public class ResourceRetriever
    {
        static string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        // Should this be async? async Task<string>
        public static string GetResource(string resourceName)
        {
            // TODO: Find file format and call the corresponding function
            // (if it's a .xes it will be in Logs, if .bpmn it will be in BPMN)

            // if (resourceName.Contains(".xes", StringComparison.OrdinalIgnoreCase))
            string saveLocation = "Logs";

            return Load(resourceName, saveLocation);
        }

        private static string Load(string resourceName, string resourceLocation)
        {
            string path = Path.Combine(pathToResources, resourceLocation);
            string filePath = Path.Combine(path, resourceName);
            string log = File.ReadAllText(filePath);
            return log;
        }

        // Alternative way. Might be better for streaming a file
        public static string GetFilePath(string resourceName)
        {
            // TODO: Find file format and call the corresponding function
            // (if it's a .xes it will be in Logs, if .bpmn it will be in BPMN)

            // if (resourceName.Contains(".xes", StringComparison.OrdinalIgnoreCase))
            string resourceLocation = "Logs";

            string path = Path.Combine(pathToResources, resourceLocation);
            string filePath = Path.Combine(path, resourceName);

            return filePath;
        }

        // If we want separate functions for each type?
        //GetPNML
        //string pathToPNML = Path.Combine(pathToResources, "PNML");
        //string pnmlName = Path.Combine(pathToPNML, resourceName);

        // GetImg
        //string pathToImages = Path.Combine(pathToResources, "Images");
        //string imageName = Path.Combine(pathToImages, resourceName);

        // GetBPMN
        // GetHTML
        // GetBIN
        // ...
    }
}
