﻿using csdot;
using csdot.Attributes.DataTypes;
using Microsoft.AspNetCore.Http;
using Repository.App;

namespace Repository.Visualizers
{
    public class ResourceConnector
    {
        static readonly string pathToResources = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        static readonly string pathToDot = Path.Combine(pathToResources, "DOT");
        public static IResult GetGraphForResource(string resourceId)
        {
            HashSet<string> exploredNodes = new HashSet<string>();
            Console.WriteLine("Getting graph for requested object: " + resourceId);
            MetadataObject? requestedMdObject = DBManager.GetMetadataObjectById(resourceId);
            if (requestedMdObject == null) return Results.BadRequest("No resource exist for that ID");

            string graphId = Guid.NewGuid().ToString();
            Graph graph = new Graph($"\"{graphId}\"");
            graph.type = "digraph";

            Node centerNode = new Node($"\"{resourceId}\"");
            centerNode.Attribute.color.Value = Color.X11.blue; // Color it because it's tec62e128-adaf-4907he requested node.
            centerNode.Attribute.label.Value = requestedMdObject.ResourceInfo.ResourceLabel;// "Center label";
            graph.AddElement(centerNode);

            RecursiveInsert(graph, centerNode, requestedMdObject, resourceId, exploredNodes);

            // This if we want to save as a file and send it as IResult instead. Can convert the dot file to svg with this command: dot -Tsvg test.dot > test.svg
            //string pathToFile = Path.Combine(pathToDot, graphId + ".dot");
            //DotDocument dotDocument = new DotDocument();
            //dotDocument.SaveToFile(graph, pathToFile);
            //return Results.File(pathToFile, resourceId);

            return Results.Ok(graph.ElementToString());
        }
        public static void RecursiveInsert(Graph graph, Node node, MetadataObject? mdObject, string resourceId, HashSet<string> exploredNodes)
        {
            if (mdObject == null) { return; } // The node is not part of this repository.

            var parentList = mdObject.GenerationTree.Parents;
            foreach (var relative in parentList ?? Enumerable.Empty<Parent>())
            {
                string relativeId = relative.ResourceId;
                string relativeFrom = relative.From;
                if (!exploredNodes.Contains(relativeId + resourceId))
                {
                    exploredNodes.Add(relativeId + resourceId);
                    CreateNodeAndEdge(graph, node, relativeId, false, exploredNodes);
                }
            }

            var childList = mdObject.GenerationTree.Children;
            foreach (var relative in childList ?? Enumerable.Empty<Child>())
            {
                string relativeId = relative.ResourceId;
                if (!exploredNodes.Contains(resourceId + relativeId))
                {
                    exploredNodes.Add(resourceId + relativeId);
                    CreateNodeAndEdge(graph, node, relativeId, true, exploredNodes);
                }
            }
        }

        private static void CreateNodeAndEdge(Graph graph, Node currentNode, string relativeId, bool isChild, HashSet<string> exploredNodes)
        {
            Node relativeNode = new Node($"\"{relativeId}\"");

            MetadataObject? relativeMdObject = DBManager.GetMetadataObjectById(relativeId);
            if (relativeMdObject == null) relativeNode.Attribute.color.Value = Color.X11.red; // Red because it's not part of this repository
            else relativeNode.Attribute.label.Value = relativeMdObject.ResourceInfo.ResourceLabel;   // Can only use label if it's part of this repo

            Edge edge;
            if (isChild) { edge = DirectedEdge(currentNode, relativeNode); }
            else { edge = DirectedEdge(relativeNode, currentNode); }

            graph.AddElements(relativeNode, edge);
            RecursiveInsert(graph, relativeNode, relativeMdObject, relativeId, exploredNodes);
        }

        private static Edge DirectedEdge(Node parent, Node child)
        {
            List<Transition> transition = new List<Transition>()
                    {
                        new Transition(parent, EdgeOp.directed),
                        new Transition(child, EdgeOp.unspecified),
                    };
            Edge edge = new Edge(transition);
            return edge;
        }
    }
}
