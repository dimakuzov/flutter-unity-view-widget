using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ARLocation;
using SocialBeeARDK;
using UnityEngine;
using UnityEngine.Serialization;


namespace SocialBeeAR
{
    
    public class GPSAnchorManager : BaseSingletonClass<GPSAnchorManager>
    {

        [SerializeField] private Camera ARCamera;
        [SerializeField] private GameObject ObjPrefab;
        private List<GameObject> gpsAnchorObjList = new List<GameObject>();

        [SerializeField] private GameObject askGPSOrNotPanel;
        [SerializeField] private GameObject gpsStrengthWeak;
        [SerializeField] private GameObject gpsStrengthMedium;
        [SerializeField] private GameObject gpsStrengthStrong;

        [SerializeField] private GameObject gpsMarkerPrefab;

        //the current engaged anchor object
        private GameObject currentEngagedAnchorObj;
        private PlaceAtLocation.PlaceAtOptions opts;

        //GPS signal strength
        private double accuracyRadius;
        private double strengthPercentage;
        
        //GPS markers are GameObjects which actually be managed by GPS+AR components.
        private Dictionary<string, GameObject> gpsMarkerDict = new Dictionary<string, GameObject>();
        
        [HideInInspector]
        private static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;
        
        // Start is called before the first frame update
        void Start()
        {
            //prepare options
            opts = new PlaceAtLocation.PlaceAtOptions()
            {
                HideObjectUntilItIsPlaced = true,
                MaxNumberOfLocationUpdates = 1, //2
                MovementSmoothing = 0f, //0.1f
                UseMovingAverage = false
            };
            
            gpsStrengthWeak.SetActive(false);
            gpsStrengthMedium.SetActive(false);
            gpsStrengthStrong.SetActive(false);
        }


        private void Update()
        {
            UpdateGPSSignalStrength();
            
            //update color according to signal strength
            if (SBContextManager.Instance.context != null && !SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
            {
                UpdateGPSAnchorColorAccordingToGPSSignalStrength();    
            }

            GameObject currVisiblePanel = BottomPanelManager.Instance.GetCurrentVisiblePanel();
            if (askGPSOrNotPanel == currVisiblePanel)
            {
                //MessageManager.Instance.DebugMessage("ask panel pop-up!!!");
                UpdateIndicatorAccodingToGPSSignalStrength();
            }
        }


        private void UpdateGPSSignalStrength()
        {
            //this accuracy number means the the radius in meter
            this.accuracyRadius = ARLocationProvider.Instance.CurrentLocation.accuracy;
            this.strengthPercentage  = Mathf.Min(1, (float)accuracyRadius / 25.0f); // this algorithm is from AR-GPS asset
            //MessageManager.Instance.DebugMessage($"GPS signal strength = {strengthPercentage}/1");
        }


        private void UpdateIndicatorAccodingToGPSSignalStrength()
        {
            //MessageManager.Instance.DebugMessage($"GPS signal strength = {strengthPercentage}");
            
            if (this.strengthPercentage == 0)
                return;
            
            //update the strength info on panel
            if (strengthPercentage <= Const.STRENGTH_WEAK && strengthPercentage > 0)
            {
                gpsStrengthWeak.SetActive(true);
                gpsStrengthMedium.SetActive(false);
                gpsStrengthStrong.SetActive(false);
            }
            else if (strengthPercentage > Const.STRENGTH_WEAK && strengthPercentage <= Const.STRENGTH_MEDIUM)
            {
                gpsStrengthWeak.SetActive(true);
                gpsStrengthMedium.SetActive(true);
                gpsStrengthStrong.SetActive(false);
            }
            else if (strengthPercentage > Const.STRENGTH_MEDIUM && strengthPercentage <= 1)
            {
                gpsStrengthWeak.SetActive(true);
                gpsStrengthMedium.SetActive(true);
                gpsStrengthStrong.SetActive(true);
            }
            else
            {
                gpsStrengthWeak.SetActive(false);
                gpsStrengthMedium.SetActive(false);
                gpsStrengthStrong.SetActive(false);
            }
        }
        
        
        private void UpdateGPSAnchorColorAccordingToGPSSignalStrength()
        {
            if (this.strengthPercentage == 0)
                return;
            
            //update GPS anchors' UI mode
            for (int i = 0; i < gpsAnchorObjList.Count; i++)
            {
                GameObject gpsAnchor = gpsAnchorObjList[i];
                if (gpsAnchor != null)
                {
                    GPSAnchorController controller = gpsAnchor.GetComponent<GPSAnchorController>();
                    if (strengthPercentage <= Const.STRENGTH_WEAK && strengthPercentage > 0)
                        controller.SetUIMode(GPSAnchorController.UIMode.Good);
                    else if(strengthPercentage > Const.STRENGTH_WEAK && strengthPercentage <= Const.STRENGTH_MEDIUM)
                        controller.SetUIMode(GPSAnchorController.UIMode.Medium);
                    else if(strengthPercentage > Const.STRENGTH_MEDIUM && strengthPercentage <= 1)
                        controller.SetUIMode(GPSAnchorController.UIMode.Bad);
                    else
                        controller.SetUIMode(GPSAnchorController.UIMode.Undefined);
                }
            }
        }


        public void ClearGPSMarkers()
        {
            foreach (KeyValuePair<string, GameObject> kv in this.gpsMarkerDict)
            {
                GameObject markerObj = kv.Value;
                markerObj.GetComponentInChildren<PulseController>().StopPulseAnimation();
                Destroy(markerObj);
            }
            
            this.gpsMarkerDict.Clear();
        }
        

        public void ShowGPSAnchors(Action postAction = null)
        {
            //------------------ for testing, only printing information------------
            // IEnumerable<AnchorDto> mapAnchorsDto = SBContextManager.Instance.context.MapAnchors;
            // foreach (var dto in mapAnchorsDto)
            // {
            //     MessageManager.Instance.DebugMessage("--->map anchor: " + dto.id);
            // }
            //
            // IEnumerable<AnchorDto> otherAnchorsDto = SBContextManager.Instance.context.OtherAnchors;
            // foreach (var dto in otherAnchorsDto)
            // {
            //     MessageManager.Instance.DebugMessage("--->other anchor: " + dto.id);
            // }
            //
            // IEnumerable<AnchorDto> allAnchorsDto = SBContextManager.Instance.context.anchors;
            // foreach (var dto in allAnchorsDto)
            // {
            //     MessageManager.Instance.DebugMessage("--->all anchor: " + dto.id);
            // }
            //--------------------------------------------------------------------
            
            IEnumerable<AnchorDto> mapAnchors = SBContextManager.Instance.context.MapAnchors;
            IEnumerable<AnchorDto> otherAnchors = SBContextManager.Instance.context.OtherAnchors;
            IEnumerable<AnchorDto> allAnchors = SBContextManager.Instance.context.MapAnchors;

            // // To guarantee that we will only show one anchor even if there were other anchors passed from Native.
            // if (!SBContextManager.Instance.context.isPlanning)
            // {                
            //     print(string.Format("Showing {0} GPS anchors from {1} anchors...", otherAnchors.Count(), allAnchors.Count()));
            //     foreach (var dto in otherAnchors)
            //     {
            //         //prepare the location info for GPS component
            //         var loc = new ARLocation.Location()
            //         {
            //             Latitude = dto.latitude,
            //             Longitude = dto.longitude,
            //             Altitude = -1f,
            //             AltitudeMode = AltitudeMode.DeviceRelative
            //         };
            //
            //         //init GPS object
            //         GameObject gpsObj = Instantiate(ObjPrefab);
            //         gpsObj.GetComponent<GPSAnchorController>().Init(ARCamera, dto); //this is for showing distance
            //
            //         //show!
            //         PlaceAtLocation.AddPlaceAtComponent(gpsObj, loc, opts, false);
            //         gpsAnchorObjList.Add(gpsObj);
            //     }
            // }
            
            //For GPS-only anchors
            if (SBContextManager.Instance.context.isPlanning ||
                !SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) return;
            
            ClearGPSMarkers();
                
            // print($"Showing GPS anchors count={mapAnchors.Count()} #debugpulse");
            foreach (var dto in mapAnchors) 
            {
                //prepare the location info for GPS component
                var loc = new ARLocation.Location()
                {
                    Latitude = dto.latitude,
                    Longitude = dto.longitude,
                    Altitude = -1,
                    AltitudeMode = AltitudeMode.DeviceRelative
                };

                GameObject gpsMarker = Instantiate(gpsMarkerPrefab);
                gpsMarker.GetComponent<MarkerController>().SetPinVisible(false);
                gpsMarkerDict.Add(dto.id, gpsMarker);
                    
                //set it managed by AR+GPS asset
                PlaceAtLocation.AddPlaceAtComponent(gpsMarker, loc, opts, false);
            }
            StartCoroutine(DelayedUpdateAnchorPos(1, postAction));
        }

        
        private IEnumerator DelayedUpdateAnchorPos(float delayedTime, Action postAction = null)
        {
            yield return new WaitForSeconds(delayedTime);

            foreach (KeyValuePair<string, GameObject> kv in this.gpsMarkerDict)
            {
                string anchorId = kv.Key;
                GameObject marker = kv.Value;
                
                GameObject rebornAnchorObj = ActiveAnchorManager.GetAnchorObjectById(anchorId);
                AnchorController aController = rebornAnchorObj.GetComponent<AnchorController>();
                aController.markerObj = marker;
                marker.GetComponent<MarkerController>().SetAnchorController(aController);
                
                rebornAnchorObj.transform.position = marker.transform.position; //put anchor the same position as marker
                MessageManager.Instance.DebugMessage($"[DelayedUpdateAnchorPos] updating anchor '{anchorId}''s pos to '{marker.transform.position}'");
                
                marker.GetComponentInChildren<PulseController>().StartPulseAnimation();
            }
            
            //post action
            if(postAction != null)
                postAction.Invoke();
        }

        
        public List<GameObject> GetAnchorObjList()
        {
            return gpsAnchorObjList;
        }


        public void RegisterCurrentAnchor(GameObject engagedAnchorObj)
        {
            this.currentEngagedAnchorObj = engagedAnchorObj;
        }


        public void ClearCurrentAnchor()
        {
            this.currentEngagedAnchorObj = null;
        }


        public void ClearAnchors()
        {
            for (int i = 0; i < this.gpsAnchorObjList.Count; i++)
            {
                GameObject gpsObj = gpsAnchorObjList[i];
                if (gpsObj == null) continue;
                
                PlaceAtLocation script = gpsObj.GetComponent<PlaceAtLocation>();
                GroundHeight script2 = gameObject.AddComponent<GroundHeight>();
                Destroy(script);
                Destroy(script2);
            }
            
            for (int i = 0; i < this.gpsAnchorObjList.Count; i++)
            {
                GameObject gpsObj = gpsAnchorObjList[i];
                if (gpsObj == null) continue;
                
                Destroy(gpsObj);
            }
            
            gpsAnchorObjList.Clear();
        }


        public GameObject GetCurrentEngagedGPSAnchor()
        {
            return currentEngagedAnchorObj;
        }


        public void SetInteractable(bool interactable, int ignoreIndex = -1)
        {            
            if (gpsAnchorObjList == null) return;

            for (int i = 0; i < this.gpsAnchorObjList.Count; i++)
            {
                if (ignoreIndex != -1)
                {
                    if(i == ignoreIndex)
                        continue;
                }
                

                var anchorController = gpsAnchorObjList[i].GetComponent<GPSAnchorController>();                
                if (anchorController != null)
                    anchorController.SetInteractable(interactable);
            }
        }

        
        //-------------------------------- dummy ------------------------------------


        public void ShowGPSAnchorsDummy()
        {
            //gpsObjList.Clear();
            ClearAnchors(); //Todo: it might have an exception(due to a bug of ARGPS asset), replace it with newer version.
            
            List<ARLocation.Location> locList = PrepareDummyLocations();
            // IEnumerable<AnchorDto> otherAnchors = SBContextManager.Instance.context.OtherAnchors;
            foreach (var loc in locList)
            {
                string title = "Changi Airport";
                string desc = "The biggest airport in the world...";

                //init GPS object
                GameObject gpsObj = Instantiate(ObjPrefab);
                gpsObj.GetComponent<GPSAnchorController>().Init(ARCamera, null); //this is for showing distance
                
                //show!
                PlaceAtLocation.AddPlaceAtComponent(gpsObj, loc, opts, false);
                gpsAnchorObjList.Add(gpsObj);
            }
        }
        

        private List<ARLocation.Location> PrepareDummyLocations()
        {
            
            var loc0 = new ARLocation.Location()
            {
                //60.268740, 24.855269
                Latitude = 60.268740, 
                Longitude = 24.855269,
                Altitude = 0f,
                AltitudeMode = AltitudeMode.GroundRelative
            };
        
            var loc1 = new ARLocation.Location()
            {
                //60.268705, 24.855459
                Latitude = 60.268705, 
                Longitude = 24.855459,
                Altitude = 0f,
                AltitudeMode = AltitudeMode.GroundRelative
            };
        
            var loc2 = new ARLocation.Location()
            {
                //60.268614, 24.855796
                Latitude = 60.268614, 
                Longitude = 24.855796,
                Altitude = 0f,
                AltitudeMode = AltitudeMode.GroundRelative
            };
        
            var loc3 = new ARLocation.Location()
            {
                //60.268367, 24.856514
                Latitude = 60.268367,
                Longitude = 24.856514,
                Altitude = 0f,
                AltitudeMode = AltitudeMode.GroundRelative
            };
        
            var loc4 = new ARLocation.Location()
            {
                //60.267857, 24.856845
                Latitude = 60.267857, 
                Longitude = 24.856845,
                Altitude = 0f,
                AltitudeMode = AltitudeMode.GroundRelative
            };
        
            var loc5 = new ARLocation.Location()
            {
                //60.266378, 24.856746
                Latitude = 60.266378, 
                Longitude = 24.856746,
                Altitude = 0f,
                AltitudeMode = AltitudeMode.GroundRelative
            };



            List<ARLocation.Location> locationList = new List<ARLocation.Location>();
            locationList.Add(loc0);
            locationList.Add(loc1);
            locationList.Add(loc2);
            locationList.Add(loc3);
            locationList.Add(loc4);
            locationList.Add(loc5);

            return locationList;
        }

    }
    
}

