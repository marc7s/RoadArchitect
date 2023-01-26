using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace RoadArchitect {
    public static class LaneGuideLineGeneration {
        // <summary>Gets a color from a range of colors based on the provided seed</summary>
        public static Color GetColor(int seed) {
            List<Color> colors = new List<Color>(){ Color.red, Color.blue, Color.green, Color.cyan, Color.magenta };
            return colors[seed % colors.Count];
        }

        // <summary>Updates the lane guide lines</summary>
        public static void updateLaneGuideLines(Road road, List<Vector3> RoadVectors) {
            RemoveGuideLines();

            List<Vector3> guide = new List<Vector3>();
            List<List<Vector3>> roadGuideLines = new List<List<Vector3>>();
            List<Vector3> UniqueRoadVectors = RoadVectors.Distinct().ToList();

            // Add an empty guide line for each lane
            for(int i = 0; i < road.laneAmount; i++) {
                roadGuideLines.Add(new List<Vector3>());
            }

            /*
                Each vertex in the UniqueRoadVectors list lies on the road edge, changing side for every index. 
                The vertex at index 0 is on the left edge, index 1 on the right, 2 on the left and so on.
                We use this fact to find the center of the road through a linear interpolation of 50% from the vertex on the left to the vertex on the right.
                Then, we offset the center line for each lane using another linear interpolation in order to get a line centered in every lane.
            */
            Vector3 left = UniqueRoadVectors[0];
            for(int i = 0; i < UniqueRoadVectors.Count; i++) {
                // Get the current vertex
                Vector3 v = UniqueRoadVectors[i];
                // Every even vertex will be on the left side, in that case simply update the left vertex
                if(i % 2 == 0) {
                    left = v;
                } else {
                    // This vertex is on the right side, and we know that the previous one was on the left side
                    // Find the center of the road at half the distance between the left and right vertices
                    Vector3 center = Vector3.Lerp(left, v, 0.5f);
                    
                    // Store the number of lanes in one direction
                    int oneWayLanes = road.laneAmount / 2;
                    
                    // For each lane on one side, generate a line centered in that lane and the opposite lane
                    for(int j = 0; j < oneWayLanes; j++) {
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
                        roadGuideLines[2 * j].Add(leftLaneCenter);
                        roadGuideLines[2 * j + 1].Add(rightLaneCenter);
                    }
                }
            }
            
            // Draw the guide lines in different colors
            for(int i = 0; i < roadGuideLines.Count; i++) {
                DrawGuideLine(roadGuideLines[i], GetColor(i));
            }
        }

        private static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            GameObject line = new GameObject();
            line.name = "Line";
            line.tag = "RoadLine";
            line.transform.position = start;
            line.AddComponent<LineRenderer>();
            LineRenderer lr = line.GetComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = 0.5f;
            lr.endWidth = 0.5f;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

        private static void DrawGuideLine(List<Vector3> guideLine, Color color) {
            if(guideLine.Count < 1) return;
            
            Vector3 lastG = guideLine[0];
            for(int i = 0; i < guideLine.Count; i++) {
                Vector3 g = guideLine[i];
                DrawLine(lastG, g, color);
                lastG = g;
            }
        }

        private static void RemoveGuideLines() {
            foreach(GameObject rl in GameObject.FindGameObjectsWithTag("RoadLine")) {
                Object.DestroyImmediate(rl);
            }
        }
    }
}