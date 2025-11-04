// It is assumed that the map only appears when there are more than one anchor

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ARLocation;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Location = ARLocation.Location;

public class MiniMapManager : MonoBehaviour {
    
    [SerializeField] private GameObject anchorPointOnMiniMapPrefab;
    [SerializeField] private Transform rotateRoot;
    [SerializeField] private Transform anchorsRoot;
    [SerializeField] private Transform rotateRootGPS;
    [SerializeField] private Transform anchorsRootGPS;
    [SerializeField] private RectTransform mapField;
    [SerializeField] private RectTransform rootTr;
    [SerializeField] private Toggle zoomToggle;
    [SerializeField] private float zoomInScale;
    [SerializeField] private GameObject redPointPrefab;
    private Transform[] redPointHighlights;
    [SerializeField] private Color completedColor;
    [SerializeField] private Color gpsColor;
    [SerializeField] private Color orangeColor;
    
    private List<PointOnMimiMap> points = new List<PointOnMimiMap>();
    private float fieldWidth;
    private float scale;
    private Transform mainCamera;
    private bool isActive;
    private Vector3 previousPos = Vector3.zero;
    private double currLatitude;
    private double currLongitude;
    private Vector3 initRotate;
    private bool isScaleByAR;
    
    private bool isRedPointEnable;
    

    private static MiniMapManager _instance;
    public static MiniMapManager Instance
    {
        get
        {
            return _instance;
        }
    }

    #region Unity Functions

    private void Awake() {
        _instance = this;
    }

    private void Start() {
        fieldWidth = mapField.rect.width;
        mainCamera = Camera.main.transform;

        zoomToggle.onValueChanged.AddListener(delegate { ZoomToggleValueChanged(zoomToggle); });
        Input.location.Start();
        Input.compass.enabled = true;

        redPointAnimation = RedPointAnimation();
    }

    private int otherCount = 0;
    private Vector3 startPosition;
    void Update() {
        // ToDo: disable this method temporarily by exiting now. Dmitry, will fix this.
        // return;

        if (isActive) {
            if (Vector3.Distance(previousPos, mainCamera.position) > 2) {
                SetGPSPoints();
                previousPos = mainCamera.position;
            }
            SetARPositionAnchorsOnMap();
            RotateMap();
        }
    }
    
    
    #endregion

    #region Public functions
    
    
    public void ShowMiniMap() {
        if(!isActive) {
            if (points.Count > 1 || SBContextManager.Instance.context.IsConsuming() ||
                SBContextManager.Instance.context.isEditing) {
                //print($"-=- MiniMap ShowMiniMap()");
                isActive = true;
                mapField.gameObject.SetActive(true);
            }
        }
    }

    public void HideMiniMap() {
        //print($"-=- MiniMap HideMiniMap()");
        isActive = false;
        mapField.gameObject.SetActive(false);
    }

    public void AddOtherAnchors() {
        //print($"-=- MiniMap AddOtherAnchors");
        IEnumerable<AnchorDto> anchors = SBContextManager.Instance.context.OtherAnchors;
        if(anchors.Count() > 0) {
            foreach (var anchor in anchors) {
                bool newAnchor = true;
                foreach (var point in points) {
                    if (point.id == anchor.id) {
                        newAnchor = false;
                    }
                }
                
                if(newAnchor) {
                    PointOnMimiMap newPoint = new PointOnMimiMap() {
                        isGPS = true,
                        latitude = anchor.latitude,
                        longitude = anchor.longitude,
                        color = gpsColor,
                        id = anchor.id
                    };

                    points.Add(newPoint);
                }
            }
            CreateGPSAnchors();
        }
    }
    
    public void AddContextAnchors(SBContext context) {
        //print($"-=- MiniMap AddContextAnchors anchors.Count() = {context.anchors.Count()}");
        IEnumerable<AnchorDto> anchors = context.anchors;
        if (anchors.Count() <= 0) return;
        
        foreach (var anchor in anchors) {
            bool newAnchor = true;
            foreach (var point in points.Where(point => point.id == anchor.id))
            {
                newAnchor = false;
            }

            if (!newAnchor) continue;
                
            // print($"-=- MiniMap AddContextAnchors, newAnchor id = {anchor.id}");
            var newPoint = new PointOnMimiMap() {
                isGPS = true,
                latitude = anchor.latitude,
                longitude = anchor.longitude,
                color = orangeColor,
                id = anchor.id
            };
            points.Add(newPoint);
        }
        isScaleByAR = false;
        CreateGPSAnchors();
    }

    public void AddThisAnchors(AnchorInfo anchorInfo) {
        if (anchorInfo != null) {
            bool newAnchor = true;
            foreach (var point in points) {
                if (point.id == anchorInfo.id) {
                    newAnchor = false;
                }
            }

            if (newAnchor) {
                //print($"-=- MiniMap AddThisAnchors");
                PointOnMimiMap newPoint = new PointOnMimiMap() {
                    isGPS = false,
                    arPosition = anchorInfo.pose.position,
                    color = orangeColor,
                    id = anchorInfo.id
                };
                points.Add(newPoint);
                
                CreateOrAddARAnchor(newPoint);
                ScaleMap();
            }
        }
    }

    public void AddExistConsumeAnchor(Vector3 position, string id) {
        foreach (var point in points) {
            if (point.id == id) {
                //print($"-=- MiniMap AddExistConsumeAnchor id = {id}, position = {position}");
                isScaleByAR = true;
                point.isGPS = false;
                point.arPosition = position;
                if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
                    point.color = gpsColor;
                }
                else {
                    point.color = orangeColor;
                }
                
                CreateOrAddARAnchor(point);
                ScaleMap();
            }
        }
    }

    public IEnumerator CorrectPositionDelay(AnchorController ac, string id) {
        yield return new WaitForSeconds(1.5f);
        AddExistConsumeAnchor(ac.transform.position, id);
    }

    public void SetRedPoint(string id) {
        foreach (var point in points) {
            if (point.id == id) {
                //print($"-=- MiniMap SetRedPoint id = {id}");
                point.color = Color.red;
                StartRedPointAnimation(point.pointObj.transform);
            }
        }
    }

    public void RemoveRedPoint() {
        StopRedPointAnimation();
    }
    
    public void SetGreenPoint(string id) {
        foreach (var point in points) {
            if (point.id == id) {
                //print($"-=- MiniMap SetGreenPoint id = {id}");
                point.color = completedColor;
                point.image.color = completedColor;
            }
        }
    }

    public void CleanMiniMap() {
        DeleteARPoints();
        DeleteGPSPoints();
        
        points.Clear();
        StopRedPointAnimation();
    }
    
    #endregion
    
    #region Creating & Editing
    
    
    void CreateGPSAnchors() {
        //print($"-=- MiniMap CreateAndUpdateMap");
#if PLATFORM_IOS
        currLatitude = Input.location.lastData.latitude;
        currLongitude = Input.location.lastData.longitude;
        if (currLatitude == 0) {
            SBContext context = SBContextManager.Instance.context;
            currLatitude = context.UserLocation.Latitude;
            currLongitude = context.UserLocation.Longitude;
        }
#endif
        
        DeleteGPSPoints();
        AddAllGpsAnchors();
        ScaleMap();
    }

    void SetGPSPoints() {
        //print($"-=- MiniMap SetGPSPoints");
        DeleteGPSPoints();
        AddAllGpsAnchors();
        ApplyScale();
    }
    
    void DeleteARPoints() {
        foreach (var point in points) {
            if (!point.isGPS) {
                Destroy(point.pointObj);
            }
        }
    }

    void DeleteGPSPoints() {
        foreach (var point in points) {
            if (point.isGPS) {
                Destroy(point.pointObj);
            }
        }
    }
    
    void CreateOrAddARAnchor(PointOnMimiMap point) {
        Vector3 position = new Vector3(point.arPosition.x, point.arPosition.z, 0);
        if (point.pointObj) {
            Destroy(point.pointObj);
        }

        GameObject anchor = Instantiate(anchorPointOnMiniMapPrefab, position, Quaternion.identity);
        anchor.transform.SetParent(anchorsRoot, false);
        point.pointObj = anchor;
        point.image = point.pointObj.GetComponent<Image>();
        point.isGPS = false;
        point.image.color = point.color;
    }

    void AddAllGpsAnchors() {
        int c = 0;
        foreach (var point in points) {
            if (point.isGPS) {
                NewAddGpsAnchor(point.latitude, point.longitude, point.id);
                c++;
            }
        }
        //print($"-=- MiniMapManager AddAllGpsAnchors() Count = {c}");
    }

    void ScaleMap() {
        // --- found max distance between anchors
        float maxDistance = 0;
        int count = points.Count;
        
        if (isScaleByAR) {
            for (int first = 0; first < count - 1; first++) {
                if (!points[first].isGPS) {
                    for (int second = first + 1; second < count; second++) {
                        if (!points[second].isGPS) {
                            float dis = Vector3.Distance(points[first].pointObj.transform.localPosition,
                                points[second].pointObj.transform.localPosition);
                            if (maxDistance < dis) {
                                maxDistance = dis;
                            }
                        }
                    }
                }
            }
            foreach (var point in points) {
                if (!point.isGPS) {
                    if (maxDistance < Vector3.Distance(point.pointObj.transform.localPosition, Vector3.zero)) {
                        maxDistance = Vector3.Distance(point.pointObj.transform.localPosition, Vector3.zero);
                    }
                }
            }
        }

        else {
            for (int first = 0; first < count - 1; first++) {
                for (int second = first + 1; second < count; second++) {
                    float dis = Vector3.Distance(points[first].pointObj.transform.localPosition,
                        points[second].pointObj.transform.localPosition);
                    if (maxDistance < dis) {
                        maxDistance = dis;
                    }
                }
            }
            foreach (var point in points) {
                if (maxDistance < Vector3.Distance(point.pointObj.transform.localPosition, Vector3.zero)) {
                    maxDistance = Vector3.Distance(point.pointObj.transform.localPosition, Vector3.zero);
                }
            }
        }

        // --- set scale of map
        scale = fieldWidth * 0.5f / maxDistance;
        //print($"-=- MiniMap ScaleMap scale = {scale}, maxDistance = {maxDistance}, isScaleByAR = {isScaleByAR}");

        ApplyScale();
    }

    void ApplyScale() {
        anchorsRoot.localScale = Vector3.one;
        anchorsRootGPS.localScale = Vector3.one;
        
        anchorsRoot.localScale *= scale;
        anchorsRootGPS.localScale *= scale;
        
        foreach (var point in points) {
            if (point.pointObj) {
                point.pointObj.transform.localScale = Vector3.one;
                point.pointObj.transform.localScale /= scale;
            }
        }
    }
    
    void NewAddGpsAnchor(double latitude, double longitude, string id = "") {
        
        Location l1 = new Location(latitude, longitude);
        Location l2 = new Location(currLatitude, currLongitude);

        var d = HorizontalDistance(l1, l2);
        var direction = (l2.HorizontalVector - l1.HorizontalVector).normalized;
        Vector3 pos = new Vector3(-(float) direction.y, -(float) direction.x, 0);
        pos *= (float) d;

        GameObject anchor = Instantiate(anchorPointOnMiniMapPrefab, pos, Quaternion.identity);
        anchor.transform.SetParent(anchorsRootGPS, false);
        anchor.transform.RotateAround(anchorsRootGPS.position, Vector3.forward, Input.compass.magneticHeading);
        anchor.GetComponent<Image>().color = gpsColor;
        
        initRotate = new Vector3(0, 0, mainCamera.eulerAngles.y);
        //print($"-=- MiniMap NewAddGpsAnchor Location l1 Latitude = {l1.Latitude}, Longitude = {l1.Longitude}");
        
        foreach (var point in points) {
            if (point.isGPS && point.id == id) {
                point.pointObj = anchor;
                point.image = anchor.GetComponent<Image>();
            }
        }
    }
    
    void RotateMap() {
        Vector3 rotateInDegree = new Vector3(0, 0, mainCamera.eulerAngles.y);
        rotateRoot.localEulerAngles = rotateInDegree;

        rotateInDegree -= initRotate;
        rotateRootGPS.localEulerAngles = rotateInDegree;
    }
    
    void SetARPositionAnchorsOnMap() {
        Vector3 pos = (Vector3.zero - new Vector3(mainCamera.position.x,mainCamera.position.z,0)) * scale;
        anchorsRootGPS.localPosition = pos;
        anchorsRoot.localPosition = pos;
    }
    

    #endregion
    
    #region Zooming

    public void ZoomToggleValueChanged(Toggle toggle) {
        if (!toggle.isOn) {
            ZoomIn();
        }
        else {
            ZoomOut();
        }
    }

    void ZoomIn() {
        Debug.Log($"ZoomIn()");
        StartCoroutine(Zoom(true));
    }

    void ZoomOut() {
        Debug.Log($"ZoomOut()");
        StartCoroutine(Zoom(false));
        
    }

    IEnumerator Zoom(bool zoomIn) {
        float speed = 0.25f;
        for (float f = 0; f < speed; f += Time.deltaTime) {
            float newScale;
            newScale = zoomIn ? Mathf.Lerp(1, zoomInScale, f / speed) : Mathf.Lerp(zoomInScale, 1, f / speed);
            mapField.sizeDelta = new Vector2(fieldWidth * newScale, fieldWidth * newScale);
            rootTr.localScale = new Vector3(newScale, newScale, newScale);
            yield return null;
        }
        
        if (zoomIn) {
            mapField.sizeDelta = new Vector2(fieldWidth * zoomInScale, fieldWidth * zoomInScale);
            rootTr.localScale = new Vector3(zoomInScale, zoomInScale, zoomInScale);
        }
        else {
            mapField.sizeDelta = new Vector2(fieldWidth, fieldWidth);
            rootTr.localScale = Vector3.one;
        }
        yield return null;
    }
    
    #endregion

    #region RedPointAnimation

    private IEnumerator redPointAnimation;
    private float highlightScale = 4.4f;
    private float highlightTime = 2.3f;
    Image[] images = new Image[2];
    
    private GameObject redPoint;
    
    void StartRedPointAnimation(Transform point) {
        //print($"-=- MiniMap StartRedPointAnimation");
        if (redPoint) {
            StopRedPointAnimation();
        }

        //create from prefab so that it doesn't get null, it will be destroyed together with its parent point.
        redPoint = Instantiate(this.redPointPrefab);
        redPoint.SetActive(true);
        redPoint.transform.SetParent(point);
        redPoint.transform.localPosition = Vector3.zero;
        redPoint.transform.localScale = Vector3.one;

        redPointHighlights = new Transform[2];
        redPointHighlights[0] = redPoint.transform.Find("Image");
        redPointHighlights[1] = redPoint.transform.Find("Image2");
        images[0] = redPointHighlights[0].GetComponent<Image>();
        images[1] = redPointHighlights[1].GetComponent<Image>();

        StartCoroutine(redPointAnimation);
    }
    
    void StopRedPointAnimation()
    {
        StopCoroutine(redPointAnimation);
        if (redPoint) {
            Destroy(redPoint);
        }
    }

    IEnumerator RedPointAnimation()
    {
        Color col = images[0].color;

        while (true) {
            for (float f = 0; f < 1; f += Time.deltaTime / highlightTime) {
                
                //Added by Cliff for avoiding NullReferenceException
                if (!images[0] || !images[1] || !redPointHighlights[0] || !redPointHighlights[1])
                    yield break;

                // circle 1
                float fScale = f * highlightScale;
                redPointHighlights[0].localScale = new Vector3(fScale, fScale, fScale);
                images[0].color = new Color(col.r, col.g, col.b, 1 - f);
                
                // circle 2
                fScale = fScale / highlightScale + 0.5f;
                if (fScale > 1) {
                    fScale -= 1.0f;
                }

                fScale *= highlightScale;
                redPointHighlights[1].localScale = new Vector3(fScale, fScale, fScale);
                images[1].color = new Color(col.r, col.g, col.b, 1 - fScale / highlightScale);

                yield return null;
            }
        }
    }

    #endregion
    
    #region Calculate GPS Data To Local Vector3

    double HorizontalDistance(ARLocation.Location l1, Location l2)
    {
        //var type = ARLocation.ARLocation.Config.DistanceFunction;
        //switch (type)
        //{
        //    case ARLocationConfig.ARLocationDistanceFunc.Haversine:
        //        return HaversineDistance(l1, l2);
        //    case ARLocationConfig.ARLocationDistanceFunc.PlaneSpherical:
        //        return PlaneSphericalDistance(l1, l2);
        //    case ARLocationConfig.ARLocationDistanceFunc.PlaneEllipsoidalFcc:
        //        return PlaneEllipsoidalFccDistance(l1, l2);
        //    default:
        //        return HaversineDistance(l1, l2);
        //}
        return ARLocation.Location.HorizontalDistance(l1, l2);
    }
    
    double HaversineDistance(Location l1, Location l2)
    {
        var r = ARLocation.ARLocation.Config.EarthMeanRadiusInKM;
        var rad = Math.PI / 180;
        var dLat = (l2.Latitude - l1.Latitude) * rad;
        var dLon = (l2.Longitude - l1.Longitude) * rad;
        var lat1 = l1.Latitude * rad;
        var lat2 = l2.Latitude * rad;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

        return r * 2 * Math.Asin(Math.Sqrt(a)) * 1000;
    }
    
    double PlaneSphericalDistance(Location l1, Location l2)
    {
        var r = ARLocation.ARLocation.Config.EarthMeanRadiusInKM;
        var rad = Math.PI / 180;
        var dLat = (l2.Latitude - l1.Latitude) * rad;
        var dLon = (l2.Longitude - l1.Longitude) * rad;
        var lat1 = l1.Latitude * rad;
        var lat2 = l2.Latitude * rad;
        var mLat = (lat1 + lat2) / 2.0;
        var mLatC = Math.Cos(mLat);

        var a = dLat * dLat;
        var b = mLatC * mLatC * dLon * dLon;

        return r * Math.Sqrt(a + b) * 1000.0;
    }
    
    double PlaneEllipsoidalFccDistance(Location l1, Location l2)
    {
        var rad = Math.PI / 180;
        var lat1 = l1.Latitude * rad;
        var lat2 = l2.Latitude * rad;
        var mLat = (lat1 + lat2) / 2.0;

        var k1 = 111.13209 - 0.56605 * Math.Cos(2 * mLat) + 0.00120 * Math.Cos(4 * mLat);
        var k2 = 111.41513 * Math.Cos(mLat) - 0.09455 * Math.Cos(3 * mLat) + 0.00012 * Math.Cos(5 * mLat);

        var a = k1 * (l2.Latitude - l1.Latitude);
        var b = k2 * (l2.Longitude - l1.Longitude);

        return 1000.0 * Math.Sqrt(a * a + b * b);
    }

    #endregion

    public class PointOnMimiMap {
        public bool isGPS = false;
        public string id;
        public Color color; // "Green", "Red", "Orange", "Gray"
        public Image image;
        public GameObject pointObj;
        
        public Vector3 arPosition;
        public double latitude;
        public double longitude;
    }
}