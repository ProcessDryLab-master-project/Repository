using Repository.App;
using System.Data.SqlTypes;
using System.Xml;

namespace Repository.Visualizers
{
    public class HistogramGenerator
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        public static IResult GetHistogram(string resourceId)
        {
            // TODO: Check if a histogram already exists for this file
            Console.WriteLine("Creating histogram for requested object: " + resourceId);
            MetadataObject? metadataObject = DBManager.GetMetadataObjectById(resourceId);
            if (metadataObject == null || metadataObject.ResourceInfo?.FileExtension == null) return Results.BadRequest("Invalid resource ID. No reference to resource could be found.");
            string pathToFileExtension = Path.Combine(pathToResources, metadataObject.ResourceInfo.FileExtension.ToUpper()); // TODO: Add null check or try/catch
            string pathToFile = Path.Combine(pathToFileExtension, resourceId + "." + metadataObject.ResourceInfo.FileExtension);
            if (!File.Exists(pathToFile) || metadataObject.ResourceInfo?.ResourceType != "EventLog")
            {
                string badResponse = "No file of type EventLog exists for path " + pathToFile; // TODO: Should not return the entire path, just easier like this for now
                return Results.BadRequest(badResponse);
            }

            XmlTextReader reader = new XmlTextReader(pathToFile);

            while(reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        Console.Write("<" + reader.Name);

                        while (reader.MoveToNextAttribute()) // Read the attributes.
                            Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                        Console.Write(">");
                        Console.WriteLine(">");
                        break;
                    case XmlNodeType.Text: //Display the text in each element.
                        Console.WriteLine(reader.Value);
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        Console.Write("</" + reader.Name);
                        Console.WriteLine(">");
                        break;
                }
                //if (reader.NodeType )
                //switch (reader.NodeType)
                //{
                //    case XmlNodeType.Element: // The node is an element.
                //        Console.Write("<" + reader.Name);
                //        Console.WriteLine(">");
                //        break;

                //    case XmlNodeType.Text: //Display the text in each element.
                //        Console.WriteLine(reader.Value);
                //        break;

                //    case XmlNodeType.EndElement: //Display the end of the element.
                //        Console.Write("</" + reader.Name);
                //        Console.WriteLine(">");
                //        break;
                //}
            }
            //XmlDocument doc = new XmlDocument();
            //doc.Load("c:\\temp.xml");

            return Results.File(pathToFile, resourceId);
        }
    }
}
