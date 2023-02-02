using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace RoadArchitect {
    public static class LaneGeneration 
    {
        public static string LANE_CONTAINER_NAME = "Lanes";
        private static string LANE_NAME = "Lane";
        /// <summary>Gets a color from a range of colors based on the provided seed</summary>
        public static Color GetColor(int seed) 
        {
            List<Color> colors = new List<Color>(){ Color.red, Color.blue, Color.green, Color.cyan, Color.magenta };
            return colors[seed % colors.Count];
        }

        // Get a unique list of the road vectors
        public static List<Vector3> getUniqueRoadVectors(List<Vector3> RoadVectors){
            return RoadVectors.Distinct().ToList();
        }

        /// <summary>Updates the lanes</summary>
        public static void updateLanes(Road road, List<Vector3> uniqueRoadVectors) 
        {
            List<List<Vector3>> roadLanes = new List<List<Vector3>>();

            // Add an empty list for each lane
            for(int i = 0; i < road.laneAmount; i++) 
            {
                roadLanes.Add(new List<Vector3>());
            }

            /*
                Each vertex in the UniqueRoadVectors list lies on the road edge, changing side for every index. 
                The vertex at index 0 is on the left edge, index 1 on the right, 2 on the left and so on.
                We use this fact to find the center of the road through a linear interpolation of 50% from the vertex on the left to the vertex on the right.
                Then, we offset the center line for each lane using another linear interpolation in order to get a line centered in every lane.
            */
            Vector3 left = uniqueRoadVectors[0];
            for(int i = 0; i < uniqueRoadVectors.Count; i++) 
            {
                // Get the current vertex
                Vector3 v = uniqueRoadVectors[i];
                // Every even vertex will be on the left side, in that case simply update the left vertex
                if(i % 2 == 0) 
                {
                    left = v;
                } 
                else
                {
                    // This vertex is on the right side, and we know that the previous one was on the left side
                    // Find the center of the road at half the distance between the left and right vertices
                    Vector3 center = Vector3.Lerp(left, v, 0.5f);
                    
                    // Store the number of lanes in one direction
                    int oneWayLanes = road.laneAmount / 2;
                    
                    // For each lane on one side, generate a line centered in that lane and the opposite lane
                    for(int j = 0; j < oneWayLanes; j++) 
                    {
                        // Calculate the linear interpolation coefficient for the center of the lane
                        // Since we interpolate from left to center or center to right respectively, the coefficient should be
                        // some form of 1 / oneWayLanes
                        // Then, in order to center it in the middle of the lane and not the edge, we need an offset of 0.5
                        // To get the different lanes, we need a factor of j
                        float lerpCoef = (j + 0.5f) / (float)oneWayLanes;

                        // Calculate the left and right lane centers
                        Vector3 leftLaneCenter = Vector3.Lerp(left, center, lerpCoef);
                        Vector3 rightLaneCenter = Vector3.Lerp(center, v, lerpCoef);
                        
                        // Add the points to the guide lines
                        roadLanes[2 * j].Add(leftLaneCenter);
                        roadLanes[2 * j + 1].Add(rightLaneCenter);
                    }
                }
            }

            // Get all the end points of the road
            List<SplineN> endNodes = road.spline.nodes.FindAll(n => n.isEndPoint);
            
            // If there are two end points, and they are located at the same position that means that the road is looped
            if(endNodes.Count == 2 && endNodes[0].pos == endNodes[1].pos) {
                // Since the road is looped, we add a copy of the first position of each lane to the end of the lane in order to close the loop
                foreach(List<Vector3> lane in roadLanes) {
                    lane.Add(lane[0]);
                }
            }
            
            // Draw the lanes in different colors
            for(int i = 0; i < roadLanes.Count; i++) 
            {
                // The left lanes are on even indexes
                bool isLeft = i % 2 == 0;
                
                // Separate the left and right lane indexes
                int index = isLeft ? i / 2 : (i - 1) / 2;

                // Reverse the lane if it is in the opposite direction
                if ((isLeft && road.roadSystem.drivingSide == DrivingSide.Right) || (!isLeft && road.roadSystem.drivingSide == DrivingSide.Left))
                {
                    roadLanes[i].Reverse();
                }
                // Draw the lane
                DrawLane(road, roadLanes[i], index, GetColor(i), isLeft);
            }
        }
        /// <summary>Helper function that performs the drawing of a lane's path</summary>
        private static void DrawLanePath(GameObject line, List<Vector3> lane, Color color)
        {
            // Get the line renderer
            LineRenderer lr = line.GetComponent<LineRenderer>();

            // Give it a material
            lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

            // Give it a color
            lr.startColor = color;
            lr.endColor = color;
            
            // Give it a width
            lr.startWidth = 0.5f;
            lr.endWidth = 0.5f;
            
            // Set the positions
            lr.positionCount = lane.Count;
            lr.SetPositions(lane.ToArray());
        }

        /// <summary>Draws a lane</summary>
        private static void DrawLane(Road road, List<Vector3> lane, int index, Color color, bool isLeft) 
        {
            if(lane.Count < 1) return;
            
            // Create the lane object
            GameObject laneObject = new GameObject();
            
            // Set the lane name
            laneObject.name = (isLeft ? "Left" : "Right") + LANE_NAME + index;
            
            // Set the lane as a child of the Lanes container object
            laneObject.transform.parent = road.Lanes.transform;

            // Add a line renderer to the lane
            laneObject.AddComponent<LineRenderer>();
            
            // Draw the lane path
            DrawLanePath(laneObject, lane, color);
        }

        /// <summary>Draws a line, used for debugging</summary>
        public static void DrawDebugLine(List<Vector3> line, Color color) 
        {
            if(line.Count < 1) return;
            
            // Create the line object
            GameObject lineObject = new GameObject();

            // Add a line renderer to the lane
            lineObject.AddComponent<LineRenderer>();
            
            // Draw the lane path
            DrawLanePath(lineObject, line, color);
        }
    }
}