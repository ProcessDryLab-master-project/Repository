namespace Repository.App.API
{
    public class Registration
    {
        public static string GetConfiguration()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            string config = File.ReadAllText(path);
            return config;
        }
    }
}
