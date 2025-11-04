using System;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SocialBeeARDK;
using UnityEngine.XR.ARFoundation;

namespace SocialBeeAR
{
    

    /// <summary>
    ///  The engine of SocialBeeAR
    /// </summary>
    public class SocialBeeARMain : MonoBehaviour //, PlacenoteListener
    {

        private float currentAnchorScanningProgress;
        private int lastMapSize;
        private bool mapQualityThresholdCrossed;
        private bool isNewCollection = true;
    
        // #placenote2lightship
        // private LibPlacenote.MapMetadataSettable currMapDetails;
        // #placenote2lightship
        // private LibPlacenote.MapInfo selectedMapInfo;

        //activity creation mode related items
        public ExecutionMode executionMode = ExecutionMode.Undefined;
        public ActivityInfo predefinedActivityInfo;

        //callback after AR is finished
        // public Action<string, LocationInfo> OnSingleActivityCreationSuccessful;
        // public Action<string> OnSingleActivityCeationFailed;
        public Action<string, LocationInfo> OnContinuousActivityCreationSuccessful;
        public Action<string> OnContinuousActivityCeationFailed;

        private bool isInitialized;
        
        [HideInInspector]
        private static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;
        
        private string SelectedMapId
        {
            get
            {
                // #placenote2lightship
                // return selectedMapInfo != null ? selectedMapInfo.placeId : null;
                return null;
            }
        }

        private static SocialBeeARMain _instance;
        public static SocialBeeARMain Instance
        {
            get
            {
                return _instance;
            }
        }


        //------------------------Monobehaviour methods-------------------------


        void Awake()
        {
            _instance = this;

            // 
            // NOTE: This should be present anywhere where we want to extract
            //       the user's location information such as the city name.
            //       It is best if we put this and the callback in SocialBeeARMain
            //       and then pass the information to the appropriate facade object.
            //
            // Start getting location information.            
            // LocationProxy.StartUpdatingLocation(name);
        }


        void Start()
        {
            print("#debugStart: SocialBeeARMain");
            UIManager.Instance.SetUIMode(UIManager.UIMode.Entrance);
        }
        
        
        public void StartAsStandaloneApp()
        {
            //set UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Initializing);
            UIManager.Instance.UpdateSBContext();
            
            //by default, hide the plane detected
            // PlaneManager.Instance.SetARPlanesVisible(false);
            ARHelper.Instance.StopPlaneDetection();
        
            // wait for ar session to start tracking and for placenote to initialize
            StartCoroutine(WaitForARSessionThenStart(() =>
            {
                //in standalone mode, the action after initialization is fixed, it just shows up the UI to the user.
                // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);
            }));
            print(string.Format("SocialBeeARMain started in mode: '{0}'", executionMode));
        }
        

        public void StartAsIntegratedModule(Action onInitialized)
        {
            //set UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Initializing);
            UIManager.Instance.UpdateSBContext();

            //prepare execution mode info
            InteractionManager.Instance.executionMode = ExecutionMode.Standalone;

            //by default, hide the plane detected
            // PlaneManager.Instance.SetARPlanesVisible(false);
            // No need to call "ARHelper.Instance.StopPlaneDetection()" here.
            
            // wait for ar session to start tracking and for placenote to initialize
            StartCoroutine(WaitForARSessionThenStart(() =>
            {
                //in integration mode, the action after initialization is customized.
                onInitialized(); 
            }));
            print(string.Format("2* SocialBeeARMain started in mode: '{0}'", executionMode));
        }


        private IEnumerator WaitForARSessionThenStart(Action onInitialized)
        {
            print("WaitForARSessionThenStart" + ": IsInitialized() = " + IsInitialized());
            if (!isInitialized) //initialize if not yet
            {
                
                print("SocialBeeARMain > WaitForARSessionThenStart > Initializing Placenote AR Session...");

                // #placenote2lightship
                //while (ARSession.state != ARSessionState.SessionTracking || !LibPlacenote.Instance.Initialized())
                while (ARSession.state != ARSessionState.SessionTracking)
                {
                    //print(string.Format("ARSession.state='{0}', PNInitialized='{1}'", ARSession.state, LibPlacenote.Instance.Initialized()));
                    yield return null;
                }

                // #placenote2lightship BEGIN
                //LibPlacenote.Instance.RegisterListener(this); // Register listener for onStatusChange and OnPose
                //FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);
                // #placenote2lightship END

                // AR Session has started tracking here. Now start the session
                Input.location.Start();

                // #placenote2lightship BEGIN
                // // Set up the localization thumbnail texture event.
                // PNThumbnailSelector.Instance.ReportTexture += OnGetThumbnail;

                MessageManager.Instance.ShowMessage("Initialization completed.");
                // print("Initialization completed.");

                isInitialized = true;
            }
            
            onInitialized();
        }
        

        private void OnGetThumbnail(Texture2D thumbnailTexture)
        {
            // #placenote2lightship BEGIN
            // if (LocalizationFacade.Instance.mapThumbnail == null)
            // {
            //     return;
            // }
            //
            // //the UI for showing thumbnail
            // RectTransform rectTransform = LocalizationFacade.Instance.mapThumbnail.rectTransform;
            // if (thumbnailTexture.width != (int)rectTransform.rect.width)
            // {
            //     rectTransform.SetSizeWithCurrentAnchors(
            //         RectTransform.Axis.Horizontal, thumbnailTexture.width * 2);
            //     rectTransform.SetSizeWithCurrentAnchors(
            //         RectTransform.Axis.Vertical, thumbnailTexture.height * 2);
            //     rectTransform.ForceUpdateRectTransforms();
            // }
            // LocalizationFacade.Instance.mapThumbnail.texture = thumbnailTexture;
            // #placenote2lightship END
        }

 
        //-----------------------------InitPanel-------------------------------


        // #placenote2lightship BEGIN
        // public void OnSearchMapsClick()
        // {
        //     MessageManager.Instance.ShowMessage("Searching for saved maps");
        //
        //     //TODO: later we can use this location info for querying the related maps.
        //     LocationInfo locationInfo = Input.location.lastData;
        //     
        //     LibPlacenote.Instance.SearchMaps(Const.MAP_PREFIX, (mapList) =>
        //     {
        //         foreach (Transform t in SelectMapFacade.Instance.mapListContentParent.transform)
        //         {
        //             Destroy(t.gameObject);
        //         }
        //
        //         print(string.Format("Found {0} maps.", mapList.Length));
        //         if (mapList.Length == 0)
        //         {
        //             MessageManager.Instance.ShowMessage("No maps found. Create a map first!");
        //             return;
        //         }
        //
        //         // Render the map list!
        //         foreach (LibPlacenote.MapInfo mapId in mapList)
        //         {
        //             if (mapId.metadata.userdata != null)
        //             {
        //                 Debug.Log(mapId.metadata.userdata.ToString(Formatting.None));
        //             }
        //
        //             AddMapToList(mapId);
        //         }
        //
        //         MessageManager.Instance.ShowMessage("Please select a map to load");
        //
        //         //update UI mode
        //         UIManager.Instance.SetUIMode(UIManager.UIMode.SelectMap);
        //         SelectMapFacade.Instance.SetUIMode(SelectMapFacade.UIMode.SelectMap_NoMapSelected);
        //
        //     });
        // }
        //
        //
        // private void AddMapToList(LibPlacenote.MapInfo mapInfo)
        // {
        //     GameObject newElement = Instantiate(SelectMapFacade.Instance.mapInfoElementPrefab) as GameObject;
        //     if (newElement != null)
        //     {
        //         SBMapInfoElement listElement = newElement.GetComponent<SBMapInfoElement>();
        //         listElement.Initialize(mapInfo, SelectMapFacade.Instance.mapListContentToggleGroup, SelectMapFacade.Instance.mapListContentParent, (value) =>
        //         {
        //             OnMapSelected(mapInfo);
        //         });
        //     }
        // }
        //
        //
        // private void OnMapSelected(LibPlacenote.MapInfo mapInfo)
        // {
        //     selectedMapInfo = mapInfo;
        //
        //     //UIManager.Instance.SetUIMode(UIManager.UIMode.SelectMap); //toggle button state will be ruined if enabled...
        //     SelectMapFacade.Instance.SetUIMode(SelectMapFacade.UIMode.SelectMap_MapSelected);
        //     //print("Map selected: " + mapInfo.placeId);
        // }
        // #placenote2lightship END


        public void OnNewActivityClick()
        {
            if (!IsInitialized()) return;

            // UI navigation and label updates to signal entry into mapping mode
            MessageManager.Instance.ShowMessage("Point at any flat surface, like a table, then hit the + button to place the model");

            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.ActivitySetting);
            ActivitySettingFacade.Instance.SetUIMode(ActivitySettingFacade.UIMode.ActivitySetting_PlacingReticle);

            //start placing reticle
            GetComponent<ReticleController>().StartReticle();
            ActivitySettingFacade.Instance.EnableTapAnimation(true);

            // #placenote2lightship - we don't need this in ARDK
            //set detected planes visible
            // PlaneManager.Instance.SetARPlanesVisible(true);
        }
        
        
        public void OnBackToNativeClick()
        {
            //replace below code with integration code here... 
            UIManager.Instance.SetUIMode(UIManager.UIMode.Entrance);

            NativeCall nativeCall = NativeCall.Instance;
            nativeCall.ShowNative("Back");

            // LocationProxy.StopUpdatingLocation();
        }

 
        //-------------------------ActivitySettingPanel-------------------------

        public void OnReticlePlaced(GameObject reticle)
        {
            GetComponent<ReticleController>().StopReticle();
            ActivitySettingFacade.Instance.EnableTapAnimation(false);
            
            //place the anchor object
            print(string.Format("Creating activity in '{0}' mode", executionMode.ToString()));
             
            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.ActivitySetting);
            ActivitySettingFacade.Instance.SetUIMode(ActivitySettingFacade.UIMode.ActivitySetting_SpawnedButNoActivitySeleted);
                
            MessageManager.Instance.ShowMessage("Tap the object to choose an activity.");
        }
        
        
        /// <summary>
        /// When user confirm the position and activity type of an anchor
        /// </summary>
        public void OnConfirmActivitySettingClick()
        {
            var anchorController = ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>();
            //////anchorController.SetUIMode(ContentFacade.UIMode.Creator_PostSetting);
            //////anchorController.SetBehaviourMode(AnchorBehaviourMode.Scanning);
            
            switch (executionMode)
            {

                case ExecutionMode.Integrated:
                    //////ActiveAnchorManager.StartEditingCurrentAnchor();
                    break;
                
                case ExecutionMode.Standalone:
                    //////ActiveAnchorManager.StartEditingCurrentAnchor(); //start editing activity name
                    break;    
            }
        }


        public void OnCancelCreatorProcess()
        {
            MessageManager.Instance.ShowMessage("Activity creation canceled.");
            
            //stop reticle if it's existing
            if(UIManager.Instance.GetCurrentUIMode() == UIManager.UIMode.ActivitySetting)
                GetComponent<ReticleController>().StopReticle();

            //stop placenote session
            // #placenote2lightship 
            // LibPlacenote.Instance.StopSession();
            // FeaturesVisualizer.ClearPointcloud();
            
            //clear created anchor objects
            GetComponent<AnchorManager>().ClearAnchors();
            
            //update UI mode to the initial mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);

            //critical: reset some flags!
            this.lastMapSize = 0; //reset the map size based on the last scanned anchor.
            isNewCollection = true; //reset the flag for creating collection
            
            //reset some debug info (optional)
            MessageManager.Instance.UpdateMapSize(0);
            MessageManager.Instance.UpdateCurrentAnchorSize(0);
        }


        //----------------------------MappingPanel-----------------------------


        public void OnStartScanningClick()
        {
            //start progress info panel
            UIManager.Instance.SetUIMode(UIManager.UIMode.Mapping);
            MappingFacade.Instance.SetUIMode(MappingFacade.UIMode.Mapping_Scanning);
            //////ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetUIMode(ContentFacade.UIMode.Creator_Scanning);
            
            // #placenote2lightship
            // StartMapping();
        }


        //-----------------------ContinuousMappingPanel-------------------------


        public void OnSaveMapClick()
        {
            if (!IsInitialized()) return;

            if (!mapQualityThresholdCrossed)
            {
                MessageManager.Instance.ShowMessage(string.Format("Map quality is not good enough to save. Scan a small area with many features and try again."));
                return;
            }

            bool isLocationServiceAvailable = Input.location.status == LocationServiceStatus.Running;
            LocationInfo locationInfo = Input.location.lastData;
            
            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.ContinuousMapping);
            ContinuousMappingFacade.Instance.SetUIMode(ContinuousMappingFacade.UIMode.SavingMap);

            MessageManager.Instance.ShowMessage("Saving...");

            // #placenote2lightship BEGIN
            // LibPlacenote.Instance.SaveMap((mapId) =>
            // {
            //     LibPlacenote.Instance.StopSession();
            //     FeaturesVisualizer.ClearPointcloud();
            //
            //     //continousMappingPanel.SetActive(false); //why again???
            //
            //     LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();
            //
            //     metadata.name = Utilities.GenerateMapId();
            //     MessageManager.Instance.ShowMessage("Saving map: " + metadata.name);
            //
            //     JObject userdata = new JObject();
            //     metadata.userdata = userdata;
            //
            //     JObject anchorList = GetComponent<AnchorManager>().AnchorInfoListToJSON();
            //
            //     userdata[Const.ANCHOR_DATA_JSON_ROOT] = anchorList;
            //     GetComponent<AnchorManager>().ClearAnchors();
            //
            //     if (isLocationServiceAvailable)
            //     {
            //         metadata.location = new LibPlacenote.MapLocation();
            //         metadata.location.latitude = locationInfo.latitude;
            //         metadata.location.longitude = locationInfo.longitude;
            //         metadata.location.altitude = locationInfo.altitude;
            //     }
            //
            //     LibPlacenote.Instance.SetMetadata(mapId, metadata, (success) =>
            //     {
            //         if (success)
            //         {
            //             Debug.Log("Meta data successfully saved!");
            //         }
            //         else
            //         {
            //             Debug.Log("Meta data failed to save");
            //         }
            //     });
            //     currMapDetails = metadata;
            // }, (completed, faulted, percentage) =>
            // {
            //     if (completed)
            //     {
            //         MessageManager.Instance.ShowMessage($"Upload Complete! executionMode={executionMode}");
            //         
            //         if (executionMode == ExecutionMode.Integrated)
            //         {
            //             MessageManager.Instance.ShowMessage("Upload Complete!");
            //
            //             //reset some flags
            //             this.lastMapSize = 0;
            //             isNewCollection = true;
            //             
            //             //invoke callback to switch back to native module
            //             OnContinuousActivityCreationSuccessful(currMapDetails.name, locationInfo);
            //
            //             NativeCall nativeCall = FindObjectOfType<NativeCall>();
            //             nativeCall.EndAR();
            //         }
            //         else if(executionMode == ExecutionMode.Standalone)
            //         {
            //             MessageManager.Instance.ShowMessage("Upload Complete! You can now click My Maps and choose a map to load.");
            //
            //             //reset some flags
            //             this.lastMapSize = 0;
            //             isNewCollection = true; 
            //             
            //             //return back to the initial UI
            //             UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            //             InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);
            //         }
            //     }
            //     else if (faulted)
            //     {
            //         MessageManager.Instance.ShowMessage(string.Format("Upload of map '{0}' failed", currMapDetails.name));
            //         // if (executionMode == ExecutionMode.Integrated_SingleAnchorCreationWithSpecifiedActivity)
            //         //     OnSingleActivityCeationFailed(currMapDetails.name);
            //         // else 
            //         if(executionMode == ExecutionMode.Integrated)
            //             OnContinuousActivityCeationFailed(currMapDetails.name);
            //     }
            //     else //updating progress percentage info.
            //     {
            //         MessageManager.Instance.ShowMessage(string.Format("Uploading map ( {0} %)", (percentage * 100.0f).ToString("F2")));
            //     }
            // });
            // #placenote2lightship END
        }


        //------------------------------LocalizationPanel--------------------------------


        // #placenote2lightship
        // private void StartMapping()
        // {
        //     MessageManager.Instance.ShowMessage("Slowly move around the object.");
        //
        //     //disable detected planes
        //     PlaneManager.Instance.SetARPlanesVisible(false);
        //
        //     //reset progress status
        //     MappingFacade.Instance.ResetMappingProgress();
        //     mapQualityThresholdCrossed = false;
        //     currentAnchorScanningProgress = 0f;
        //
        //     // Enable point-cloud
        //     FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);
        //
        //     isScanningComplete = false;
        //     if (isNewCollection)
        //     {
        //         LibPlacenote.Instance.StartSession();
        //         print("New mapping started");
        //     }
        //     else
        //     {
        //         LibPlacenote.Instance.RestartSendingFrames(); //resume passing camera frames to PlaceNote
        //         print("Mapping resumed");
        //     }
        // }


        public void OnExitMapClick()
        {
            MessageManager.Instance.ShowMessage("Session was reset. You can start new map or load your map again.");

            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);

            // #placenote2lightship BEGIN
            // LibPlacenote.Instance.StopSession();
            // FeaturesVisualizer.ClearPointcloud();
            // #placenote2lightship END

            GetComponent<AnchorManager>().ClearAnchors();
        }
        
        
        public void OnExtendMapClick()
        {
            /*TODO: This is not correct! Before switching to ContinuousMappingMode, we should enter 'ExtendMap' mode,
            which guide user to go back to one of the previous anchor and locate there, after then switch
            to 'ContinuosMappingMode' */
            //UIManager.Instance.SetUIMode(UIManager.UIMode.ContinuousMapping);

            ////LibPlacenote.Instance.StopSession(); //NO NEED!
            ////DebugMessageManager.Instance.PrintDebugMessage("Session stopped");

            //FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);

            //LibPlacenote.Instance.StartSession(true);
            //print("Session started in extending mode");
        }


        public void OnUpdateActivityClick()
        {
            if (!IsInitialized()) return;

            MessageManager.Instance.ShowMessage("Updating anchor info...");

            // #placenote2lightship BEGIN
            // LibPlacenote.MapMetadataSettable metadataUpdated = new LibPlacenote.MapMetadataSettable();
            //
            // metadataUpdated.name = selectedMapInfo.metadata.name;
            //
            // JObject userdata = new JObject();
            // metadataUpdated.userdata = userdata;
            //
            // JObject notesList = GetComponent<AnchorManager>().AnchorInfoListToJSON();
            // userdata[Const.ANCHOR_DATA_JSON_ROOT] = notesList;
            // metadataUpdated.location = selectedMapInfo.metadata.location;
            //
            // LibPlacenote.Instance.SetMetadata(SelectedMapId, metadataUpdated, (success) =>
            // {
            //     if (success)
            //     {
            //         MessageManager.Instance.ShowMessage("Anchor updated! To end the session, click Exit.");
            //         print("Anchor info successfully updated.");
            //     }
            //     else
            //     {
            //         print("Anchor info failed to save");
            //     }
            // });
            // #placenote2lightship END
        }
        
        
        //---------------------------------Others--------------------------------


        public void OnEditAcivityNameDone()
        {
            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Mapping);
            MappingFacade.Instance.SetUIMode(MappingFacade.UIMode.Mapping_PreScanning);
            //////ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetUIMode(ContentFacade.UIMode.Creator_PreScanning);

            MessageManager.Instance.ShowMessage("Make sure to stay closed to the activity object, tap 'Yes' when ready");
        }


        //use-less now
        // public void OnActivitySelected()
        // {
        //     //enable confirm button
        //     UIManager.Instance.SetUIMode(UIManager.UIMode.ActivitySetting);
        //     ActivitySettingFacade.Instance.SetUIMode(ActivitySettingFacade.UIMode.ActivitySetting_SpawnedAndActivitySelected);
        // }


        //-------------------------- PlaceNote callback---------------------------

        /// <summary>
        /// PlaceNote callback: When 'Pose'
        /// </summary>
        /// <param name="outputPose"></param>
        /// <param name="arkitPose"></param>
        public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
        {
            // #placenote2lightship BEGIN
            // if (LibPlacenote.Instance.IsPerformingMapping())
            // {
            //     //CheckIfEnoughPointcloudCollectedForMap();
            //     CheckIfEnoughPointcloudCollectedForAnchor();
            // }
            // #placenote2lightship END
        }


        private bool isScanningComplete = false;
        private void CheckIfEnoughPointcloudCollectedForAnchor()
        {
            // get the full point built so far
            int currentAnchorSize = GetCurrentAnchorSize();

            currentAnchorScanningProgress = (float)currentAnchorSize / (float)Const.MIN_MAP_SIZE;
            MappingFacade.Instance.SetMappingProgress(currentAnchorScanningProgress); 

            // #placenote2lightship BEGIN
            // if (currentAnchorSize >= Const.MIN_MAP_SIZE)
            // {
            //     if (LibPlacenote.Instance.GetMode() == LibPlacenote.MappingMode.MAPPING)
            //         MessageManager.Instance.ShowMessage("Enough information collected for this activity.");
            //
            //     // Check the map quality to confirm whether you can save
            //     if (LibPlacenote.Instance.GetMappingQuality() == LibPlacenote.MappingQuality.GOOD && !isScanningComplete)
            //     {
            //         OnScanningComplete();
            //         isScanningComplete = true;
            //     }
            // }
            // #placenote2lightship END
        }


        private void OnScanningComplete()
        {
            print(string.Format(
                "Scanning completed, activityCreationMode = '{0}'", executionMode.ToString()));
             
            //when enough feature points are collected!
            mapQualityThresholdCrossed = true;

            //set the last map size!!!
            // #placenote2lightship 
            // this.lastMapSize = LibPlacenote.Instance.GetMap().Length;
            
            //pause mapping
            PauseMapping();

            //update UI mode
            MappingFacade.Instance.SetUIMode(MappingFacade.UIMode.Mapping_PostScanning);
            //////ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetUIMode(ContentFacade.UIMode.Creator_PostScanning);
            MessageManager.Instance.ShowMessage("Excellent! \nYou may continue to creat more activities or save the map.");
            
            // }
        }
        

        private void PauseMapping()
        {
            this.isNewCollection = false;

            //pause scanning
            // #placenote2lightship 
            // LibPlacenote.Instance.StopSendingFrames();
        }


        private void StopMapping()
        {
            this.isNewCollection = false;

            //pause scanning
            // #placenote2lightship
            // LibPlacenote.Instance.StopSession();
        }


        public void OnPostScanningConfirmed()
        {
            //switch anchor BehaviourMode
            //////ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetBehaviourMode(AnchorBehaviourMode.PostScanningBeforeSaving);

            //switching UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.ContinuousMapping);
            ContinuousMappingFacade.Instance.SetUIMode(ContinuousMappingFacade.UIMode.Init);
        }


        private int GetCurrentAnchorSize()
        {
            // #placenote2lightship
            // //another alternative method to get feature point count:
            // //List<Vector3> fullPointCloudMap = FeaturesVisualizer.GetPointCloud();
            // //fullPointCloudMap.Count
            //
            // LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap();
            // if (map != null && map.Length > 0)
            //     return map.Length - this.lastMapSize;
            // else
            //     return 0;
            return 0;
        }


        // #placenote2lightship BEGIN
        // /// <summary>
        // /// PlaceNote event: When PlaceNote mapping status is changed.
        // /// </summary>
        // /// <param name="prevStatus"></param>
        // /// <param name="currStatus"></param>
        // public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
        // {
        //    
        //     string currentMode = LibPlacenote.Instance.GetMode().ToString();
        //     string status = currStatus.ToString();
        //     
        //     MessageManager.Instance.UpdateStatus(currentMode, status);
        //     Debug.Log(string.Format("Mode: '{0}', Status changed: '{1}'->'{2}'", currentMode, prevStatus.ToString(), status));
        //     
        //     if (currStatus == LibPlacenote.MappingStatus.LOST && prevStatus == LibPlacenote.MappingStatus.WAITING)
        //     {
        //         MessageManager.Instance.ShowMessage("Point your phone at the area shown in the thumbnail");
        //     }
        //     
        //     MessageManager.Instance.EnableMappingQualityInfo(LibPlacenote.Instance.IsPerformingMapping());
        //     
        // }
        // #placenote2lightship END


        /// <summary>
        /// PlaceNote callback: when re-localisation happened
        /// </summary>
        public void OnLocalized()
        {
            // #placenote2lightship BEGIN
            // this.lastMapSize = 0;
            //     
            // MessageManager.Instance.ShowMessage("Localized. Add or edit notes and click Update. Or click Exit to end the session.");
            // print("Localized, loading virtual objects...");
            // GetComponent<AnchorManager>().AnchorInfoListFromJSON(selectedMapInfo.metadata.userdata);
            // print("Content loaded.");
            //
            // //update UI mode
            // UIManager.Instance.SetUIMode(UIManager.UIMode.Localization);
            // LocalizationFacade.Instance.SetUIMode(LocalizationFacade.UIMode.Localization_Localized);
            //
            // FeaturesVisualizer.DisablePointcloud();
            // #placenote2lightship END
        }
        
        //--------------------------------- Utils ------------------------------
        public bool IsInitialized()
        {
            return isInitialized;
        }

    }

}