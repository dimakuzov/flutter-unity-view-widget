using System;
using System.Collections;
using System.Collections.Generic;
using ARLocation;
using Mapbox.CheapRulerCs;
using Mapbox.Examples;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Niantic.Lightship.AR.VpsCoverage;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MapBoxManager : BaseSingletonClass<MapBoxManager>
{
    [SerializeField]
    public AbstractMap _map;
    public GameObject mapEssentials;
    public Camera mapCamera;
    public GameObject navigationMapItem;
    public GameObject navigationMapScreen;
    public float arMapHeightOffset;
    public GameObject navigationMapCamera;
    public GameObject wayspotMarkerPrefab;
    public GameObject playerMarkerPrefab;
    public float markerSpawnScale = 10;
    private GameObject currentSelectedMarker = null;
    [SerializeField]
    [Geocode]
    List<string> _locationStrings;
    Vector2d[] _locations;
    [Geocode]
    public string _playerLocationString;
    Vector2d _locationPlayer;
    public List<GameObject> _spawnedObjects;
    GameObject spawnedPlayer;

    //private LocalizationTarget selectedMarkerData;
    private bool isNavigatingToActivity = false;
    
    // Threshold Distance in Metres.
    // public float distanceThreshold = 50f;

    //Ui Related
    public GameObject loadingPanel;
    public bool isMapLoading = false;
    // private bool hasUpdatedUIForPreLocalization = false;
    private bool hasUpdatedUIForPreLocalizationAfterNavigation = false;

    // For debugging only:
    private DateTime debugProbingStartTime;
    // END: debugging.
    
    /// <summary>
    /// Camera Settings for Full Screen Map and AR Map at the Bottom
    /// </summary>
    public void ApplyCameraSetting(float cameraFov, float cameraRectWidth,float cameraRectHeight,bool interactionStatus)
    {

        mapCamera.fieldOfView = cameraFov;
        mapCamera.rect = new Rect(0,0, cameraRectWidth, cameraRectHeight);
        _map.gameObject.GetComponent<QuadTreeCameraMovement>().enabled = interactionStatus;
        MarkerInteraction(interactionStatus);
    }

    public void SetMapView(MapView currentMapView, bool willNavigate = false)
    {
        PointsBarManager.Instance.HidePointsBar();
        if (currentMapView == MapView.FullViewMap)
        {
            spawnedPlayer.GetComponent<RotateWithLocationProvider>().enabled = false;
            spawnedPlayer.transform.GetChild(0)?.gameObject.SetActive(true);
            spawnedPlayer.transform.GetChild(1)?.gameObject.SetActive(false);
            navigationMapItem.SetActive(false);
            navigationMapCamera.SetActive(false);
            mapCamera.gameObject.SetActive(true);
        }
        else
        {
            spawnedPlayer.transform.GetChild(0)?.gameObject.SetActive(false);
            spawnedPlayer.transform.GetChild(1)?.gameObject.SetActive(true);
            navigationMapCamera.transform.position = mapCamera.transform.position;
            navigationMapCamera.gameObject.SetActive(true);
            navigationMapItem.SetActive(true);
            spawnedPlayer.GetComponent<RotateWithLocationProvider>().enabled = true;
            mapCamera.gameObject.SetActive(false);
        }

        isNavigatingToActivity = willNavigate;
        print($"isNavigatingToActivity={isNavigatingToActivity}");
        if (isNavigatingToActivity && SystemInfo.deviceUniqueIdentifier == SBContextManager.VonsDeviceID)
        {
            debugProbingStartTime = DateTime.Now;
        }
        // hasUpdatedUIForPreLocalization = false;
        hasUpdatedUIForPreLocalizationAfterNavigation = false;
    }

    /// <summary>
    /// Generate Map Based on Wayspots
    /// </summary>
    /// <param name="Wayspots"></param>
    public void GenerateMap(List<LocalizationTarget> Wayspots)
    {

        // Destroy previous markers if any...
        DestroyAllMarkers();



        _map.Options.locationOptions.latitudeLongitude = VpsCoverageManager.Instance.LastKnownUserLocation != null
            ? $"{VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Latitude},{VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Longitude}"
            : _playerLocationString;

        Debug.Log("Current Location of Map is " + _map.Options.locationOptions.latitudeLongitude);
        _map.gameObject.GetComponent<QuadTreeCameraMovement>().enabled = false;
        ApplyCameraSetting(60, 1, 1,true);
        mapEssentials.SetActive(true);

        _spawnedObjects = new List<GameObject>();
        _locationStrings = new List<string>();

        
        for (int i = 0; i < Wayspots.Count; i++)
        {
            var spawnedWayspot = Instantiate(wayspotMarkerPrefab);
            spawnedWayspot.transform.localScale = new Vector3(markerSpawnScale, markerSpawnScale, markerSpawnScale);
            spawnedWayspot.gameObject.GetComponent<WayspotMarker>().wayspotData = Wayspots[i];
            spawnedWayspot.name = spawnedWayspot.gameObject.GetComponent<WayspotMarker>().wayspotData.Identifier;
            StartCoroutine(LoadImageFromURL(spawnedWayspot.gameObject.GetComponent<WayspotMarker>().wayspotData.ImageURL,
               spawnedWayspot.gameObject.GetComponent<WayspotMarker>().markerThumbnail));
            var currentLatLong = spawnedWayspot.gameObject.GetComponent<WayspotMarker>().wayspotData.Center.Latitude.ToString() + ", " +
                spawnedWayspot.gameObject.GetComponent<WayspotMarker>().wayspotData.Center.Longitude
                .ToString();
            _locationStrings.Add(currentLatLong);
            _spawnedObjects.Add(spawnedWayspot);

        }

        _locations = new Vector2d[_locationStrings.Count];
        PlaceMarkers();
        SetMapView(MapView.FullViewMap);
        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ShowVpsRequestPanel);
    }

    /// <summary>
    /// Placement of Markers Based on Longitude and Latitude.
    /// </summary>
    void PlaceMarkers()
    {
        for (int i = 0; i < _locationStrings.Count; i++)
        {
            var locationString = _locationStrings[i];
            _locations[i] = Conversions.StringToLatLon(locationString);
            _spawnedObjects[i].transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
            Debug.Log("Current Position is " + _spawnedObjects[i].transform.localPosition);

        }

        // Place Player Marker...
        var playerLocation = VpsCoverageManager.Instance.LastKnownUserLocation != null
            ? $"{VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Latitude},{VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Longitude}"
            : _playerLocationString;
        Debug.Log($"User Location: {playerLocation}");
        PlaceUserMarkerOnMap(playerLocation);

        loadingPanel.SetActive(false);
        _map.gameObject.GetComponent<QuadTreeCameraMovement>().enabled = true;
        isMapLoading = false;
    }

    public void PlaceUserMarkerOnMap(string playerLocation)
    {
        // Place Player Marker...
        var player = Instantiate(playerMarkerPrefab);
        // var playerLocation = VpsCoverageManager.Instance.LastKnownUserLocation != null
        //     ? $"{VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Latitude},{VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Longitude}"
        //     : _playerLocationString;
        Debug.Log($"User Location: {playerLocation}");
        _locationPlayer = Conversions.StringToLatLon(playerLocation);
        player.transform.localPosition = _map.GeoToWorldPosition(_locationPlayer, true);
        player.transform.localScale = new Vector3(markerSpawnScale, markerSpawnScale, markerSpawnScale);
        spawnedPlayer = player;
    }

    private void Update()
    {
        // Stick Marker to its Original Location.
        int count = _spawnedObjects.Count;
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                var spawnedObject = _spawnedObjects[i];
                var location = _locations[i];
                spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
                spawnedObject.transform.localScale = new Vector3(markerSpawnScale, markerSpawnScale, markerSpawnScale);
            }
        }
       

        // For Player Marker.
        if (spawnedPlayer != null)
        {
            spawnedPlayer.transform.localPosition = _map.GeoToWorldPosition(_locationPlayer, true);
            spawnedPlayer.transform.localScale = new Vector3(markerSpawnScale, markerSpawnScale, markerSpawnScale);
        }
        
        UpdateLocalization();
    }

    void UpdateLocalization()
    {
        // if (NearbyVPSManager.Instance.selectedWayspot != null && Instance.DistanceToDisplay(NearbyVPSManager.Instance.selectedWayspot) <=
        //     NearbyVPSManager.Instance.distanceThreshold)
        // {
        //     if (hasUpdatedUIForPreLocalization) return;
        //     ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.PreLocalization);
        //     hasUpdatedUIForPreLocalization = true;
        // }
        // else 
        if (isNavigatingToActivity && SystemInfo.deviceUniqueIdentifier == SBContextManager.VonsDeviceID)
        {
            print($"Debug navigating to the activity...");
            var elapsed = DateTime.Now.Subtract(debugProbingStartTime);
            if (elapsed.Seconds <= 5) return;
            debugProbingStartTime = DateTime.MaxValue;
            if (hasUpdatedUIForPreLocalizationAfterNavigation) return;
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.PreLocalizationAfterNavigation);
            hasUpdatedUIForPreLocalizationAfterNavigation = true;
        }
    }
    
    /// <summary>
    /// Distance to show between player and the marker.
    /// </summary>
    /// <param name="currentSelctedMarker"></param>
    /// <returns></returns>
    public float DistanceToDisplay(LocalizationTarget? currentSelctedMarker)
    {
        if (currentSelctedMarker == null) return int.MaxValue;
        
        double[] playerLocation = new double[] { VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Latitude,
            VpsCoverageManager.Instance.LastKnownUserLocation!.Value.Longitude };
        double[] currentMarkerLocation = new double[] { currentSelctedMarker.Value.Center.Latitude, currentSelctedMarker.Value.Center.Longitude };
        CheapRuler cr = new CheapRuler(playerLocation[1], CheapRulerUnits.Meters);
        return (float)cr.Distance(playerLocation, currentMarkerLocation);
    }

    /// <summary>
    /// Disable Marker Prompts
    /// </summary>
    public void DisablePopUp()
    {
        foreach (var item in _spawnedObjects)
            item.transform.Find("PromptCanvas").gameObject.SetActive(false);
    }


    /// <summary>
    /// Clear lists of marker locations and objects.
    /// </summary>
    public void ClearSpawnedMarkers()
    {
        _locationStrings.Clear();
        foreach (var item in _spawnedObjects)
            Destroy(item);
    }

    void MarkerInteraction(bool interactable)
    {
        foreach (var item in _spawnedObjects)
            item.GetComponent<BoxCollider>().enabled = interactable;
    }

    public IEnumerator LoadImageFromURL(string url, SpriteRenderer spriteRendrer)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2());
            spriteRendrer.sprite = sprite;
        }
    }

    /// <summary> 
    /// Only to show the Selected Marker in AR/Navigation Map, Already called on Localize Button.
    /// </summary>
    public void DisableMarker(LocalizationTarget wayspotData)
    {
        foreach (var item in _spawnedObjects)
        {
            item.SetActive(false);
        }
        currentSelectedMarker = _spawnedObjects.Find(p => p.name == wayspotData.Identifier);
        currentSelectedMarker.SetActive(true);
        currentSelectedMarker.GetComponent<BoxCollider>().enabled = false;
    }

    /// <summary>
    /// Function to enable all available markers.
    /// Call this function if coming back from AR/Navigation Map to Full Screen Map.
    /// </summary>
    public void EnableMarkers()
    {
        foreach (var item in _spawnedObjects)
        {
            item.SetActive(true);
            item.GetComponent<BoxCollider>().enabled = true;
        }
        ApplyCameraSetting(60, 1, 1, true);
        currentSelectedMarker = null;
    }

    /// <summary>
    /// For Destroy All The Spawned Markers (Player and Wayspots)...
    /// </summary>
    public void DestroyAllMarkers()
    {
        if (_spawnedObjects.Count > 0)
        {
            foreach (var marker in _spawnedObjects)
                Destroy(marker);
        }

        if (spawnedPlayer != null)
            Destroy(spawnedPlayer);

    }

    /// <summary>
    /// Function to Enable or Disable the AR/Navigation Map at any stage.
    /// </summary>
    public void ToggleNavigationMap(bool show)
    {
        print($"2 - Toggling the navigation map -> {show}");
        if (spawnedPlayer != null)
            spawnedPlayer.SetActive(show);

        if (currentSelectedMarker!= null)
            currentSelectedMarker.SetActive(show);

        mapEssentials.SetActive(show);
        navigationMapCamera.gameObject.SetActive(show);
        mapCamera.gameObject.SetActive(show);
        
        navigationMapCamera.SetActive(show);
        navigationMapItem.SetActive(show);
    }

}



public enum MapView
{
    FullViewMap, ARViewMap
}

