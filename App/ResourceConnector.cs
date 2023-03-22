using csdot;
using csdot.Attributes.DataTypes;

namespace Repository.App
{
    public class ResourceConnector
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToVisualization = Path.Combine(pathToResources, "Visualization");
        static readonly string pathToDot = Path.Combine(pathToVisualization, "DOT");
        static HashSet<string> exploredNodes = new HashSet<string>();
        public static IResult GetGraphForResource(string resourceId)
        {
            var requestedMdObject = DBManager.GetMetadataObjectById(resourceId);
            //string graphId = Guid.NewGuid().ToString();
            string graphId = "test";
            string pathToFile = Path.Combine(pathToDot, graphId + ".dot");
            //File.AppendAllText(pathToFile, "");

            // Build graph
            Graph graph = new Graph(graphId);
            //graph.strict = true;
            graph.type = "digraph";

            Node centerNode = new Node($"\"{resourceId}\"");
            centerNode.Attribute.color.Value = Color.X11.blue; // Color it because it's the requested node.
            centerNode.Attribute.label.Value = requestedMdObject.ResourceLabel;// "Center label";
            Console.WriteLine("Getting graph for requested object: " + requestedMdObject.ResourceLabel);
            graph.AddElement(centerNode);

            InsertNode(graph, centerNode, requestedMdObject, resourceId);

            DotDocument dotDocument = new DotDocument();
            dotDocument.SaveToFile(graph, pathToFile);

            return Results.File(pathToFile, resourceId);
        }
        public static void InsertNode(Graph graph, Node node, MetadataObject? mdObject, string resourceId)
        {
            if(mdObject == null) { return; } // The node is not part of this repository.

            var parentList = mdObject.GenerationTree.Parents;
            foreach (var relativeId in parentList ?? Enumerable.Empty<string>())
            {
                if (!exploredNodes.Contains(relativeId + resourceId))
                {
                    exploredNodes.Add(relativeId + resourceId);
                    Node relativeNode = new Node($"\"{relativeId}\"");

                    MetadataObject? relativeMdObject = DBManager.GetMetadataObjectById(relativeId);

                    if(relativeMdObject == null) relativeNode.Attribute.color.Value = Color.X11.red; // Red because it's not part of this repository
                    else relativeNode.Attribute.label.Value = relativeMdObject.ResourceLabel;   // Can only use label if it's part of this repo
                    graph.AddElement(relativeNode);

                    List<Transition> transition = new List<Transition>()
                    {
                        new Transition(relativeNode, EdgeOp.directed),
                        new Transition(node, EdgeOp.unspecified),
                    };
                    Edge edge = new Edge(transition);
                    graph.AddElement(edge);
                    //graph.AddElements(relativeNode, edge);

                    InsertNode(graph, relativeNode, relativeMdObject, relativeId);
                }
            }

            var childList = mdObject.GenerationTree.Children;
            foreach (var relativeId in childList ?? Enumerable.Empty<string>())
            {
                if (!exploredNodes.Contains(resourceId + relativeId))
                {
                    exploredNodes.Add(resourceId + relativeId);
                    Node relativeNode = new Node($"\"{relativeId}\"");

                    MetadataObject relativeMdObject = DBManager.GetMetadataObjectById(relativeId);
                    relativeNode.Attribute.label.Value = relativeMdObject.ResourceLabel;
                    List<Transition> transition = new List<Transition>()
                    {
                        new Transition(node, EdgeOp.directed),
                        new Transition(relativeNode, EdgeOp.unspecified),
                    };
                    Edge edge = new Edge(transition);
                    graph.AddElements(relativeNode, edge);

                    InsertNode(graph, relativeNode, relativeMdObject, relativeId);
                }
            }
        }

        //public static void InsertNode(Graph graph, Node? relativeNode, string resourceId)
        //{
        //    MetadataObject mdObject = DBManager.GetMetadataObjectById(resourceId);
        //    Node node = (Node) graph.GetElementByName(resourceId, "node");
        //    if(node == null)
        //    {   // Add current node, if node doesn't already exist:
        //        node = new Node($"\"{resourceId}\"");
        //        graph.AddElement(node);
        //        node.Attribute.label.Value = mdObject.ResourceLabel;
        //    }
        //    if(relativeNode != null)
        //    {   // Add transition unless no relative node was provided (like the first time this is called).
        //        List<Transition> transition = new List<Transition>()
        //        {
        //            new Transition(node, EdgeOp.directed),
        //            new Transition(relativeNode, EdgeOp.unspecified),
        //        };
        //        Edge edge = new Edge(transition);
        //        graph.AddElement(edge);
        //    }

        //    var parentList = mdObject.GenerationTree.Parents;
        //    var childList = mdObject.GenerationTree.Children;
        //    foreach (var parentId in parentList ?? Enumerable.Empty<string>())
        //    {
        //        if (!exploredNodes.Contains(parentId + resourceId))
        //        {
        //            exploredNodes.Add(parentId + resourceId);
        //            InsertNode(graph, node, parentId);
        //        }

        //    }
        //    foreach (var childId in childList ?? Enumerable.Empty<string>())
        //    {
        //        if (!exploredNodes.Contains(resourceId+childId))
        //        {
        //            exploredNodes.Add(resourceId+childId);
        //            InsertNode(graph, node, childId);
        //        }
        //    }
        //}

        //private static void IterateAndAdd(Graph graph, Node node, List<string>? relativeList)
        //{
        //    foreach (var relativeId in relativeList ?? Enumerable.Empty<string>())
        //    {
        //        MetadataObject mdObject = DBManager.GetMetadataObjectById(relativeId);

        //        Node relativeNode = new Node($"\"{relativeId}\"");
        //        relativeNode.Attribute.label.Value = mdObject.ResourceLabel;
        //        List<Transition> transition = new List<Transition>()
        //        {
        //            new Transition(relativeNode, EdgeOp.directed),
        //            new Transition(node, EdgeOp.unspecified),
        //        };
        //        Edge edge = new Edge(transition);
        //        graph.AddElements(relativeNode, edge);

        //        InsertNode(graph, relativeNode, relativeId);
        //    }
        //}

        public static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
