namespace Repository.App
{
    public class ResourceReceiver
    {
        static string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        public async Task SaveResource(string resourceName, string resource)
        {
            // Find file format to see where it should be saved
            string saveLocation = "Logs"; // BPMN, PNML, ...
            Save(resourceName, resource, saveLocation);
        }

        private async Task Save(string resourceName, string resource, string saveLocation)
        {
            string path = Path.Combine(pathToResources, saveLocation);
            string filePath = Path.Combine(path, resourceName);

            await File.WriteAllTextAsync(filePath, resource);
        }
    }
}
