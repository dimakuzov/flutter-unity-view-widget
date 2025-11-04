using System;
using System.Collections.Generic;
using System.IO;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class NavigationManager : BaseSingletonClass<NavigationManager> {

    [SerializeField] GameObject breadcrumbPrefab;
    [SerializeField] GameObject arrowPrefab;
    [SerializeField] GameObject targetPrefab;

    // public GameObject tempUI;

    int startPointId;
    int finishPointId;

    string mapId = "123";
    Camera camera;
    NavigationPoints navigationPoints;
    int pointsInScene;
    bool isCreatingMode;
    NavigationPointData emptyNavigationPointData = new NavigationPointData();
    private Dictionary<string, int> mapActivityIds = new Dictionary<string, int>();
    private string endActivityId;

    private Vector3 previousPos = Vector3.zero;


    
    //***********************************************************************************************
    

    #region Unity Functions
    
    
    private void Start() {
        camera = Camera.main;
        emptyNavigationPointData.id = -1;
        emptyNavigationPointData.nextPointId = -1;
        emptyNavigationPointData.neighborIds = new List<int>();

        ARSession.stateChanged += OnTrackingLost;
    }

    private void Update() {
        // if (isCreatingMode && Vector3.Distance(previousPos, camera.transform.position) > 0.5f) {
        if (isCreatingMode) {
            /*Todo: temporarily disable it by Cliff for performance issue, to be controlled according to AR tracking situation.*/
            //CreateNewPoint(false);  
        }
    }


    #endregion


    //***********************************************************************************************
    //***********************************************************************************************


    #region Public Functions

    // ------ buttons -------
    public void ShowPathButton(string endActivityId) {
        PathCreating("activity0", endActivityId);
        // PathCreating(mapId, endActivityId);
    }

    public void FinishMapCreatingButton() { // --- Temp
        if(!SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
            FinishMapCreating("breadcrumbsId:" + DateTime.Now.ToString("u"));
        }
    }
    
    // ------ public functions -------
    // --- Use for getting map by mapId and started activityId
    // public void GetData(string mappId) {
    //     mapId = mappId;
    //     navigationPoints = new NavigationPoints();
    //     navigationPoints.navigationPoints = new List<NavigationPointData>();
        // tempUI.SetActive(true);

        
        // if (ES3.FileExists("IndoorMaps.es3")) {
        //     navigationPoints = ES3.Load<NavigationPoints>(mapId, "IndoorMaps.es3");
        //     Debug.Log("--- IndoorMaps.es3 exist!");
        // }
    // }

    public void ConfigureMapData(string newMapId, string mapNavPoints) {
        mapId = newMapId;
        if (!string.IsNullOrEmpty(mapNavPoints))
        {
            navigationPoints = JsonUtility.FromJson<NavigationPoints>(mapNavPoints);
            print($"-=- NavigationManager, ConfigureMapData mapId = {mapId}, navigationPoints.navigationPoints.Count = {navigationPoints.navigationPoints.Count}");    
        }
    }
    
    // --- Use for start new map or continue previous map
    private bool isCreatingBreadcrumbs;
    public void StartMapCreating() {
        if(!isCreatingBreadcrumbs) {
            endActivityId = mapId;
            StartCreating();
            isCreatingBreadcrumbs = true;
        }
    }

    // --- use for finish and save creating map (need to set new activity id)
    public void FinishMapCreating(string activityId) {
        isCreatingBreadcrumbs = false;
        endActivityId = activityId;
        FinishCreating();
    }
    
    // --- use for create path from current point to needed point
    public void PathCreating(string startActivityId, string finishActivityId) {
        foreach (var point in navigationPoints.navigationPoints) {
            if (!String.IsNullOrEmpty(point.activityId)) {
                Debug.Log("mapActivityIds.Add: " + point.activityId);
                mapActivityIds.Add(point.activityId, point.id);
            }
        }
        startPointId = mapActivityIds[startActivityId];
        finishPointId = mapActivityIds[finishActivityId];
        CreatePath();
    }

    public void DestroyMap() {
        foreach (var point in pointsOnScene) {
            Destroy(point.gameObject);
        }
        mapActivityIds = new Dictionary<string, int>();
    }
    

    #endregion
    
    
    //***********************************************************************************************
    //***********************************************************************************************


    #region Creating Map
    

    NavigationPoints editAllPoint;
    private NavigationPointData lastPoint;
    private GameObject lastPointObject;
    private List<NavigationPoint> pointsOnScene;
    private bool lastPointWasCreated;
    
    void StartCreating() {
        // --- This we call when we get navigationPoints;
        if (navigationPoints == null || navigationPoints.navigationPoints == null ||
            navigationPoints.navigationPoints.Count == 0) {
            StartCreating(false);
        }
        else {
            foreach (var point in navigationPoints.navigationPoints) {
                if (!String.IsNullOrEmpty(point.activityId)) {
                    mapActivityIds.Add(point.activityId, point.id);
                }
            }
            StartCreating(true);
        }
    }
    
    void StartCreating(bool isMap) {
        if (pointsOnScene != null && pointsOnScene.Count > 0) {
            isCreatingMode = true;
            return;
        }
        pointsOnScene = new List<NavigationPoint>();
        editAllPoint = new NavigationPoints();
        editAllPoint.navigationPoints = new List<NavigationPointData>();
        if (isMap) {
            CreateAllPointMap();
            
            // *** Find start point
            if (mapActivityIds.ContainsKey(endActivityId))
            {
                int lastPointId = mapActivityIds[endActivityId];
                lastPoint = navigationPoints.navigationPoints[lastPointId];    
            }
        }
        else {
            CreateNewPoint(true);
        }
        isCreatingMode = true;
    }
    
    // --- To show previous paths
    void CreateAllPointMap() {
        editAllPoint = navigationPoints;
        
        foreach (var point in navigationPoints.navigationPoints) {
            GameObject pointObject;
            if (point.type == "target") {
                pointObject = Instantiate(targetPrefab, point.position, Quaternion.identity);
            }
            else {
                pointObject = Instantiate(breadcrumbPrefab, point.position, Quaternion.identity);
            }
            pointObject.GetComponent<NavigationPoint>().id = point.id;
            pointObject.GetComponent<NavigationPoint>().position = point.position;
            pointObject.GetComponent<NavigationPoint>().type = point.type;
            pointObject.GetComponent<NavigationPoint>().neighborIds = point.neighborIds;
            pointsOnScene.Add(pointObject.GetComponent<NavigationPoint>());
            pointsInScene++;
        }
        Debug.Log("**** Points on scene was created");
    }
    

    void CreateNewPoint(bool isEnd) {
        // --- Check on existing neighbor
        if (!isEnd) {
            int layerMask = 1 << 8;
            Collider[] hitColliders = Physics.OverlapSphere(camera.transform.position, 0.9f, layerMask);
            int i = 0;
            while (i < hitColliders.Length) {
                //*** Checking neighbors
                // if (hitColliders[i].CompareTag("NaviPoint")) {
                    NavigationPoint hitPoint = hitColliders[i].GetComponent<NavigationPoint>();
                    
                    if (lastPoint != null && lastPoint.id != hitPoint.id) {
                        //*** Checking if this neighborIds previously existed at lastPoint
                        foreach (var neighborId in lastPoint.neighborIds) {
                            if (neighborId == hitPoint.id) {
                                i++;
                                lastPoint = editAllPoint.navigationPoints[hitPoint.id];
                                return;
                            }
                        }
                        //*** If it is new point we add neighbors to neighborIds list
                        Debug.Log("--- add neighbors to neighborIds list, last id: " + lastPoint.id
                                                                                     + " hit id: " + hitPoint.id);
                        if (lastPointWasCreated) {
                            editAllPoint.navigationPoints[lastPoint.id].neighborIds.Add(hitPoint.id);
                            editAllPoint.navigationPoints[hitPoint.id].neighborIds.Add(lastPoint.id);
                        }
                        lastPointWasCreated = false;
                        lastPoint = editAllPoint.navigationPoints[hitPoint.id];
                    }
                    return;
                // }
                // i++;
            }
            print($"-=- Nav. CreateNewPoint hitColliders.Length = {hitColliders.Length}");
            print($"-=- Nav. CreateNewPoint camera.transform.position = {camera.transform.position}");
        }

        //*** Create new point
        SetNewPoint(isEnd);
    }

    void SetNewPoint(bool isEnd) {

        int pointInScene = FindObjectsOfType<NavigationPoint>().Length;
        print($"-=- Points in scene = {pointInScene}");
        //*** Setting new point
        NavigationPointData point = new NavigationPointData();
        point.neighborIds = new List<int>();
        point.position = camera.transform.position - new Vector3(0, .5f, 0);
        point.id = pointsInScene;
        pointsInScene++;

        if (lastPoint != null) {
            //*** If it isn't start we add neighbors to neighborIds list
            editAllPoint.navigationPoints[lastPoint.id].neighborIds.Add(point.id);
            point.neighborIds.Add(lastPoint.id);
        }

        //*** Creating and setting new point prefab
        if (isEnd) {
            point.type = "target";
            lastPointObject = Instantiate(targetPrefab, point.position, Quaternion.identity);
            point.activityId = endActivityId;
            Debug.Log("-=- Nav. End Point Created, position: " + point.position);
        }
        else {
            point.type = "path";
            lastPointObject = Instantiate(breadcrumbPrefab, point.position, Quaternion.identity);
            Debug.Log("-=- Nav. Path Point Created, position: " + point.position);
        }

        lastPointObject.GetComponent<NavigationPoint>().id = point.id;
        lastPointObject.GetComponent<NavigationPoint>().position = point.position;
        lastPointObject.GetComponent<NavigationPoint>().type = point.type;
        lastPointObject.GetComponent<NavigationPoint>().neighborIds = point.neighborIds;

        //*** Add point data to new point map
        editAllPoint.navigationPoints.Add(point);
        pointsOnScene.Add(lastPointObject.GetComponent<NavigationPoint>());
        lastPoint = point;
        lastPointWasCreated = true;
    }
    
    void FinishCreating() {
        isCreatingMode = false;
        if (Vector3.Distance(lastPoint.position, (camera.transform.position - new Vector3(0, .5f, 0)))
            < .5f && lastPoint.neighborIds.Count == 1) {
            // *** Remove last point from lists
            if (lastPoint.id == editAllPoint.navigationPoints.Count - 1) {
                Destroy(lastPointObject);
                pointsInScene--;
                editAllPoint.navigationPoints.RemoveAt(editAllPoint.navigationPoints.Count - 1);
                pointsOnScene.RemoveAt(editAllPoint.navigationPoints.Count);
                lastPoint = editAllPoint.navigationPoints[editAllPoint.navigationPoints.Count - 1];
                lastPoint.neighborIds.RemoveAt(1);
            }
        }

        CreateNewPoint(true);
        string map = SavePointMapAsJSON();
    }
    
    // --- Save new map
    public string SavePointMapAsJSON() {
        
        // ES3.Save(mapId, editAllPoint, "IndoorMaps.es3");

        return JsonUtility.ToJson(editAllPoint);
    }


    #endregion


    //***********************************************************************************************
    //***********************************************************************************************


    #region Path Creating

    private NavigationPointData startPoint;
    private NavigationPointData finishPoint;

    void CreatePath() {
        
        if(navigationPoints.navigationPoints.Count != 0) {
            FindUnnecessaryTargets(navigationPoints, startPointId, finishPointId);
        }
        else {
            Debug.Log("**** Map Is Empty");
        }
    }

    //==============================================================================================

    void FindUnnecessaryTargets(NavigationPoints allPoints, int startPointId, int endPointId) {

        //*** Creating Unnecessary Targets List
        Debug.Log("**** FindUnnecessaryTargets");
        List<NavigationPointData> unnecessaryTargets = new List<NavigationPointData>();
        foreach (var point in allPoints.navigationPoints) {
            if (point.id != startPointId && point.id != endPointId && point.type == "target") {
                if (point.neighborIds.Count >= 2) {
                    point.type = "path"; //*** Change targets with more then one neighbors
                }
                else {
                    unnecessaryTargets.Add(point);
                }
            }
        }

        startPoint = allPoints.navigationPoints[startPointId];
        finishPoint = allPoints.navigationPoints[endPointId];

        RemovePathFronUnnecessaryTargets(allPoints, unnecessaryTargets);
    }

    //==============================================================================================
    
    void RemovePathFronUnnecessaryTargets(NavigationPoints allPoints, List<NavigationPointData> unnecessaryTargets) {

        //*********** Made From Unnecessary Targets a paths
        Debug.Log("**** RemovePathFronUnnecessaryTargets");
        foreach (var targetPoint in unnecessaryTargets) {

            //*** Find points from Unnecessary Targets
            bool isFinish = false;
            NavigationPointData currentPoint = targetPoint;
            NavigationPointData lastPoint = new NavigationPointData();
            lastPoint.id = -1;

            while (!isFinish) {
                //*** Finish When Neighbor Count More Then Two
                if (currentPoint.neighborIds.Count <= 2) {

                    int neighborId = -1;
                    for (int i = 0; i < currentPoint.neighborIds.Count; i++) {
                        //*** Find New Neighbor ID
                        if (currentPoint.neighborIds[i] != lastPoint.id) {
                            neighborId = currentPoint.neighborIds[i];
                        }
                    }

                    lastPoint = currentPoint;
                    currentPoint = allPoints.navigationPoints[neighborId];
                    allPoints.navigationPoints[lastPoint.id] = emptyNavigationPointData;
                }
                else {
                    isFinish = true;
                }
            }
        }

        RecountNeighborsFromUpdatingMap(allPoints);
    }

    //==============================================================================================
    
    void RecountNeighborsFromUpdatingMap(NavigationPoints allPoints) {

        Debug.Log("**** RecountNeighborsFromUpdatingMap");
        bool wasEnd = false;
        foreach (var point in allPoints.navigationPoints) {
            if (point.id != -1) {
                //*** Checking Neighbors
                if (point.neighborIds.Count > 2) {

                    bool done = false;
                    while (!done) {

                        List<int> indexes = new List<int>();
                        foreach (var neighborId in point.neighborIds) {
                            if (allPoints.navigationPoints[neighborId].id == -1) {
                                //*** Collecting unnecessary index in indexes list
                                int index = point.neighborIds.IndexOf(neighborId);
                                indexes.Add(index);
                            }
                        }

                        if (indexes.Count >= 1) {
                            point.neighborIds.RemoveAt(indexes[0]);
                        }
                        //*** Remove neighbor from point neighbor's list
                        else if (indexes.Count == 1) {
                            foreach (var index in indexes) {
                                point.neighborIds.RemoveAt(index);
                                done = true;
                            }
                        }
                        else {
                            done = true;
                        }
                    }
                }
            }
            //*** Checking Ends in map
            if (point.neighborIds.Count == 1 && point.id != startPoint.id && point.id != finishPoint.id) {
                point.type = "target";
                wasEnd = true;
            }
        }
        
        if (wasEnd) {
            FindUnnecessaryTargets(allPoints, startPoint.id, finishPoint.id);
        }
        else {
            FindPathFromStartToFinish(allPoints);
        }
    }
    
    //==============================================================================================

    void FindPathFromStartToFinish(NavigationPoints allPoints) {

        Debug.Log("**** FindPathFromStartToFinish");
        NavPath path = new NavPath();
        path.points = new List<NavigationPointData>();

        NavigationPointData currentPoint = emptyNavigationPointData;
        NavigationPointData nextPoint = emptyNavigationPointData;

        bool pathsIsDone = false;
        currentPoint = startPoint;
        lastPoint = emptyNavigationPointData;


        int tryings = 0;
        //***************** Start of Searching ****************
        while (!pathsIsDone) {

            //*********** Checking On Finish path !
            if (currentPoint.id == finishPoint.id) {
                
                path.points.Add(currentPoint);
                pathsIsDone = true;
                ShowingPart(path.points, allPoints);
                break;
            }

            //*********** When Neighbor Count LESS Then TWO
            //*********** Checking Neighbor Count
            if (currentPoint.neighborIds.Count <= 2) {
                for (int i = 0; i < currentPoint.neighborIds.Count; i++) {
                    //*** Find New Neighbor ID
                    if (currentPoint.neighborIds[i] != lastPoint.id) {

                        //*** Setting current point & check next point
                        int nextId = currentPoint.neighborIds[i];
                        currentPoint.nextPointId = nextId;
                        currentPoint.lastPointPos = lastPoint.position;
                        lastPoint = currentPoint;
                        path.points.Add(currentPoint);
                        currentPoint = allPoints.navigationPoints[nextId];
                        break;
                    }
                }
            }

            //*** When Neighbor Count MORE Then TWO
            else {

                int nextPointId = -1;
                float dis = 11111;
                for (int i = 0; i < currentPoint.neighborIds.Count; i++) {

                    //*** Find New Neighbor ID
                    if (currentPoint.neighborIds[i] != lastPoint.id) {

                        //*** Check nearest point
                        int nextId = currentPoint.neighborIds[i];
                        float distance = Vector3.Distance(allPoints.navigationPoints[nextId].position,
                            finishPoint.position);
                        
                        if (distance < dis) {
                            dis = distance;
                            nextPointId = nextId;
                        }
                    }
                }
                
                currentPoint.nextPointId = nextPointId;
                currentPoint.lastPointPos = lastPoint.position;
                lastPoint = currentPoint;
                path.points.Add(currentPoint);
                currentPoint = allPoints.navigationPoints[nextPointId];
            }
        }
    }

    //==============================================================================================
    
    
    
    void ShowingPart(List<NavigationPointData> path, NavigationPoints allPoints) {
        pointsOnScene = new List<NavigationPoint>();
        foreach (var point in path) {
            if(point.id != -1) {

                GameObject pointObject;
                if (point.type == "target") {
                    pointObject = Instantiate(targetPrefab, point.position, Quaternion.identity);
                }
                else {
                    pointObject = Instantiate(arrowPrefab, point.position, Quaternion.identity);
                }
                NavigationPoint navPoint = pointObject.GetComponent<NavigationPoint>();
                navPoint.id = point.id;
                navPoint.position = point.position;
                navPoint.type = point.type;
                navPoint.neighborIds = point.neighborIds;
                navPoint.nextPointId = point.nextPointId;
                navPoint.lastPointPos = point.lastPointPos;
                pointsOnScene.Add(navPoint);
                
                pointObject.transform.LookAt(allPoints.navigationPoints[navPoint.nextPointId].position, Vector3.up);
                if(navPoint.type == "path") {
                    navPoint.SetLastPointDirection();
                }
                pointsInScene++;
            }
        }
        
    }
    
    #endregion
    

    //***********************************************************************************************
    
    
    private void OnTrackingLost(ARSessionStateChangedEventArgs args)
    {
        if(args.state == ARSessionState.None || args.state == ARSessionState.Unsupported) {
            print("AR Tracking is lost!");
            
            //handle tracking lost here:
            //...
        }
    }
    
    
}

[Serializable] public class NavPath {
    public List<NavigationPointData> points;
}

[Serializable] public class NavigationPoints {
    public List<NavigationPointData> navigationPoints;
}

[Serializable] public class NavigationPointData {
    public int id;
    public Vector3 position;
    public string type; // "target", "path"
    public List<int> neighborIds;
    
    public int nextPointId;// if point not set: -1
    public Vector3 lastPointPos;
    
    // --- This is id of activity in activity collection
    public string activityId;
}