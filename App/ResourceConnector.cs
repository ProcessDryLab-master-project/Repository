using csdot;
using csdot.Attributes.DataTypes;

namespace Repository.App
{
    public class ResourceConnector
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToDot = Path.Combine(pathToResources, "DOT");
        public static IResult GetGraphForResource(string resourceId)
        {
            var requestedMdObject = DBManager.GetMetadataObjectById(resourceId);
            string graphId = Guid.NewGuid().ToString();
            string pathToFile = Path.Combine(pathToDot, graphId + ".dot");

            // Build graph
            Graph graph = new Graph(graphId);
            graph.strict = true;
            graph.type = "diagraph";
            Node centerNode = new Node(resourceId);

            foreach (var parent in requestedMdObject.GenerationTree.Parents)
            {
                Node node = new Node(parent);
                Edge edge = new Edge();
                List<Transition> transition = new List<Transition>()
                {
                    new Transition(centerNode, EdgeOp.directed),
                    //new Transition(b, EdgeOp.undirected),
                    //new Transition(c, EdgeOp.unspecified)
                };
                edge.Transition = transition;
                //centerNode
            }


            return Results.File(pathToFile, resourceId);
        }
    }
}
