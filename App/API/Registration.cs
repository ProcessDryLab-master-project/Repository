using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Repository.App.Entities;

namespace Repository.App.API
{
    public class Registration
    {
        public static string GetConfiguration()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            string config = File.ReadAllText(path);
            var jsonConfig = JObject.Parse(config); // To remove comments
            string configFormat = JsonConvert.SerializeObject(jsonConfig, Formatting.Indented);
            return configFormat;
        }
    }
}
