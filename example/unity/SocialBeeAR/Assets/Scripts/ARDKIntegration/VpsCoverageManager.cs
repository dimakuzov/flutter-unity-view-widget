using System;
using System.Collections.Generic;
using Niantic.Lightship.AR.VpsCoverage;
using UnityEngine;

namespace SocialBeeAR
{
    public class VpsCoverageManager : BaseSingletonClass<VpsCoverageManager>
    {
        [Tooltip("GPS used in Editor and for testing Private VPS.")]
        // Default is the Ferry Building in San Francisco
        public LatLng ProxyLocation = new LatLng(37.7954, -122.3937);
        
        // [Tooltip("Used for testing Private VPS.")]
        // private bool _useProxyLocation;
        
        [SerializeField]
        [Range(0,500)] 
        private int _queryRadius = 250;
        
        /// <summary>
        /// The closest VPS-activated location to the user.
        /// </summary>
        [HideInInspector]
        public LocalizationTarget? ClosestWayspot { get; private set; }
        
        /// <summary>
        /// The VPS-activated location that the user chooses to localize with.
        /// </summary>
        [HideInInspector]
        public LocalizationTarget? SelectedWayspot { get; set; }

        [HideInInspector] public string SelectedWayspotId {
            get
            {
                return SelectedWayspot?.Identifier ?? "";
            }
        }

        /// <summary>
        /// Flag that sets if error or warning messages about the location is shown to the user.
        /// </summary>
        [HideInInspector]
        public bool ShowLocationMessage = true;

        private bool _pauseLocationUpdate = false;
        
        private CoverageClient _coverageClient;

        public Action<IReadOnlyDictionary<string, LocalizationTarget>, ErrorInfo> OnLocalizationTargetsFound;

        [HideInInspector] public LatLng? LastKnownUserLocation = null;
        
        void Awake()
        {
            _coverageClient = new CoverageClient();
        }

        public void StartTrackingUserLocation()
        {
            print($"VpsCoverageManager StartTrackingUserLocation start");

            if (ShowLocationMessage)
                BottomPanelManager.Instance.ShowMessagePanel("Finding available wayspots...");

            // The mockResponses object is a ScriptableObject containing the data that a Mock
            // implementation of the ICoverageClient will return. This is a required argument for using
            // the mock client on a mobile device. It is optional in the Unity Editor; the mock client
            // will simply use the data provided in the ARDK/VirtualStudio/VpsCoverage/VPS Coverage Responses.asset file.
            // _coverageClient = CoverageClientFactory.Create(_coverageClientRuntime, MockResponses);

            _pauseLocationUpdate = false;
            StartCoroutine(StartLocationServiceCoroutine());
        }
        
        private System.Collections.IEnumerator StartLocationServiceCoroutine()
        {
            // if (_useProxyLocation)
            // {
            //     Debug.Log($"Using proxy location: {ProxyLocation.Latitude}, {ProxyLocation.Longitude}");
            //     LastKnownUserLocation = ProxyLocation;
            //     RequestCoverageAtLocation(ProxyLocation);
            //     yield break;
            // }

            if (!Input.location.isEnabledByUser)
            {
                OnLocalizationTargetsFound?.Invoke(null, new ErrorInfo
                {
                    ErrorCode = ErrorCodes.NetworkError,
                    Title = "Location Services Disabled",
                    Message = "Please enable location services in your device settings."
                });
                
                yield break;
            }

            Input.location.Start(1f, 1f);

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (maxWait < 1)
            {
                OnLocalizationTargetsFound?.Invoke(null, new ErrorInfo
                {
                    ErrorCode = ErrorCodes.NetworkError,
                    Title = "Location Timeout",
                    Message = "Unable to determine device location."
                });
                
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                OnLocalizationTargetsFound?.Invoke(null, new ErrorInfo
                {
                    ErrorCode = ErrorCodes.NetworkError,
                    Title = "Location Failed",
                    Message = "GPS signal unavailable."
                });
                
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                var locationData = Input.location.lastData;
                var coordinates = new LatLng(locationData.latitude, locationData.longitude);
                
                LastKnownUserLocation = coordinates;
                RequestCoverageAtLocation(coordinates);
            }
        }
        
        private void RequestCoverageAtLocation(LatLng location)
        {
            print($"VpsCoverageManager RequestCoverageAtLocation start");
            if (_coverageClient == null)
            {
                Debug.LogError("CoverageClient is null!");
                return;
            }

            if (_pauseLocationUpdate)
            {
                Debug.Log("Location update paused");
                return;
            }

            Debug.Log($"Requesting VPS coverage at {location.Latitude}, {location.Longitude} with radius {_queryRadius}m");
            _coverageClient.TryGetCoverageAreas(location, _queryRadius, ProcessAreasResult);
        }

        public void StopTrackingUserLocation()
        {
            _pauseLocationUpdate = true;
            if (Input.location.isEnabledByUser && Input.location.status == LocationServiceStatus.Running)
            {
                Input.location.Stop();
                Debug.Log("Location service stopped");
            }
        }
        
        public void PauseTrackingUserLocation()
        {
            _pauseLocationUpdate = true;
        }

        public void StopCoverage()
        {
            StopTrackingUserLocation();
            _coverageClient = null;
            ClosestWayspot = null;
            SelectedWayspot = null;
            LastKnownUserLocation = null;
        }

        // private void OnLocationUpdated(LocationUpdatedArgs args)
        // {
        //     if (ARHelper.Instance.Session.State != ARSessionState.Running)
        //     {
        //         print($"Session state={ARHelper.Instance.Session.State}");
        //         return;
        //     }
        //     
        //     if (_pauseLocationUpdate) return;
        //     
        //     print($"Session state={ARHelper.Instance.Session.State} | Finding wayspots...");
        //     if (LocationService.Status != LocationServiceStatus.Running)
        //     {
        //         print($"LocationService.Status={LocationService.Status}");
        //         return;
        //     }
        //     LocationService.LocationUpdated -= OnLocationUpdated;
        //
        //     if (SBContextManager.Instance.context.enablePrivateVPSAnchors)
        //     {
        //         if (SystemInfo.deviceUniqueIdentifier == "069E2708-FAA2-542E-9408-9DBDC1462686") // put your device ID here
        //         {
        //             // Dev note: Use this to test your location.
        //             ProxyLocation = new LatLng(37.79531921750984, -122.39360429639748); 
        //         }
        //         LastKnownUserLocation = ProxyLocation;
        //         _coverageClient.RequestCoverageAreas(ProxyLocation, _queryRadius, ProcessAreasResult);
        //     }
        //     else
        //     {
        //         LastKnownUserLocation = args.LocationInfo.Coordinates;
        //         _coverageClient.RequestCoverageAreas(args.LocationInfo, _queryRadius, ProcessAreasResult);
        //     }
        // }
        
        private void ProcessAreasResult(CoverageAreasResult result)
        {
            if (result.Status != ResponseStatus.Success)
            {
                Debug.LogWarning("CoverageAreas request failed with status: " + result.Status);
                OnLocalizationTargetsFound(null,new ErrorInfo
                {
                    ErrorCode = ErrorCodes.WayspotInitializationFailed,
                    Title = "Coverage Areas Init Failed",
                    Message = "Cannot start querying for wayspot locations."
                });
                return;
            }
            if (result.Areas == null || result.Areas.Length == 0)
            {
                Debug.Log($"No areas found at {LastKnownUserLocation}.");
                OnLocalizationTargetsFound(null,new ErrorInfo
                {
                    ErrorCode = ErrorCodes.CoverageAreasNotFound,
                    Title = "Coverage Areas Not Found",
                    Message = "The app cannot find any wayspots near you."
                });
                return;
            }
            
            var allTargets = new List<string>();
            
            Debug.Log($"Showing available wayspots... [user location: {LastKnownUserLocation?.Latitude ?? 0},{LastKnownUserLocation?.Longitude ?? 0}]");
            foreach (var area in result.Areas)
            {
                allTargets.AddRange(area.LocalizationTargetIdentifiers);
            }
            
            _coverageClient.TryGetLocalizationTargets(allTargets.ToArray(), ProcessTargetsResult);
        }
        
        private void ProcessTargetsResult(LocalizationTargetsResult result)
        {
            print($"ProcessTargetsResult > status={result.Status}");
            if (result.Status != ResponseStatus.Success)
            {
                Debug.LogWarning($"Getting localization target failed: {result.Status}");
                OnLocalizationTargetsFound(null,new ErrorInfo
                {
                    ErrorCode = ErrorCodes.CoverageAreasNotFound,
                    Title = "Coverage Areas Not Found",
                    Message = "The app cannot find any wayspots near you."
                });
                return;
            }

            SelectedWayspot = null;
            ClosestWayspot = null;
            foreach (var target in result.ActivationTargets)
            {
                // Debug.Log($"{target.Key}: {target.Value.Name} | {target.Value.ImageURL} | {target.Value.Center.ToString()}");
                ClosestWayspot ??= target.Value;
            }
            
            // if (result.ActivationTargets.Count > 0)
            // {
            //     Vector2 imageSize = _targetImage.rectTransform.sizeDelta;
            //     LocalizationTarget firstTarget = result.ActivationTargets.FirstOrDefault().Value;
            //
            //     firstTarget.DownloadImage((int)imageSize.x, (int)imageSize.y, args => _targetImage.texture = args);
            //
            // }
            // print("Invoking OnLocalizationTargetsFound...");
            OnLocalizationTargetsFound(result.ActivationTargets, null);

        }
    }
}