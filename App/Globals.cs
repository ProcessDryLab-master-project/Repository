namespace Repository.App
{
    public class Globals
    {
        public static object FileAccessLock = new object(); // Locks writes to files. Source: https://stackoverflow.com/questions/33627449/replace-line-in-file-during-multiple-threads
        public static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        public static readonly string pathToMetadata = Path.Combine(pathToResources, "resourceMetadata.json");
        public static readonly string pathToHistogram = Path.Combine(pathToResources, "Histogram");
        public static readonly string pathToEventLog = Path.Combine(pathToResources, "EventLog");
        public static readonly string pathToDot = Path.Combine(pathToResources, "DOT");
    }
}
