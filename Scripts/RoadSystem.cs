#region "Imports"
using UnityEngine;
using System.Collections.Generic;
#endregion


namespace RoadArchitect
{
    public enum DrivingSide
    {
        Left,
        Right
    }
    public class RoadSystem : MonoBehaviour
    {
        #region "Vars"
        [UnityEngine.Serialization.FormerlySerializedAs("opt_bMultithreading")]
        public bool isMultithreaded = true;
        [UnityEngine.Serialization.FormerlySerializedAs("opt_bSaveMeshes")]
        public bool isSavingMeshes = false;
        [UnityEngine.Serialization.FormerlySerializedAs("opt_bAllowRoadUpdates")]
        public bool isAllowingRoadUpdates = true;

        public Camera editorPlayCamera = null;
        public GameObject Graph;
        public Dictionary<string, GraphNode> RoadGraph { get; private set; }
        [UnityEngine.Serialization.FormerlySerializedAs("ShowGraph")]
        public bool ShowGraph = false;
        #endregion


        public DrivingSide drivingSide = DrivingSide.Right;
        void Start()
        {
            ClearRoadGraph();
        }
        /// <summary> Adds a new road to this RoadSystem </summary>
        public GameObject AddRoad(bool _isForceSelected = false)
        {
            Road[] roads = GetComponentsInChildren<Road>();
            int newRoadNumber = (roads.Length + 1);

            //Road:
            GameObject roadObj = new GameObject("Road" + newRoadNumber.ToString());

            #if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(roadObj, "Created road");
            #endif

            roadObj.transform.parent = transform;
            Road road = roadObj.AddComponent<Road>();

            // Splines
            GameObject splineObj = new GameObject("Splines");
            splineObj.transform.parent = road.transform;
            road.spline = splineObj.AddComponent<SplineC>();
            road.spline.splineRoot = splineObj;
            road.spline.road = road;
            road.splineObject = splineObj;
            road.roadSystem = this;
            RoadArchitect.RootUtils.SetupUniqueIdentifier(ref road.UID);

            road.ResetTerrainHistory();

            #if UNITY_EDITOR
            if (_isForceSelected)
            {
                UnityEditor.Selection.activeGameObject = roadObj;
            }
            #endif

            return roadObj;
        }


        /// <summary> Sets the editorPlayCamera to the first camera, if it is the only camera in this scene </summary>
        public void EditorCameraSetSingle()
        {
            if (editorPlayCamera == null)
            {
                Camera[] editorCams = GameObject.FindObjectsOfType<Camera>();
                if (editorCams != null && editorCams.Length == 1)
                {
                    editorPlayCamera = editorCams[0];
                }
            }
        }


        /// <summary> Updates all roads of this RoadSystem </summary>
        public void UpdateAllRoads()
        {
            Road[] allRoadObjs = GetComponentsInChildren<Road>();
            int roadCount = allRoadObjs.Length;
            SplineC[] piggys = null;
            if (roadCount > 1)
            {
                piggys = new SplineC[roadCount];
                for (int i = 0; i < roadCount; i++)
                {
                    piggys[i] = allRoadObjs[i].spline;
                }
            }

            Road road = allRoadObjs[0];
            if (piggys != null && piggys.Length > 0)
            {
                road.PiggyBacks = piggys;
            }
            road.UpdateRoad();
            // update the road graph
            UpdateRoadSystemGraph();
        }


        //Workaround for submission rules:
        /// <summary> Writes isMultithreaded into roads of this system </summary>
        public void UpdateAllRoadsMultiThreadedOption(bool _isMultithreaded)
        {
            Road[] roads = GetComponentsInChildren<Road>();
            int roadsCount = roads.Length;
            Road road = null;
            for (int i = 0; i < roadsCount; i++)
            {
                road = roads[i];
                if (road != null)
                {
                    road.isUsingMultithreading = _isMultithreaded;
                }
            }
        }


        //Workaround for submission rules:
        /// <summary> Writes isSavingMeshes into roads of this system </summary>
        public void UpdateAllRoadsSavingMeshesOption(bool _isSavingMeshes)
        {
            Road[] roads = GetComponentsInChildren<Road>();
            int roadsCount = roads.Length;
            Road road = null;
            for (int i = 0; i < roadsCount; i++)
            {
                road = roads[i];
                if (road != null)
                {
                    road.isSavingMeshes = _isSavingMeshes;
                }
            }
        }
        public void UpdateRoadSystemGraph()
        {
            // Clear the graph
            ClearRoadGraph();
            
            // Create a new empty graph
            CreateEmptyRoadGraph();
            
            // Generate a new graph
            RoadGraph = RoadSystemGraph.GenerateRoadGraph(this);

            // Display the graph if the setting is active
            if (ShowGraph) {
                RoadSystemGraph.DrawGraph(this, RoadGraph);
            }
                
        }

        private void ClearRoadGraph() {
            RoadGraph = null;
            if(Graph != null) {
                Object.DestroyImmediate(Graph);
            }
            
            Graph = null;
        }

        private void CreateEmptyRoadGraph() {
            Graph = new GameObject("Graph");
            Graph.transform.parent = transform;
        }
    }
}
