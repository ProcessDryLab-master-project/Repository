using csdot;
using csdot.Attributes.DataTypes;

namespace Repository.App
{
    public class ResourceConnector
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToVisualization = Path.Combine(pathToResources, "Visualization");
        static readonly string pathToDot = Path.Combine(pathToVisualization, "DOT");
        public static IResult GetGraphForResource(string resourceId)
        {
            var requestedMdObject = DBManager.GetMetadataObjectById(resourceId);
            //string graphId = Guid.NewGuid().ToString();
            string graphId = "test";
            string pathToFile = Path.Combine(pathToDot, graphId + ".dot");
            //File.AppendAllText(pathToFile, "");

            // Build graph
            Graph graph = new Graph(graphId);
            graph.strict = true;
            graph.type = "diagraph";
            Node centerNode = new Node(resourceId);
            centerNode.Attribute.color.Value = Color.X11.blue; // Color it because it's the requested node.
            centerNode.Attribute.label.Value = "Center label";
            graph.AddElement(centerNode);
            foreach (var parent in requestedMdObject.GenerationTree.Parents)
            {
                Node node = new Node(parent);
                List<Transition> transition = new List<Transition>()
                {
                    new Transition(node, EdgeOp.directed),
                    new Transition(centerNode, EdgeOp.unspecified),
                };
                Edge edge = new Edge(transition);
                graph.AddElements(node, edge);
            }
            DotDocument dotDocument = new DotDocument();
            dotDocument.SaveToFile(graph, pathToFile);

            return Results.File(pathToFile, resourceId);
        }
    }
}
