using System.Collections.Generic;
using UnityEngine;

namespace RoadArchitect {
    public class GraphEdge
    {
        public double Distance;
        public double SpeedLimit;
        public double Cost;
        public GraphNode EndNode;
        public GraphEdge(GraphNode endNode, double distance, double speedLimit)
        {
            EndNode = endNode;
            Distance = distance;
            SpeedLimit = speedLimit;
            Cost = distance / speedLimit;
        }

    }
    public class GraphNode
    {
        public SplineN Node;
        public List<GraphEdge> Edges = new List<GraphEdge>();
        public GraphNode(SplineN startNode)
        {
            this.Node = startNode;
        }
    }
    public static class RoadSystemGraph
    {
        private const string GRAPH_NODE_SPHERE_NAME = "Graph Node";
        private const float GRAPH_NODE_SPHERE_SIZE = 15f;
        private const float EDGE_LINE_WIDTH = 5f;
        // The road graph, key is the corresponding splines node's uID
        private static Dictionary<string, GraphNode> roadSystemGraph;
        /// <summary> Generates a road graph from the given road system </summary>
        public static Dictionary<string, GraphNode> GenerateRoadGraph(RoadSystem roadSystem)
        {
            // Create an empty road
            roadSystemGraph = new Dictionary<string, GraphNode>();
            
            // Update all connected nodes
            Navigation.UpdateConnectedNodes();
            
            // Loop through all roads in the road system
            foreach(Road road in roadSystem.transform.GetComponentsInChildren<Road>())
            {
                // Get the first node in the road. We only need to find one node in the road as all will be connected
                SplineN startSpline = road.transform.Find("Splines").GetChild(0).GetComponent<SplineN>();
                
                // Map the road into the graph
                MapRoadSystem(startSpline);
            }
            return roadSystemGraph;
        }

        /// <summary> Maps the road onto the road graph </summary>
        private static void MapRoadSystem(SplineN startNode)
        {
            // Create a new GraphNode from the start node
            GraphNode startGraphNode = new GraphNode(startNode);

            // Add the graph node to the road system graph
            roadSystemGraph.Add(startNode.uID, startGraphNode);

            // Adding the first node's edges will map out the rest of the road
            GraphNode nextNode = GetConnectedNode(startNode.connectedNode[0], startGraphNode);
            startGraphNode.Edges.Add(new GraphEdge(nextNode, nextNode.Edges[0].Distance, 1));
        }

        /// <summary> Gets the graph node for the given spline node, recursively searches through the road and creates neighboring nodes   </summary>
        private static GraphNode GetConnectedNode(SplineN node, GraphNode previousNode, double currentCost = 0)
        {
            // If the node is an end point we should add it to the road system graph
            if (node.isEndPoint)
            {
                // Increment the cost by the distance from the previous spline node to the end node
                currentCost += CalculateCostBetweenNodes(GetNextNode(node), node);
                
                // Create a new GraphNode for the end point
                GraphNode endNode = new GraphNode(node);

                // Add the previous node to the end node's edge list
                endNode.Edges.Add(new GraphEdge(previousNode, currentCost, 1));
                
                // Add the end node to the road system graph
                roadSystemGraph.Add(node.uID, endNode);
                
                return endNode;
            }
            
            // Increment the cost by the distance from the previous spline node to the next spline node
            currentCost += CalculateCostBetweenNodes(GetPreviousNode(node), node);
            
            // If the node is not an intersection, recursively continue searching in the next node
            if (!node.isIntersection)
            {
                return GetConnectedNode(GetNextNode(node), previousNode, currentCost);
            }

            // The node must be an intersection, so we check whether the intersecting node has already been added to the road system graph
            if (roadSystemGraph.ContainsKey(node.intersectionOtherNode.uID))
            {
                // Get the intersecting node
                GraphNode intersectionNode = roadSystemGraph[node.intersectionOtherNode.uID];
                
                // Add an edge to the intersecting node from the previous node with the current cost
                intersectionNode.Edges.Add(new GraphEdge(previousNode, currentCost, 1));
                
                // Add an edge to the next node
                intersectionNode.Edges.Add(new GraphEdge(GetConnectedNode(GetNextNode(node), intersectionNode), currentCost, 1));
                
                return intersectionNode;
            }
            
            // Create a node if the crossing road hasn't created it
            GraphNode newIntersectionNode = new GraphNode(node);
            
            // Add the node to the road system graph
            roadSystemGraph.Add(node.uID, newIntersectionNode);
        
            // Add an edge from the previous node to this one
            newIntersectionNode.Edges.Add(new GraphEdge(previousNode, currentCost, 1));
            
            // Add the next edge by recursively searching for the next node
            newIntersectionNode.Edges.Add(new GraphEdge(GetConnectedNode(GetNextNode(node), newIntersectionNode), currentCost, 1));
            
            return newIntersectionNode;
        }
        
        /// <summary> Calculate the cost from one spline to the other </summary>
        private static double CalculateCostBetweenNodes(SplineN node1, SplineN node2)
        {
            // Get the distance between the two nodes
            double distance = Vector3.Distance(node1.transform.position, node2.transform.position);

            // TODO: Get the speed limit of the road once it is implemented
            return distance;
        }
        
        /// <summary> Get the previous splineN node of a given splineN </summary>
        private static SplineN GetPreviousNode(SplineN node)
        {
            return node.connectedNode[1];
        }
        
        /// <summary> Get the next splineN node of a given splineN </summary>
        private static SplineN GetNextNode(SplineN node)
        {
            return node.connectedNode[0];
        }

        /// <summary> Draws the graph </summary>
        public static void DrawGraph(RoadSystem roadSystem, Dictionary<string, GraphNode> roadGraph)
        {
            foreach (GraphNode node in roadGraph.Values)
            {
                // Create a new graph node sphere
                GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                // Name it and place it on the correct location
                nodeObject.name = GRAPH_NODE_SPHERE_NAME;
                nodeObject.transform.parent = roadSystem.Graph.transform;
                nodeObject.transform.position = node.Node.transform.position;

                // Set the scale of the sphere
                nodeObject.transform.localScale = new Vector3(GRAPH_NODE_SPHERE_SIZE, GRAPH_NODE_SPHERE_SIZE, GRAPH_NODE_SPHERE_SIZE);
                
                nodeObject.AddComponent<LineRenderer>();
                
                // Create a line renderer for the edges
                LineRenderer lr = nodeObject.GetComponent<LineRenderer>();
                
                // Create a list to contain all the graph node positions
                List<Vector3> graphNodePositions = new List<Vector3>(){ node.Node.transform.position };
                
                // Add the end node of each edge to the list of positions
                foreach (GraphEdge edge in node.Edges)
                {
                    // To draw the graph, draw the line from the origin node to the target of the edge, and then back to the origin
                    // This is to make sure we only draw lines along the edges
                    graphNodePositions.Add(edge.EndNode.Node.transform.position);
                    graphNodePositions.Add(node.Node.transform.position);
                }
                lr.SetPositions(graphNodePositions.ToArray());
                lr.startWidth = EDGE_LINE_WIDTH;
                lr.endWidth = EDGE_LINE_WIDTH;
            }
        }
    }
}