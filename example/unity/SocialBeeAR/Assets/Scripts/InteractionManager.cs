using System;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.PersistentAnchors;
using Niantic.Lightship.AR.VpsCoverage;
using Serilog;
using SocialBeeARDK;
using UnityEngine.XR.ARSubsystems;
using ARDKAR = Niantic.ARDK.AR;


namespace SocialBeeAR
{

    /// <summary>
    /// This is for controlling how AR is executed. e.g. it can be executed as a standalone Unity app, or be integrated
    /// as a module in iOS native app.
    /// </summary>
    public enum ExecutionMode
    {
        Undefined,
        Integrated,
        Standalone,
        TestingARSession
    }


    /// <summary>
    ///  The engine of SocialBeeAR
    /// </summary>
    public class InteractionManager : BaseSingletonClass<InteractionManager>//, PlacenoteListener
    {
        private NativeCall nativeCall;

        private float currentAnchorScanningProgress;
        private int lastMapSize;
        private bool mapQualityThresholdCrossed;
        private bool isNewCollection = true;

        // #placenote2lightship
        // private LibPlacenote.MapMetadataSettable lastSavedMapMetadata;
        // #placenote2lightship
        // private LibPlacenote.MapInfo selectedMapInfo;
        private string lastSavedMapId; //ID of the last successfully saved map
        private List<string> activityIdsToUdpate;
        
        //activity creation mode related items
        public ExecutionMode executionMode = ExecutionMode.Undefined;

        //callback after AR is finished
        public Action<string> OnMapSavedSuccessful;
        public Action<string> OnMapSavedFailed;
        
        //indicator of whether it's locating
        private bool isLocating;
        private WayspotLocalizationState _wayspotLocalizationState = WayspotLocalizationState.Undefined;
        
        public bool isInitialized;
        public bool IsInitialized()
        {
            return isInitialized;
        }

        public bool IsIntegrated
        {
            get
            {
                return Instance.executionMode == ExecutionMode.Integrated;
            }
        }

        private string SelectedMapId
        {
            get
            {
                // #placenote2lightship
                //return selectedMapInfo != null ? selectedMapInfo.placeId : null;
                return null;
            }
        }

        [HideInInspector] public PhotoVideoActivityForConsume currentChallengePhotoVideoActivity;

        [HideInInspector]
        private static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;
        
        //------------------------Mono-behaviour methods-------------------------
 
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                print($"Log tracking: {condition} | {stackTrace}");
                Log.Error($"{condition} | {stackTrace}");
            }
        }

        void Start()
        {
            print($"#debugStart: InteractionManager | device ID: {SystemInfo.deviceUniqueIdentifier}");
            if (SystemInfo.deviceUniqueIdentifier == "913A3F73-BC0D-497F-91A7-C04C61767CB2") // <-- put your Device ID here to test the stand-alone mode and not interfere with the normal process flow.
            {
                UIManager.Instance.SetUIMode(UIManager.UIMode.Entrance);   
            }
            nativeCall = NativeCall.Instance;
        }

        public void StartAsStandaloneApp(Action onInitialized)
        {
            print($"StartAsStandaloneApp start");

            //set UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Initializing);
            UIManager.Instance.UpdateSBContext();
            SBContextManager.Instance.isARCancelled = false;
            // Dev notes: do not remove this callback assignment. Handle the execution mode inside the callback.
            SBRestClient.Instance.OnMapActivitySubmission += OnMapActivitySubmission;
            SBRestClient.Instance.OnBreadcrumbSubmitted += OnBreadcrumbSubmitted;
            
            // wait for ar session to start tracking and for PlaceNote to initialize
            StartCoroutine(WaitForARSessionThenStart(() =>
            {
                //in standalone mode, the action after initialization is fixed, it just shows up the UI to the user.
                // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);
                onInitialized();
            }));
        }


        public void StartAsIntegratedModule(Action onInitialized) 
        {
            print("StartAsIntegratedModule STARTED.");
            EnableARSession();
        
            //prepare execution mode info
            Instance.executionMode = ExecutionMode.Integrated;
            // Dev notes: do not remove this callback assignment. Handle the execution mode inside the callback.
            SBRestClient.Instance.OnMapActivitySubmission += OnMapActivitySubmission;
            SBRestClient.Instance.OnKeywordsReceived += OnKeywordsReceived;
            SBContextManager.Instance.isARCancelled = false;
            
            // wait for ar session to start tracking and for the AR SDK to initialize
            StartCoroutine(WaitForARSessionThenStart(() =>
            {
                // print("StartAsIntegratedModule > onInitialized");
                //in integration mode, the action after initialization is customized.
                
                // We will now delay the user-tracking and VPS query. 
                // This process is now on-demand.
                //// We can optionally turn off VPS if we are in GPS or Marker-based mode.
                // VpsCoverageManager.Instance.ShowLocationMessage = false;
                // VpsCoverageManager.Instance.StartTrackingUserLocation();
                
                onInitialized();
                UIManager.Instance.UpdateSBContext();
            }));
        }

        private IEnumerator WaitForARSessionThenStart(Action onInitialized)
        {
            print($"WaitForARSessionThenStart: isInitialized={IsInitialized()} | isOffline={SBContextManager.Instance.context.isOffline}");

            var isWaiting = false;
            if (!isInitialized && !SBContextManager.Instance.context.isOffline) //initialize if not yet
            {
                 
                 
               // while (SocialBeeARDK.ARHelper.Instance.Session.currentTrackingMode != ARDKAR.ARSessionState.Running && !SBContextManager.Instance.isARCancelled)
               // {
               //     if (!isWaiting)
               //     {
               //         print($"ARSession.state={SocialBeeARDK.ARHelper.Instance.Session?.State}");
               //         isWaiting = true;
               //     }
               //     yield return null;
               // }
               yield return null;
                 
                Input.location.Start();
                 
                isInitialized = true;
                print("Initialization completed.");
            }
            else // start right away
            {
                // print("Initialization completed - GPS only.");
                // AR Session has started tracking here. Now start the session
              
                Input.location.Start();                
            }            
             
            // set offline status from top menu
            UIManager.Instance.ShowOfflineStatus(SBContextManager.Instance.context.isOffline);
            //hide progress indicator
            UIManager.Instance.HideLoader();
             
            if(!SBContextManager.Instance.context.startWithPhotoVideo) {
                UIManager.Instance.FadeOutInitCover();
            }
            
             //callback
             // #placenote2lightship
             // print(string.Format("PlacenoteListener number: \'{0}\'", LibPlacenote.Instance.GetListeners().Count));
            onInitialized();
        }

        // Update is called once per frame
        void Update()
        {
            // For Testing Purpose...
            if (Input.GetKeyDown(KeyCode.Space))
            {
                
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ShowNearbyVpsMapPanel);
                
            }
        }
        
        //----------------------------------------- PreScan/Scan/PostScan -----------------------------------------

        public void ShowNearbyVPS()
        {
            NearbyVPSManager.Instance.OnWayspotsFound += OnWayspotsFound;
            NearbyVPSManager.Instance.OnGpsOptionSelected += OnGpsOptionSelected;
            NearbyVPSManager.Instance.OnWayspotSelected += OnWayspotSelected;
            NearbyVPSManager.Instance.OnNavigateToSelectedWayspot += OnNavigateToSelectedWayspot;
            ActivityUIFacade.Instance.isSelectedTypeOfExperience = false;
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ShowNearbyVpsMapPanel);
            NearbyVPSManager.Instance.SearchWayspots();
            Debug.Log("Show NearByVPS");
        }

        public void AskIndoorOrOutdoor()
        {
            ActivityUIFacade.Instance.isSelectedTypeOfExperience = false;
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AskIndoorOrOutdoor);
        }
        
        /// <summary>
        /// For GPS activities.
        /// </summary>
        public void OnNewActivityClicked()
        {
            // print("InteractionManager.OnNewActivityClicked...");
            if (!IsInitialized() && !SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
               return;
            }

            //if the there are any active bottom UI, slide down first.
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                //start placing reticle
                // #placenote2lightship
                //ReticleController.Instance.StartReticle();
                ARCursorRenderer.Instance.StartCursor();
                
                //update UI mode
                UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.MovingReticle);
                
                // #placenote2lightship - we don't need this in ARDK?
                // //configure plane manager & set planes visible
                // PlaneManager.Instance.SetARPlanesVisible(true);
                // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.Hexagon);
                // if(SBContextManager.Instance.context.isPlanning)
                //     PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.Horizontal);
                // else
                //     PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.All);
                // END. --------------------------------------------------------------------------------

                //hide points-bar and minimap
                PointsBarManager.Instance.HidePointsBar();
                MiniMapManager.Instance.HideMiniMap();
                
                //show GPS anchors from other maps
                //GPSAnchorManager.Instance.ShowGPSAnchorsDummy();
                GPSAnchorManager.Instance.ShowGPSAnchors();
            });
            
            ActiveAnchorManager.HideActivities();
        }

        /// <summary>
        /// For VPS activities.
        /// </summary>
        public void OnNewVpsActivityClicked()
        {
            if (WayspotAnchorManager.Instance.IsLocalized)
            {
                OnWayspotLocalized();
                return;
            }
            
            //if the there are any active bottom UI, slide down first.
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.PreLocalization);
                if (VpsCoverageManager.Instance.ClosestWayspot != null)
                {
                    SBThumbnailSelector.Instance.LoadInitialThumbnail(VpsCoverageManager.Instance.ClosestWayspot.Value.ImageURL);
                }
                // We don't need to start localization here yet.
                // The PrelLocalization event will handle showing the CTA
                // that will allow the user to trigger the localization. 
            });
            
            // ActiveAnchorManager.HideActivities();
        }
        
        /// <summary>
        /// Kicks off the activity management (CRUD) or consumption. This is where VPS discovery and localization happens.
        /// </summary>
        /// <returns></returns>
        public void StartMarkerBasedActivities()
        {
            if (!IsInitialized() && !SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
                return;
            }

            BottomPanelManager.Instance.ShowMessagePanel("Searching for nearby Wayspots...", autoClose: false);
            ActiveAnchorManager.HideActivities();
            VpsCoverageManager.Instance.StartTrackingUserLocation();
        }
        
        public void OnStartScanClicked()
        {
            //start progress info panel
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.StartScan);
            ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetUIMode(AnchorController.UIMode.Creating_Scanning);
            ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetBehaviourMode(AnchorController.BehaviourMode.Creating_Scanning);

            //start scanning with placenote
            DoStartScanning();
        }


        public void OnCancelStartScanClicked()
        {
            print("Cancel start scanning.");
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.CancelStartScan);
            ActivityUIFacade.Instance.ShowAnchorMoveAndLockGuidance();
        }


        public void OnAbortScanClicked() {
            print("Abort scanning.");
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AbortScan);

            //enable detected planes
            // PlaneManager.Instance.SetARPlanesVisible(true);
            // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.Hexagon);
            // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.All);

            //// StopMapping();
            // PauseMapping();

            //set the last map size, so that the 200/300 wll be re-calculated from the start
            // #placenote2lightship
            // this.lastMapSize = LibPlacenote.Instance.GetMap().Length;
            this.lastMapSize = 0;

            //reset anchor status
            BottomPanelManager.Instance.HideCurrentPanel(() => {
                var currAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
                var controller = currAnchorObj.GetComponent<AnchorController>();

                controller.SetUIMode(AnchorController.UIMode.Creating_Spawned);
                controller.SetBehaviourMode(AnchorController.BehaviourMode.Creating_Spawned);
                controller.lockerPlate.SetActive(true);
                controller.LockAnchorWithAnimation(false);

                mapQualityThresholdCrossed = true;
            });
        }


        private void DoStartScanning()
        {
            //disable detected planes
            // PlaneManager.Instance.SetARPlanesVisible(false);
            // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.None);
            // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);
            ARHelper.Instance.StopPlaneDetection();

            //reset progress status
            ActivityUIFacade.Instance.ResetMappingProgress();
            mapQualityThresholdCrossed = false;
            currentAnchorScanningProgress = 0f;

            // Enable point-cloud
            // #placenote2lightship
            // FeaturesVisualizer.EnablePointcloud(Const.FEATURE_POINT_WEAK, Const.FEATURE_POINT_STRONG);

            isScanningComplete = false;
            if (isNewCollection)
            {
                // #placenote2lightship
                // LibPlacenote.Instance.StartSession();
                print("New mapping started");
            }
            else
            {
                // #placenote2lightship
                // LibPlacenote.Instance.RestartSendingFrames(); //resume passing camera frames to PlaceNote
                print("Mapping resumed");
            }

            //start thumbnail capturing
            ThumbnailManager.Instance.StartCapturing();
        }


        // public void OnPostScanConfirmed()
        // {
        //     //switch anchor BehaviourMode
        //     ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>().SetBehaviourMode(AnchorController.BehaviourMode.Creating_SettingActivities);
        //     BottomPanelManager.Instance.HideCurrentPanel();
        // }


        public void OnDeletingAnchor()
        {
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            if (currentAnchorObj)
            {
                currentAnchorObj.GetComponent<AnchorController>().DoDeleteAnchor();
            }
            //BottomPanelManager.Instance.HideCurrentPanel();
        }


		public void OnDeleteActivity()
        {
            print("OnDeleteActivity - GetComponent<AnchorController>()");

            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            AnchorController anchorController = currentAnchorObj.GetComponent<AnchorController>();
            //AnchorController anchorController = ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>();

            // print("OnDeleteActivity - GetActivityManager().GetActiveActivity()");
            GameObject activeActivityObj = anchorController.GetActivityManager().GetActiveActivity();
            if (activeActivityObj)
            {
                // print("OnDeleteActivity - GetComponent<SBActivity>()");
                var activityToDelete = activeActivityObj.GetComponent<SBActivity>();
                // print($"OnDeleteActivity: activityToDelete is null? {activityToDelete==null}");
                activityToDelete.DoDelete();
            }
        }


        //---------------------------------------- Photo/Video mode ---------------------------------------

        public void OnPhotoVideoCameraOpen()
        {
            print("Switching to plugin's camera view");
            UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);

            //Todo: launching NatCoder
            //...
        }


        public void OnPhotoTaken(string contentPath)
        {
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            if (currentAnchorObj)
            {
                currentAnchorObj.GetComponentInChildren<AnchorController>().OnPhotoTaken(contentPath);
                UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
            }
        }

        public void OnVideoTaken()
        {
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            if (currentAnchorObj)
            {
                currentAnchorObj.GetComponentInChildren<AnchorController>().OnVideoTaken();
                UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
            }
        }
        
        public void BeforeOnVideoTaken()
        {
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            if (currentAnchorObj)
            {
                currentAnchorObj.GetComponentInChildren<AnchorController>().BeforeOnVideoTaken();
                UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
            }
        }

        /// <summary>
        /// Triggers a call to the native app to get the location information based on the latitude and longitude.
        /// </summary>
        /// <param name="latitude">The latitude of the location.</param>
        /// <param name="longitude">The longitude of the location.</param>
        public void OnWillGetLocationInfo(double latitude, double longitude)
        {            
            print("OnWillGetLocationInfo");
            nativeCall.OnWillGetLocationInfo(latitude, longitude);            
        }

        /// <summary>
        /// Triggers a call to the native app to get the keywords from the image @ contentPath.
        /// </summary>
        /// <param name="contentPath"></param>
        public void OnWillGetImageKeywords(string contentPath, bool getAll)
        {
            // ToDo: make the necessary changes on the UI here.
            // The Unity app will call the native app to get the keywords from the image.
            print("OnWillGetImageKeywords");
            //nativeCall.OnWillGetImageKeywords(contentPath, getAll);
            SBRestClient.Instance.GetImageKeywords(contentPath);            
        }

        /// <summary>
        /// The callback for <see cref="OnWillGetImageKeywords(string)"/>.
        /// </summary>
        public void OnKeywordsGenerated(string keywords)
        {
            print("OnKeywordsGenerated");
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            if (SBContextManager.Instance.context.IsConsuming()) {
                currentChallengePhotoVideoActivity.ShowKeywords(keywords);
            }
            else if (currentAnchorObj)
            {
                currentAnchorObj.GetComponentInChildren<AnchorController>().OnKeywordsGenerated(keywords);

                // Do we need this SetUIMode here?
                UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
            }
            else {
                OnBoardManager.Instance.GetKeywords(keywords);
            }
        }

        /// <summary>
        /// Button on alert panel(wrong challenge keywords), user will take new photo
        /// </summary>
        public void OnRetakeChallenge() {
            print("OnRetakeChallenge");
            if (SBContextManager.Instance.context.IsConsuming()) {
                print("-=- InteractionManager OnRetakeChallenge()");
                BottomPanelManager.Instance.HideCurrentPanel();
                UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
                RecordManager.Instance.PhotoTakeOnly();
                // currentChallengePhotoVideoActivity.ShowChallengeLoader(true);
            }
            else {
                BottomPanelManager.Instance.HideCurrentPanel();
                OnBoardManager.Instance.Retake();
            }
        }
        
        /// <summary>
        /// Button on alert panel(wrong challenge keywords), user won't take photo
        /// </summary>
        public void OnTakeMyLossChallenge() {
            print("-=- InteractionManager OnTakeMyLossChallenge()");
            BottomPanelManager.Instance.HideCurrentPanel();
            
            if (SBContextManager.Instance.context.IsConsuming()) {
                currentChallengePhotoVideoActivity.SubmitPhotoChallenge(true);
                // PhotoVideoActivityForConsume[] photoVideoConsumes =
                //     ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>()
                //         .GetComponentsInChildren<PhotoVideoActivityForConsume>();
                //
                // foreach (var photoVideoConsume in photoVideoConsumes) {
                //     print("-=- InteractionManager OnTakeMyLossChallenge foreach (var photoVideoConsume in photoVideoConsumes)");
                //     if (photoVideoConsume.isActivePanel) {
                //         print("-=- InteractionManager OnTakeMyLossChallenge photoVideoConsume.isActivePanel");
                //         photoVideoConsume.SubmitPhotoChallenge(true);
                //     }
                // }
            }

            // if it is OnBoard
            else {
                OnBoardManager.Instance.WrongPhoto();
            }
            // GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            // if (currentAnchorObj) {
            //     currentAnchorObj.GetComponentInChildren<PhotoVideoActivityForConsume>().SubmitPhotoChallenge(true);
            // }
        }

        /// <summary>
        /// Use this to call quick photo/video before scanning next anchor
        /// </summary>
        public void OnQuickPhotoTake() {
            UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
            RecordManager.Instance.ClearPaths();
            RecordManager.Instance.startWithPhotoVideo = true;
            RecordManager.Instance.HideARContent();
            
            ActiveAnchorManager.HideActivities();
        }

        public void ShowVideoThumbnail(Texture2D tex) {
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            if (currentAnchorObj != null)
            {
                currentAnchorObj.GetComponentInChildren<AnchorController>().ShowVideoThumbnail(tex);
            }

            if (currentChallengePhotoVideoActivity != null) {
                currentChallengePhotoVideoActivity.ShowThumbnail(tex);
            }
        }
        

        //---------------------------------------- Back to native ---------------------------------------

        public void OnBackToNativeClicked()
        {
            if (RecordManager.Instance.isFullscreen) {
                RecordManager.Instance.ExitFullscreen();
                return;
            }

            if (!SBContextManager.Instance.context.IsConsuming()) //if it's creator
            {
                if (ActiveAnchorManager.IsReadyToSaveMap())
                {
                    if (SBContextManager.Instance.context.isEditing)
                    {
                        // Fail-safe, in case we ended up here while editing activities.                        
                        // No need to ask anymore, just auto-save and go back to native when done.
                        DoBackToNative(true, false);
                    }
                    else
                    {
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.UserTooFarAway);
                        // BottomPanelManager.Instance.UpdateMessage("Are you done creating activities?");
                        //// No need to ask anymore, just auto-save and go back to native when done.
                        //OnSaveAndQuitARClicked();
                    }                    
                }
                else
                {
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.BackToNative);
                    BottomPanelManager.Instance.UpdateMessage("Are you done creating activities?");
                }
            }
            else //if it's consumer
            {
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.BackToNative);
                BottomPanelManager.Instance.UpdateMessage("Are you sure you are done consuming activities?");
            }
        }


        public void DoBackToNative(bool quitARSession = true) //for Unity editor to call!
        {
            DoBackToNative(true, false);
        }

        public void DoBackToNative(bool quitARSession = true, bool deleteNewActivities = false)
        {
            print("InteractionManager.OnBackToNativeClicked: started");
            
            //close all open message panel and go back to native map view
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                // print("InteractionManager.DoBackToNative > HideCurrentPanel > postAction...");
                // #placenote2lightship
                // LibPlacenote.Instance.RemoveListener(this);
            
                //clear created anchor objects
                // Effectively, we can just do a "ActiveAnchorManager.ClearAnchors()"
                
                AnchorManager.Instance.ClearAnchors();
                WayspotAnchorManager.Instance.ClearAnchors();
            
                //clear GPS anchors
                GPSAnchorManager.Instance.ClearAnchors();
                GPSAnchorManager.Instance.ClearGPSMarkers();
                
                ARHelper.Instance.StopPlaneDetection();
                //quit PlaceNote session
                if (quitARSession)
                {
                    //stop placenote session
                    // #placenote2lightship
                    // LibPlacenote.Instance.StopSession();
                    QuiARSession();
                }

                //reset the flag for showing thumbnail
                ActivityUIFacade.Instance.canShowThumbnail = false;
                ActivityUIFacade.Instance.thumbnailParent.SetActive(false);
                
                //reset phone pose detection
                PhonePoseManager.Instance.EnablePoseWarning(false);
                PhonePoseManager.Instance.ResetAlertPanels();
                
                //Reset plane detection to disabled
                // PlaneManager.Instance.SetARPlanesVisible(false);
                // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.None);
                // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);

                //clear visualized point-cloud
                // #placenote2lightship
                // FeaturesVisualizer.ClearPointcloud();

                //clear reticle (if existing)
                //ReticleController.Instance.StopReticle();
                ARCursorRenderer.Instance.StopCursor();
                
                // --- when we call ActivityUIFacade in quick photo, we get a NullReferenceException
                if(!RecordManager.Instance.startWithPhotoVideo) {
                    //reset next/save buttons as they are not managed by BottomPanelManager
                    ActivityUIFacade.Instance.SetNextOrSaveButtonsVisible(false, false, false);
                    
                    //reset animation (animation get error in quick photo/video case)
                    ActivityUIFacade.Instance.EnableTapToPlaceAnimation(false);
                    ActivityUIFacade.Instance.EnableMoveDeviceAnimation(false);
                    
                    //hide off-screen indicator
                    OffScreenIndicatorManager.Instance.HideArrow();
                }

                //critical: reset some flags!
                this.lastMapSize = 0; //reset the map size based on the last scanned anchor.
                isNewCollection = true; //reset the flag for creating collection

                //reset some debug info (optional)
                MessageManager.Instance.UpdateMapSize(0);
                MessageManager.Instance.UpdateCurrentAnchorSize(0);
            
                //back to native
                if (deleteNewActivities)
                {
                    // print("InteractionManager.OnBackToNativeClicked, deleteNewActivities = true");
                    nativeCall.OnDeleteMultiple(string.Join(",", SBContextManager.Instance.context.NewActivities));
                }
                
                MiniMapManager.Instance.CleanMiniMap();
                SBContextManager.Instance.isARCancelled = true;
                RemoveDelegates();
                // DestroyARSession();
                nativeCall.EndAR();
            });
        }

        public void OnDropAndQuitARClicked()
        {
            ActivityUIFacade.Instance.SetNextOrSaveButtonsVisible(false, false, false);
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                DoBackToNative(true, true);
            });
        }


        public void OnSaveAndQuitARClicked()
        {
            ActivityUIFacade.Instance.SetNextOrSaveButtonsVisible(false, false, false);
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                // if(!SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
                //     // #placenote2lightship
                //     // if (LibPlacenote.Instance.GetStatus() == LibPlacenote.MappingStatus.RUNNING) {
                //     //     OnAbortScanClicked();
                //     // }
                //
                //     ActiveAnchorManager.CompleteAllAnchors(() => { SaveMap(); });
                // }
                // else {
                //     FinishGPSOnlyCreation();
                // }
                // // SaveMap();
                FinishGPSOnlyCreation();
            });
        }


        public void OnTooFarAlertCloseClicked()
        {
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                ActiveAnchorManager.isAlerted = true;
            });
        }


        //Yes this is needed but this can be done through the post-callback of HideCurrentPanel
        // public void BackToNativeAfterHideCurrentPanel() {
        //     StartCoroutine(BackToNativeAfterHideCurrentPanel(Const.PANEL_ANIMATION_TIME + 0.3f));
        // }
        //
        // IEnumerator BackToNativeAfterHideCurrentPanel(float time) {
        //     print($"Time for delay = {time}");
        //     yield return new WaitForSeconds(time);
        //     print("Going back to native...");
        //     DoBackToNative(true);
        // }
        

        //----------------------------------------- Save map -----------------------------------------
        
        public void FinishGPSOnlyCreation()
        {
            PhonePoseManager.Instance.EnablePoseWarning(false);
            //MessageManager.Instance.ClearDebug();

            if (SBContextManager.Instance.context.isOffline) {
                print($"FinishGPSOnlyCreation() isOffline = true");
                
                PointsBarManager.Instance.HidePointsBar();
                MiniMapManager.Instance.HideMiniMap();

                ActiveAnchorManager.ClearAnchors();
                
                this.lastMapSize = 0;
                isNewCollection = true;
                
                DoBackToNative(true, false);
                return;
            }
            
            BottomPanelManager.Instance.UpdateMessage("Finalizing...");
            print("Publishing added (own) activities, waiting for response from the SB server...");
            this.StartThrowingCoroutine(SBRestClient.Instance.PublishAddedActivitiesInConsumeEnumerator(SBContextManager.Instance.context.experienceId, (response) =>
            {
                print($"PublishAddedActivitiesInConsumeEnumerator response: {response}");

                PointsBarManager.Instance.HidePointsBar();
                MiniMapManager.Instance.HideMiniMap();

                ActiveAnchorManager.ClearAnchors();
                
                this.lastMapSize = 0;
                isNewCollection = true;

                DoBackToNative(true, false);
            }), ex =>
            {
                print($"ErrorHandler > callback - PublishAddedActivitiesInConsumeEnumerator: {ex.StackTrace}");
                BottomPanelManager.Instance.ShowMessagePanel($"There's an issue finalizing your activities. Try to finish again.", true, true,
                    (() => {
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextOrCompleteGPSOnlyCreation);
                    }));
            });
             
        }
        
        public void BackToNativeAfterSavingMap()
        {
            //slide down panel and back to native
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                OnMapSavedSuccessful(lastSavedMapId);
            });
        }


        //---------------------------------------- Locate ------------------------------------------------------


        public void LoadSelectedMap()
        {
            print("LoadMap 1");
            LoadMap(SelectedMapId);
        }


        public void ReloadMap()
        {
            print("LoadMap 2");
            LoadMap(lastSuccLoadedMapId);
        }

        
        private string newMapId;
        public void LoadNewMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                newMapId = lastSuccLoadedMapId;
            }
            else
            {
                newMapId = mapId;    
            }

            //popup confirm dialog
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ConfirmToLoadNewMap);
        }

        public void DoLoadNewMap()
        {
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                //stop placenote session
                // #placenote2lightship END
                // LibPlacenote.Instance.StopSession();
                
                //clear created anchor objects
                ActiveAnchorManager.ClearAnchors();

                //clear GPS anchors
                GPSAnchorManager.Instance.ClearAnchors();

                // //load the last map
                // ReloadMap();
                LoadMap(newMapId);
            });
        }
        
        private string lastSuccLoadedMapId;
        /// <summary>
        /// Load the activities, used in editing or consuming. 
        /// </summary>
        /// <param name="mapId"></param>
        public void LoadMap(string mapId)
        {
            print($"InteractionManager.LoadMap... mapId={mapId}");
            //if (!IsInitialized())
            //{
            
            var context = SBContextManager.Instance.context;
            if (context != null)
            {
                Debug.Log($"context.isPlanning = {context.isPlanning}");
                Debug.Log($"context.plannedLocation = {context.plannedLocation}");
                
                if(context.plannedLocation != null)
                    Debug.Log($"The thumbail for the Google Map Location panel: {SBContextManager.Instance.context.plannedLocation.Thumbnail}");
            }
            
            switch (context)
            {
                case { isPlanning: true }:
                    OnEditPlanning();
                    return;
                case { isCreatingGPSOnlyAnchors: true }:
                    OnEditGPSOnly();
                    return;
            }

            // If we reach this point then our AR mode is for VPS.
            if (SBContextManager.Instance.IsEditCreating() && context is { IsVPSPlacementType: true })
            {
                StartPreLocalization(context.initialThumbnail);
            }

            // ---------------------------------------------------------------------------------------------------------------------------------------
            // If we are in VPS mode then we will wait for the handler "OnWayspotsFound" to receive an event.
            // In that handler, a UI is presented to the user and wait for the action if localization should start.
            // If the user chooses to localize, then the loading of content will happen in "OnWayspotLocalized".
            // ---------------------------------------------------------------------------------------------------------------------------------------
            
            //return;
            //}

            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
            
            // This PreLocalization is not needed here because the VPS coverage is triggered inside "StartAsIntegratedModule".
            // This PreLocalization methods will be called inside the handler "OnWayspotsFound".
            // ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.PreLocalization);
            // if (VpsCoverageManager.Instance.ClosestWayspot != null)
            // {
            //     SBThumbnailSelector.Instance.LoadInitialThumbnail(VpsCoverageManager.Instance.ClosestWayspot.Value.ImageURL);
            // }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="altText">The alternative text to show on the panel.</param>
        public void Localize(string altText)
        {
            //update UI
            // if (VpsCoverageManager.Instance.ShowLocationMessage)
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.Localizing, altText: altText);
            print("Session started in Locating mode");

            //start placenote localization session
            // #placenote2lightship END
            // LibPlacenote.Instance.StartSession();
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated -= OnAnchorLocalizationStateUpdated;
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated += OnAnchorLocalizationStateUpdated;
            WayspotAnchorManager.Instance.StartLocalization();
            isLocating = true;
        }

        public void RetryLocalization()
        {
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.Localizing);
            WayspotAnchorManager.Instance.RetryLocalization();
            isLocating = true;
        }
        
        #region Helper Methods
 
        /// <summary>
        /// Used to respawn the anchors from the server.
        /// </summary>
        /// <param name="reticlePosition"></param>
        public void LoadContent(Vector3 reticlePosition)
        {
            lastMapSize = 0;
            print("Localized, loading virtual content...");

            //organize anchors.
            SBContextManager.Instance.context.SetMapAnchors();
            SBContextManager.Instance.context.SetOtherAnchors();
                
            //reborn anchor objects from the SB server.
            var anchorList = SBContextManager.Instance.context.ToAnchorInfoList(reticlePosition);                
            ActiveAnchorManager.LoadAnchors(anchorList);
            print($"Content loaded from the API. plannedLocation is nil? {SBContextManager.Instance.context.plannedLocation == null} | anchors={anchorList.mapContent.Length} | otherAnchors={SBContextManager.Instance.context.OtherAnchors.Count()}");
        }

        #endregion
        
        
        //-----------------------------InitPanel-------------------------------


        public void OnSearchMapsClick()
        {
            MessageManager.Instance.ShowMessage("Searching for saved maps");

            //TODO: later we can use this location info for querying the related maps.
            LocationInfo locationInfo = Input.location.lastData;
 
            // #placenote2lightship BEGIN
            // LibPlacenote.Instance.SearchMaps(Const.MAP_PREFIX, (mapInfoList) =>
            // {
            //     foreach (Transform t in SelectMapFacade.Instance.mapListContentParent.transform)
            //     {
            //         Destroy(t.gameObject);
            //     }
            //
            //     print(string.Format("Found {0} maps.", mapInfoList.Length));
            //     if (mapInfoList.Length == 0)
            //     {
            //         MessageManager.Instance.ShowMessage("No maps found. Create a map first!");
            //         return;
            //     }
            //
            //     // Render the map list!
            //     foreach (LibPlacenote.MapInfo mapInfo in mapInfoList)
            //     {
            //         if (mapInfo.metadata.userdata != null)
            //         {
            //             Debug.Log(mapInfo.metadata.userdata.ToString(Formatting.None));
            //         }
            //
            //         AddMapToList(mapInfo);
            //     }
            //
            //     MessageManager.Instance.ShowMessage("Please select a map to load");
            //
            //     //update UI mode
            //     UIManager.Instance.SetUIMode(UIManager.UIMode.SelectMap);
            //     SelectMapFacade.Instance.SetUIMode(SelectMapFacade.UIMode.SelectMap_NoMapSelected);
            //
            // });
            // #placenote2lightship END
        }

        //-----------------------------SelectMapPanel-----------------------------


        public void OnCancelSelectingMapClick()
        {
            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);
        }


        public void OnDeleteMapClick()
        {
            if (!IsInitialized()) return;

            MessageManager.Instance.ShowMessage(string.Format("Deleting Map ID: '{0}'", SelectedMapId));
            // #placenote2lightship BEGIN
            // LibPlacenote.Instance.DeleteMap(SelectedMapId, (deleted, errMsg) =>
            // {
            //     if (deleted)
            //     {
            //         //update UI mode
            //         UIManager.Instance.SetUIMode(UIManager.UIMode.SelectMap);
            //         SelectMapFacade.Instance.SetUIMode(SelectMapFacade.UIMode.SelectMap_MapDeleted);
            //
            //         MessageManager.Instance.ShowMessage(string.Format("Deleted map '{0}'", SelectedMapId));
            //         OnSearchMapsClick();
            //     }
            //     else
            //     {
            //         MessageManager.Instance.ShowMessage(string.Format("Failed to delete map '{0}'", SelectedMapId));
            //     }
            // });
            // #placenote2lightship END
        }


        /// <summary>
        /// When the whole anchor is concluded
        /// </summary>
        // public void OnConfirmActivitySettingClick()
        // {
        //     AnchorController anchorController = AnchorManager.Instance.GetCurrentAnchorObject().GetComponent<AnchorController>();
        //     anchorController.SetUIMode(AnchorController.UIMode.Creating_Ready);
        //     anchorController.SetBehaviourMode(AnchorBehaviourMode.Scanning);
        //
        //     switch (executionMode)
        //     {
        //         case ExecutionMode.Integrated:
        //             AnchorManager.Instance.StartEditingCurrentAnchor();
        //             break;
        //
        //         case ExecutionMode.Standalone:
        //             AnchorManager.Instance.StartEditingCurrentAnchor(); //start editing activity name
        //             break;
        //     }
        // }


        //----------------------------MappingPanel-----------------------------


        // public void OnStartScanningClick()
        // {
        //     //start progress info panel
        //     UIManager.Instance.SetUIMode(UIManager.UIMode.Mapping);
        //     MappingFacade.Instance.SetUIMode(MappingFacade.UIMode.Mapping_Scanning);
        //     AnchorManager.Instance.GetCurrentAnchorObject().GetComponent<AnchorController>().SetUIMode(ContentFacade.UIMode.Creator_Scanning);
        //
        //     StartMapping();
        // }



        //------------------------------LocalizationPanel--------------------------------


        public void OnExitMapClick()
        {
            MessageManager.Instance.ShowMessage("Session was reset. You can start new map or load your map again.");

            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
            // InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);

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
            // JObject notesList = AnchorManager.Instance.AnchorInfoListToJSON();
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
        
        /// <summary>
        /// The callback after saving the cloud map.
        /// </summary>
        /// <param name="mapId">The Id of the cloud map.</param>
        /// <param name="activityIds">The list of Id of the activities created in the cloud map.</param>
        /// <param name="error">The error encountered when saving the map.</param>
        void OnMapActivitySubmission(string mapId, IEnumerable<string> activityIds, ErrorInfo error)
        {
            print($"InteractionManager.OnMapActivitySubmission started w/ mode: {Instance.executionMode}");

            if (!IsIntegrated)
            {
                print($"InteractionManager.OnMapActivitySubmission: skipping entire method.");
                return;
            }

            if (error != null) // Then the mapID was not linked to the activities.
            {
                print($"InteractionManager > Error attaching the activities to the map: {error}");
                BottomPanelManager.Instance.UpdateMessage("Your activities were not completely saved.");
            }
            else
            {
                print($"InteractionManager > The mapId was successfully linked to the activities.");
                nativeCall.OnWillUpdateActivitiesMapId(mapId, activityIds == null ? new string[] { }  : activityIds.ToArray());

                DoBackToNative(true, false);
            }
        }

        /// <summary>
        /// The callback after saving breadcrumb for a cloud map.
        /// </summary>
        /// <param name="error">The error encountered when saving the map.</param>
        void OnBreadcrumbSubmitted(ErrorInfo error)
        {
            print($"InteractionManager.OnBreadcrumbSubmitted started w/ mode: {Instance.executionMode}");

            if (!Instance.IsIntegrated)
            {
                print($"InteractionManager.OnBreadcrumbSubmitted: skipping entire method.");
                return;
            }

            if (error != null)
            {
                print($"InteractionManager > Error attaching the activities to the map: {error}");
                // Then the breadcrumb was not submitted.
                // ToDo: Use the "error" object to notify the user what happened.
                //      Show a modal/panel or whatever appropriate UI element
                //      that will allow the user to "retry".

            }
            else
            {
                print($"InteractionManager > the breadcrumb was successlly saved.");

                //ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.);
            }
        }
        
        private void OnWayspotSelected(LocalizationTarget location)
        {
            OnWayspotTakeAction(location, ActivityUIFacade.UIEvent.PreLocalization);
        }

        private void OnNavigateToSelectedWayspot(LocalizationTarget location)
        {
            OnWayspotTakeAction(location, ActivityUIFacade.UIEvent.NavigateBeforeLocalization);
        }

        private void OnWayspotTakeAction(LocalizationTarget location, ActivityUIFacade.UIEvent uiEvent)
        {
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                PreLocalizationHelper(uiEvent,location, "");
                
                MapBoxManager.Instance.ApplyCameraSetting(25.0f, 1, 0.4f, false);
                MapBoxManager.Instance.SetMapView(MapView.ARViewMap, uiEvent == ActivityUIFacade.UIEvent.NavigateBeforeLocalization);
            });
        }

        public void StartPreLocalization(string locationThumbnailURL)
        {
            PreLocalizationHelper(ActivityUIFacade.UIEvent.PreLocalization, null, locationThumbnailURL);
        }
        
        private void PreLocalizationHelper(ActivityUIFacade.UIEvent uiEvent, LocalizationTarget? location, string locationThumbnailURL)
        {
            if (location == null && locationThumbnailURL.IsNullOrWhiteSpace())
                throw new ArgumentException("location and locationThumbnail cannot be both null");
            
            if (WayspotAnchorManager.Instance.LocalizationState == TrackingState.Tracking)
            {
                OnWayspotLocalized();
                return;
            }
                
            ActivityUIFacade.Instance.FireUIEvent(uiEvent);
            if (location != null)
            {
                SBThumbnailSelector.Instance.LoadInitialThumbnail(location?.ImageURL);
                NearbyVPSManager.Instance.selectedWayspot = location;    
            }
            else
            {
                SBThumbnailSelector.Instance.LoadInitialThumbnail(locationThumbnailURL);
            }

            // var userLocation = $"{SBContextManager.Instance.lastKnownLocation.Latitude},{SBContextManager.Instance.lastKnownLocation.Longitude}";
            // MapBoxManager.Instance.PlaceUserMarkerOnMap(userLocation);
        }
        
        private void OnGpsOptionSelected()
        {
            // ToDo: hide the Nearby VPS screen here
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                SBContextManager.Instance.UpdateIsCreatingGPSOnlyAnchors(true);    
                Instance.OnNewActivityClicked();
            });
        }
        
        void OnWayspotsFound(IReadOnlyDictionary<string, LocalizationTarget> targets, ErrorInfo error)
        {
            if (error != null)
            {
                print($"OnLocalizationTargetsFound has error: {error.Title} | {error.Message}");
                // BottomPanelManager.Instance.ShowMessagePanel(error.Message);
                ARHelper.Instance.StopPlaneDetection();
                return;
            }
            // Cannot stop LocationService here because we need it in WayspotAnchorManager.
            // VpsCoverageManager.Instance.StopTrackingUserLocation();
            VpsCoverageManager.Instance.PauseTrackingUserLocation();
            
            print($"OnLocalizationTargetsFound > HasLocalicazationStarted={WayspotAnchorManager.Instance.HasLocalicazationStarted}");
            if (WayspotAnchorManager.Instance.HasLocalicazationStarted) return;
            
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated -= OnAnchorLocalizationStateUpdated;
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated += OnAnchorLocalizationStateUpdated;
            
            print($"mapID={SBContextManager.Instance.context.mapId} | isCreatingGPSOnlyAnchors={SBContextManager.Instance.context.isCreatingGPSOnlyAnchors} | has ClosestWayspot={VpsCoverageManager.Instance.ClosestWayspot!=null}");
            
            // No longer valid --------------.
            // Do not localize yet. We are using this callback to check
            // if there are Waypost locations near the user.
            // If so, then we will enable the VPS option on the UI.
            // We will call "StartLocalization" when the user chooses to create activities with VPS. 
            // WayspotAnchorManager.Instance.StartLocalization();
            // END No longer valid ----------.
            
            if (!SBContextManager.Instance.IsEditCreating() && !SBContextManager.Instance.IsConsuming())
            {
                print("Enabling VPS as we are not editing nor consuming.");
                //
                // Any UI flow is now handled in the NearbyVPSManager script.
                // // Enable the VPS button when we are NOT editing nor consuming.
                // ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.EnableVps);
            }
            else if (VpsCoverageManager.Instance.ClosestWayspot != null
                     && !SBContextManager.Instance.context.mapId.IsNullOrWhiteSpace()
                     && !SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
            {
                if (WayspotAnchorManager.Instance.LocalizationState == TrackingState.Tracking)
                {
                    OnWayspotLocalized();
                    return;
                }
                
                print("Loading thumbnail for VPS localization...");
                // ToDo: disabled for now.... Let's see how the new flow works.
                // // At this point we know we are either editing in VPS mode or consuming.
                // ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.PreLocalization);
                // if (VpsCoverageManager.Instance.ClosestWayspot != null)
                // {
                //     SBThumbnailSelector.Instance.LoadInitialThumbnail(VpsCoverageManager.Instance.ClosestWayspot.Value.ImageURL);
                // }
                // END ToDo.
            }
        }

        public void RefreshAnchorServiceState(WayspotLocalizationState state)
        {
            _wayspotLocalizationState = state;
            if (WayspotAnchorManager.Instance.HasLocalicazationStarted) return;
            
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated -= OnAnchorLocalizationStateUpdated;
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated += OnAnchorLocalizationStateUpdated;
        }
        
        private void OnAnchorLocalizationStateUpdated(ARPersistentAnchorStateChangedEventArgs state)
        {
            print($"OnAnchorLocalizationStateUpdated state.arPersistentAnchor.trackingState = {state.arPersistentAnchor.trackingState}");
            print($"OnAnchorLocalizationStateUpdated state.arPersistentAnchor.trackingStateReason = {state.arPersistentAnchor.trackingStateReason}");

            var newState = (WayspotLocalizationState)(int)state.arPersistentAnchor.trackingState;
            
            if (_wayspotLocalizationState == newState) return;

            // print($">>> localization new state: {newState} | old state: {_wayspotLocalizationState} #localization");
            if (_wayspotLocalizationState == WayspotLocalizationState.Localized)
            {
                // If the user has been localized already,
                // then let's not do any action for now.
                // We will handle the scenario later
                // where the user will save the activities
                // but is not localized.
                return;
            }

            print("setting status of _wayspotLocalizationState --1 #wayspotLocalizationState_SET_STATE #localization");
            _wayspotLocalizationState = newState;
            
            switch (state.arPersistentAnchor.trackingState)
            {
                case TrackingState.None:
                    // Do nothing.
                    return;
                case TrackingState.Limited:
                    // Do nothing.
                    return;
                // case TrackingState.Tracking:
                //     print("Localizing: Go and point your phone at the area shown in the thumbnail. #localization");
                //     Localize("Localizing: Go and point your phone at the area shown in the thumbnail.");
                //     return;
                // case LocalizationState.Initializing:
                //     if (VpsCoverageManager.Instance.ShowLocationMessage)
                //         BottomPanelManager.Instance.ShowMessagePanel($"Initializing...", autoClose: false);
                //     print($" *** --- localization new state: {newState} | state: {state} #localization");
                //     return;
                case TrackingState.Tracking:
                    print($"Localization was successful! [*** localization new state: {newState} | state: {state} #localization]");
                    OnWayspotLocalized();
                    // We don't want to do this here anymore because creating activities in VPS 
                    // is enabled only when wayspots are available.
                    // We used to allow the "start of creation" before finding wayspots. 
                    // StartCoroutine(BeginNewActivity());
                    return;
                // case LocalizationState.Failed:
                // case LocalizationState.Stopped:
                // default:
                //     //BottomPanelManager.Instance.ShowMessagePanel($"Localization failed.", true);
                //     print($" *** >>> localization new state: {newState} | state: {state} #localization");
                //     OnLocalizedFailed();
                //     return;
            }
        }

        // IEnumerator BeginNewActivity()
        // {
        //     yield return new WaitForSeconds(1f);
        //     
        //     Instance.OnNewActivityClicked();
        // } 
        
        void OnKeywordsReceived(PhotoChallengeKeywords keywords, ErrorInfo error)
        {
            print("InteractionManager.OnKeywordsReceived");             
            currentChallengePhotoVideoActivity?.ShowChallengeLoader(false);                        
            if (error != null)
            {
                print($"OnKeywordsReceived error: {error.Message}");

                var message = "Oops, we cannot retrieve keywords for your image. Please try again.";
                if (error.Message.Contains("Could not find file"))
                    message = "We cannot process your photo. Please select or take another photo.";
                else if (error.ErrorCode == ErrorCodes.NetworkError)
                    message = "It seems like you have a slow internet connection or not connected to the internet at all.";

                BottomPanelManager.Instance.ShowMessagePanel(message, true, false, () =>
                {
                    var currAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();

                    // The current anchor was not registered?
                    if (currAnchorObj == null)
                    {
                        var anchors = ActiveAnchorManager.GetAnchorObjectList();
                        if (anchors is { Count: > 0 })
                        {
                            currAnchorObj = anchors[0];
                        }
                    }

                    if (currAnchorObj == null)
                    {
                        // Then we have a big problem!
                        // There's no way for us to retrieve the photo panel and cancel the loader.
                        BottomPanelManager.Instance.ShowMessagePanel("Oops, it seems like we've encountered an issue that we cannot recover from. Please restart the app and try again.", false);
                        return;
                    }

                    //print($"OnKeywordsReceived > currAnchorObj is null? = {currAnchorObj==null}");
                    AnchorController controller = currAnchorObj.GetComponent<AnchorController>();
                    //print($"OnKeywordsReceived > AnchorController is null? = {controller == null}");
                    var photoActivity = controller.GetActivityManager().GetPhotoVideoActivity();
                    //print($"OnKeywordsReceived > photoActivity is null? = {photoActivity == null}");
                    if (photoActivity != null)
                    {
                        photoActivity.ShowChallengeLoader(false);
                        photoActivity.OnCancelChallenge(true);                        
                    }
                });
                return;
            }
            if (keywords.SortedKeywords.Count() > 0)
                OnKeywordsGenerated(string.Join(",", keywords.SortedKeywords));
            else
                OnKeywordsGenerated("");
        }

        public void OnFinishButton() {
            // GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            // AnchorController anchorController = currentAnchorObj.GetComponent<AnchorController>();
            // anchorController.OnFinishButtonPressed();
            print("FINISH!");
            OnBackToNativeClicked();
        }
        
        public void OnFinishButtonWithoutConfirmation() {
            print("FINISH!");
            OnDropAndQuitARClicked();
        }

        public void OnAddButton() {
            GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
            AnchorController anchorController = currentAnchorObj.GetComponent<AnchorController>();
            anchorController.OnAddButtonPressed();
        }

        private void RemoveDelegates()
        {
            IntegrationProxy.ReleaseARSessionDelegates();
            WayspotAnchorManager.Instance.OnAnchorLocalizationStateUpdated -= OnAnchorLocalizationStateUpdated;
            NearbyVPSManager.Instance.OnWayspotsFound -= OnWayspotsFound; // VpsCoverageManager.Instance.OnLocalizationTargetsFound -= OnLocalizationTargetsFound;
            NearbyVPSManager.Instance.OnGpsOptionSelected -= OnGpsOptionSelected;
            NearbyVPSManager.Instance.OnWayspotSelected -= OnWayspotSelected;
            NearbyVPSManager.Instance.OnNavigateToSelectedWayspot -= OnNavigateToSelectedWayspot;
            SBRestClient.Instance.OnMapActivitySubmission -= OnMapActivitySubmission;
            SBRestClient.Instance.OnBreadcrumbSubmitted -= OnBreadcrumbSubmitted;   
            SBRestClient.Instance.OnMapActivitySubmission -= OnMapActivitySubmission;
            SBRestClient.Instance.OnKeywordsReceived -= OnKeywordsReceived;
        }

        public void DestroyARSession() {
            // ToDo: replaced with Lightship implementation - should be removed.
            // Destroy(arSession);
            // arSession = null;
            // #lightship-REMOVE_TEMPORARILY 
            // WorldPositionСorrection.Instance.MoveAndRotateBackToInit();
            // End Todo.
            RemoveDelegates();
        }
        
        void QuiARSession()
        {
            print("1 - ClearARUser...");
            ARHelper.Instance.ClearARUser(SBContextManager.Instance.context.userId);
            
            // We need to reset this so that in the next AR session
            // when the "OnAnchorLocalizationStateUpdated" kicks in,
            // we can treat the status as a fresh one. So that we won't exit the method unnecessarily.
            _wayspotLocalizationState = WayspotLocalizationState.Undefined;
            print("2 - StopLocalization... #wayspotLocalizationState_SET_STATE");
            WayspotAnchorManager.Instance.StopLocalization();
            print("3 - StopCoverage...");
            VpsCoverageManager.Instance.StopCoverage();
            print("4 - StopARSession...");
            ARHelper.Instance.DeinitializeARSession();
            RemoveDelegates();
        }
        
        public void EnableARSession() {
            // ToDo: replaced with Lightship implementation - should be removed.
            // if (arSession == null) {
            //     arSession = Instantiate(arSessionPref);
            // }
            // End ToDo.
            
            ARHelper.Instance.InitializeARSession();

            if (Instance.executionMode != ExecutionMode.TestingARSession) return;
            // print($"ARSession.state={ARHelper.Instance.Session?.State}");
            StartCoroutine(Test_WaitForARSession(() =>
            {
                // print($"ARSession.state={ARHelper.Instance.Session?.State}");
            }));
        }
        
        private IEnumerator Test_WaitForARSession(Action onStatusUpdate)
        {
            // var isWaiting = false;
            // while (ARHelper.Instance.Session?.State != ARDKAR.ARSessionState.Running)
            // {
            //     if (!isWaiting)
            //     {
            //         print($"ARSession.state={ARHelper.Instance.Session?.State}");
            //         isWaiting = true;
            //     }
            //
            //     yield return null;
            // }
            
            yield return null;
            
            print("Initialization completed.");
            onStatusUpdate();
        }

        #region For Testing Stand-alone mode

        private void LaunchARCreation()
        {
            var json = JsonConvert.SerializeObject(MakeupDummyInitModel());
            print($"standalone > LaunchARCreation > json={json}");
            IntegrationProxy.Instance.StartARCreation(json);
        }
        private ARSessionInitModel MakeupDummyInitModel()
        {
            return new ARSessionInitModel
            {
                Context = MakeupDummyContext(),
                Config = new Configurations
                {   
                    apiURL = "https://www.bonsaimediagroup.com/",
                },
                AuthToken = new AuthorizationToken
                {
                    Identifier = "SBAPI",
                    Token = "INVALID_TOKEN",
                    ExpiresIn = DateTime.Now.AddDays(30).TimeOfDay.Ticks
                }
            };
        }
        
        private SBContext MakeupDummyContext()
        {
            SBContext fakeContext = new SBContext()
            {
                enablePrivateVPSAnchors = true,
                
                markerPlacementType = (int)SBAnchorPlacementType.VPS,
                
                experienceId = "EXP_001",
                experienceName = "Seattle City Tour",
                collectionId = "ACTG_001",
                collectionName = "Seattle Art Museum",
                userId = Guid.NewGuid().ToString(),
                stats = new ExperienceStatistics
                {
                    Distance  = 0,
                    Elevation = 0,
                    Points = 0,
                    Steps = 0,
                    TotalTime = 0
                },
                startWithPhotoVideo = false
                
            };

            return fakeContext;
        }
        
        #endregion
        
        
        //----------------------------------------- PlaceNote related -----------------------------------------

        #region Placenote related


        //-------------------- PlaceNote callbacks --------------------
        
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
        
        public void OnLocalizedFailed()
        {
            print("Localization failed, please go and face your phone to the area as the thumbnail.");
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.RetryLocalizing);
        }

        public void OnWayspotLocalized()
        {
            if (VpsCoverageManager.Instance.ShowLocationMessage)
                BottomPanelManager.Instance.ShowMessagePanel($"You are now localized. Tap on the 'X' icon to create an activity.");
            //update UI mode
            
            print($"1 - User is now localized. ClosestWayspot is null? ={VpsCoverageManager.Instance.ClosestWayspot==null}");
            VpsCoverageManager.Instance.SelectedWayspot = VpsCoverageManager.Instance.ClosestWayspot;

            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
            
            // //hide points-bar and minimap
            // PointsBarManager.Instance.HidePointsBar();
            // MiniMapManager.Instance.HideMiniMap();

            if (SBContextManager.Instance.IsEditCreating() || SBContextManager.Instance.IsConsuming())
            {
                OnEditVPSOnly();
            }
            else
            {
                if (SBContextManager.Instance.CanShowCursor())
                {
                    ARCursorRenderer.Instance.StartCursor();
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.MovingReticle);
                }
                
                // UIEvent.Localized is also fired inside OnEditVPSOnly.
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.Localized);
            }
            // //show GPS anchors from other maps
            // //GPSAnchorManager.Instance.ShowGPSAnchorsDummy();
            // GPSAnchorManager.Instance.ShowGPSAnchors();
        }
         
        /// <summary>
        /// This is called when editing Planning
        /// </summary>
        public void OnEditPlanning()
        {
            print("InteractionManager.OnEditPlanning");

            isLocating = false;
            
            //start reticle
            OnNewActivityClicked();
        }


        public void OnSelectedLocationForEditPlanning(Vector3 reticlePosition)
        {
            print("InteractionManager.OnSelectedLocationForEditPlanning");
            LoadContent(reticlePosition);

            ActiveAnchorManager.SortAnchorObjByDistance();
            ActiveAnchorManager.InitPulseIndicator();
            
            //update UI mode
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.Localized);
            
            print("Calling ActiveAnchorManager.RegisterClosestEngagedAnchor...");
            ActiveAnchorManager.RegisterClosestEngagedAnchor();
            
            StartCoroutine(DelaySetOffScreenIndicator());
        }
        

        /// <summary>
        /// This is called when editing in GPS only mode.
        /// </summary>
        public void OnEditGPSOnly()
        {
            isLocating = false;
            LoadContent(Vector3.negativeInfinity);
 
            if (!SBContextManager.Instance.context.isPlanning &&
                SBContextManager.Instance.context.OtherAnchors.Any())
            {
                //show GPS anchors for testing only
                //GPSAnchorManager.Instance.ShowGPSAnchorsDummy();
                GPSAnchorManager.Instance.ShowGPSAnchors(() =>
                {
                    ActiveAnchorManager.SortAnchorObjByDistance();
                    ActiveAnchorManager.InitPulseIndicator();
                });
            }
            
            //update UI mode
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.Localized);
            
            print("Calling ActiveAnchorManager.RegisterClosestEngagedAnchor...");
            ActiveAnchorManager.RegisterClosestEngagedAnchor();

            StartCoroutine(DelayedGroundCorrection4GPSAnchors(() =>
            {
                //AnchorManager.Instance.SortAnchorObjByDistance(); //move to LoadContent()
                StartCoroutine(DelaySetOffScreenIndicator());
                
                // PlaneManager.Instance.SetARPlanesVisible(false); 
                // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.None);
                // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);
                ARHelper.Instance.StopPlaneDetection();
            }));
        }
        
        /// <summary>
        /// This is called when editing in VPS mode.
        /// </summary>
        /// <remarks>
        /// For now, this is similar to "OnEditGPSOnly".
        /// </remarks>
        public void OnEditVPSOnly()
        {
            isLocating = false;
            LoadContent(Vector3.negativeInfinity);
 
            StartCoroutine(DelayedGroundCorrection4GPSAnchors(() =>
            {
                StartCoroutine(DelaySetOffScreenIndicator());
                ARHelper.Instance.StopPlaneDetection();
            }));
        }
        
        private IEnumerator DelayedGroundCorrection4GPSAnchors(Action postAction)
        {
            yield return new WaitForSeconds(2f);
            
            // #lightship-REMOVE_TEMPORARILY
            // float groundHeight;
            // bool grounFound = false;
            // for (int i = 2; i <= 120 && !grounFound; i++) //it will check in the following 2 minutes for a ground detection
            // {
            //     MessageManager.Instance.DebugMessage($"Checking if gound found [{i}]...");
            //     groundHeight = PlaneManager.Instance.GetGroundHeight();
            //     if (groundHeight != float.NegativeInfinity)
            //     {
            //         AnchorManager.Instance.CorrectAllAnchorHeight(groundHeight);
            //         MessageManager.Instance.DebugMessage($"Ground found!, height = {groundHeight}");
            //         grounFound = true;
            //         //yield break;
            //     }
            //     else
            //     {
            //         MessageManager.Instance.DebugMessage($"Ground NOT found.");
            //     }
            //     yield return new WaitForSeconds(1f);
            // }
            // END.
            
            //post action
            postAction?.Invoke();
        }


        /// <summary>
        /// Y-axis correction!
        /// Correct the y axis of the whole AR content, based on the ground plane from plane detection.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayedYaxisCorrection()
        {
            yield return new WaitForSeconds(2f);
            AnchorManager.Instance.YaxisCorrection();

            //turn plane detection off
            // PlaneManager.Instance.SetARPlanesVisible(false);
            // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.None);
            // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);
            ARHelper.Instance.StopPlaneDetection();
            
            //Add AR anchors after Y-axis correction
            // AnchorManager.Instance.AddARAnchors();
        }
        
        
        private IEnumerator DelaySetOffScreenIndicator()
        {
            yield return new WaitForSeconds(1f);
            
            var aController = ActiveAnchorManager.GetNeededConsumeAnchorController();
            if (aController != null)
            {
                //set target to the anchor which has not yet been consumed
                OffScreenIndicatorManager.Instance.SetTarget(aController.bodyCenter.transform);
                OffScreenIndicatorManager.Instance.ShowArrow();
            
                //update minimap
                MiniMapManager.Instance.SetRedPoint(aController.GetAnchorInfo().id);
                
                print($"DelaySetOffScreenIndicator() for {aController.GetAnchorInfo().id}");
            }
            else
                print($"DelaySetOffScreenIndicator(): nothing to consume");
        }

        
        //no need anymore since we have Y-axis correction.
        // private IEnumerator DelayStopSendingFrames()
        // {
        //     yield return new WaitForSeconds(10f);
        //     LibPlacenote.Instance.StopSendingFrames();
        //     print("Stopped sending frames to Placenote");
        // }
        

        //-------------------- other PlaceNote related methods --------------------


        private bool isScanningComplete = false;
        private void CheckIfEnoughPointcloudCollectedForAnchor()
        {
            // get the full point built so far
            int currentAnchorSize = GetCurrentAnchorSize();
            //print(string.Format("-----> last map size=\'{0}\', curr map size=\'{1}\'", this.lastMapSize, currentAnchorSize));

            currentAnchorScanningProgress = (float)currentAnchorSize / (float)Const.MIN_MAP_SIZE;
            ActivityUIFacade.Instance.SetMappingProgress(currentAnchorScanningProgress);
            //BottomPanelManager.Instance.UpdateMessage("Scan a rich-featured object around to collect as must purple dot as possible.");

            // #lightship-REMOVE_TEMPORARILY
            // //run SBThumbnail collector
            // SBThumbnailSelector.Instance.Capture(currentAnchorScanningProgress);

            //end scanning if it's good enough
            // #placenote2lightship BEGIN
            // if (currentAnchorSize >= Const.MIN_MAP_SIZE)
            // {
            //     if (LibPlacenote.Instance.GetMode() == LibPlacenote.MappingMode.MAPPING)
            //         print("Enough information collected for this activity.");
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
        
        
        private int GetCurrentAnchorSize()
        {
            //another alternative method to get feature point count:
            //List<Vector3> fullPointCloudMap = FeaturesVisualizer.GetPointCloud();
            //fullPointCloudMap.Count

            // #placenote2lightship BEGIN
            // LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap();
            // if (map != null && map.Length > 0)
            //     return map.Length - this.lastMapSize;
            // else
            //     return 0;
            return 0;
            // #placenote2lightship END
        }


        /// <summary>
        /// For GPS only mode, planining mode, or VPS mode.
        /// </summary>
        public void OnScanningSkiped() 
        {
            print(string.Format("Scanning skipped!"));
            
            //disable plane visualization
            // PlaneManager.Instance.SetARPlanesVisible(false);
            // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.None);
            // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);
            ARHelper.Instance.StopPlaneDetection();
            
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                DoSkipScanning();
            });
        }


        private void DoSkipScanning()
        {
            print("DoSkipScanning");
            //update state of anchor object

            var currentAnchor = ActiveAnchorManager.GetCurrentAnchorObject();
            print($"currentAnchor is null? {currentAnchor==null}");
            var controller = currentAnchor.GetComponent<AnchorController>();
            print($"controller is null? {controller==null}");
            if (RecordManager.Instance.startWithPhotoVideo)
            {
                print("startWithPhotoVideo");
                controller.SetUIMode(AnchorController.UIMode.Creating_PhotoVideo);
                // controller.GetActivityManager().SetInteractable(0, false);
            }
            else
            {
                print("!startWithPhotoVideo");
                controller.SetUIMode(AnchorController.UIMode.Creating_EditingPost);
            }
            
            print("apply behaviour mode...");
            //apply behaviour mode
            controller.SetBehaviourMode(AnchorController.BehaviourMode.Creating_SettingActivities);
            print("set the current anchor as 'scanning completed...");
            //set the current anchor as 'scanning completed'.
            controller.isScanningCompletedOrSkipped = true;
            print("StartMapCreating...");
            //start create breadcrumbs
            NavigationManager.Instance.StartMapCreating();
            print("SetPoiAfterDelay...");
            controller.SetPoiAfterDelay(false);
        }
        
        #endregion

        //--------------------------------------------- Legacy code -----------------------------------------------
        
        #region Legacy
        


        // #placenote2lightship BEGIN
        // private void AddMapToList(LibPlacenote.MapInfo mapInfo)
        // {
        //     GameObject newElement = Instantiate(SelectMapFacade.Instance.mapInfoElementPrefab) as GameObject;
        //     if (newElement)
        //     {
        //         SBMapInfoElement listElement = newElement.GetComponent<SBMapInfoElement>();
        //         listElement.Initialize(mapInfo, SelectMapFacade.Instance.mapListContentToggleGroup, SelectMapFacade.Instance.mapListContentParent, (value) =>
        //         {
        //             OnMapSelected(mapInfo);
        //         });
        //     }
        // }
        // #placenote2lightship END

        // #placenote2lightship BEGIN
        // private void OnMapSelected(LibPlacenote.MapInfo mapInfo)
        // {
        //     // selectedMapInfo = mapInfo;
        //
        //     //UIManager.Instance.SetUIMode(UIManager.UIMode.SelectMap); //toggle button state will be ruined if enabled...
        //     SelectMapFacade.Instance.SetUIMode(SelectMapFacade.UIMode.SelectMap_MapSelected);
        //     //print("Map selected: " + mapInfo.placeId);
        // }
        // #placenote2lightship END


        // public void OnAbortCreatorProcess()
        // {
        //     print("Activity creation canceled.");
        //
        //     // //stop reticle if it's existing
        //     // if(UIManager.Instance.GetCurrentUIMode() == UIManager.UIMode.ActivitySetting)
        //     //     ReticleController.Instance.StopReticle();
        //
        //     //stop placenote session
        //     LibPlacenote.Instance.StopSession();
        //     FeaturesVisualizer.ClearPointcloud();
        //
        //     //clear created anchor objects
        //     GetComponent<AnchorManager>().ClearAnchors();
        //
        //     //critical: reset some flags!
        //     this.lastMapSize = 0; //reset the map size based on the last scanned anchor.
        //     isNewCollection = true; //reset the flag for creating collection
        //
        //     //reset some debug info (optional)
        //     MessageManager.Instance.UpdateMapSize(0);
        //     MessageManager.Instance.UpdateCurrentAnchorSize(0);
        //
        //     //update UI mode to the initial mode
        //     UIManager.Instance.SetUIMode(UIManager.UIMode.Init);
        //     InitFacade.Instance.SetUIMode(InitFacade.UIMode.Inititialized);
        // }


        

        #endregion
        
    }

}
