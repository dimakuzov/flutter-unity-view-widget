using System;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.PersistentAnchors;
using Niantic.Lightship.AR.Protobuf;
using Niantic.Lightship.AR.Subsystems.PersistentAnchor;
using Niantic.Lightship.AR.XRSubsystems;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using TrackingState = UnityEngine.XR.ARSubsystems.TrackingState;

namespace SocialBeeARDK
{
    /// <summary>
    /// Extension of <see cref="LocalizationState"/>.
    /// </summary>
    public enum WayspotLocalizationState  
    {
        // Undefined = -1,
        
        /// System has not been started yet
        Undefined = 0,

        /// System is using device and GPS information to determine if localization is possible.
        Localizing = 1,

        /// Localization in process. Continue to scan the localization target.
        Localized = 2,

        /// Localization succeeded. Anchors can now be created.
        // Localized = 3,

        /// Localization failed, a failure reason will be provided.
        Failed = 4,

        /// Localization stopped by user
        Stopped = 5

    }

    public class SBWayspotAnchor
    {
        public SBWayspotAnchor()
        {
            IsFresh = true;
        }
        
        /// <summary>
        /// The GameObject of the anchor.
        /// </summary>
        public GameObject Object { get; set; }
        /// <summary>
        /// The information about the anchor that includes the activities created with the anchor.
        /// </summary>
        public AnchorInfo Info { get; set; }
        /// <summary>
        /// Flag that indicates if the anchor has just been spawned.
        /// The Waypostservice can trigger the HandleWayspotAnchorTrackingUpdated multiple times,
        /// changing the position and rotation of the anchor.
        /// We need this to prevent the anchor from being "Born" or "Reborn".
        /// </summary>
        public bool IsFresh { get; set; }
    }
    
    /// <summary>
    /// This class manages the instance of all the anchor('Activity') objects
    /// </summary>
    public class WayspotAnchorManager : BaseAnchorManager<WayspotAnchorManager>, IAnchorManager
    {
        [SerializeField] private ARPersistentAnchorManager anchorManager;

        private Dictionary<TrackableId, SBWayspotAnchor> _wayspotAnchors =  new Dictionary<TrackableId, SBWayspotAnchor>();
        
        public Action<ARPersistentAnchorStateChangedEventArgs> OnAnchorLocalizationStateUpdated;
        /// <summary>
        /// True, if the localization has started even once. It doesn't matter if it has been restarted.
        /// </summary>
        public bool HasLocalicazationStarted;
        
        public TrackingState LocalizationState 
        {
            get
            {
                if (anchorManager != null && anchorManager.trackables.count > 0)
                {
                    foreach (var anchor in anchorManager.trackables)
                    {
                        return anchor.trackingState;
                    }
                }
                return TrackingState.None;
            }
        }

        public bool IsLocalized {
            get
            {
                if (anchorManager != null)
                {
                    foreach (var anchor in anchorManager.trackables)
                    {
                        if (anchor.trackingState == TrackingState.Tracking)
                            return true;
                    }
                }
                return false;
            }
        }

        private ARPersistentAnchorManager AnchorManager => anchorManager;

        // private readonly Dictionary<TrackableId, ARPersistentAnchor> _trackedAnchors =
        //     new Dictionary<TrackableId, ARPersistentAnchor>();


        public void Start()
        {
            anchorManager.arPersistentAnchorStateChanged += OnLocalizationStatusChanged;
            anchorManager.DebugInfoUpdated += OnDebugInfoUpdated;
            anchorManager.VpsDebuggerEvent += OnVpsDebuggerEvent;
        }
        
        private void OnDestroy()
        {
            // _anchorManager?.Dispose();
            if (anchorManager != null)
            {
                anchorManager.arPersistentAnchorStateChanged -= OnLocalizationStatusChanged;
                anchorManager.arPersistentAnchorStateChanged += OnAnchorStateChanged;
                anchorManager.DebugInfoUpdated -= OnDebugInfoUpdated;
                anchorManager.VpsDebuggerEvent -= OnVpsDebuggerEvent;
            }
        }
        
        private void OnLocalizationStatusChanged(ARPersistentAnchorStateChangedEventArgs args)
        {
            OnAnchorLocalizationStateUpdated?.Invoke(args);
        }
        
        private void OnAnchorStateChanged(ARPersistentAnchorStateChangedEventArgs args)
        {
            var anchor = args.arPersistentAnchor;
        
            Debug.Log($"╔══════════════════════════════════════════════════════════");
            Debug.Log($"║ [ANCHOR STATE CHANGED]");
            Debug.Log($"║ Anchor ID: {anchor.trackableId}");
            Debug.Log($"║ Tracking State: {anchor.trackingState}");
            Debug.Log($"║ Tracking Reason: {anchor.trackingStateReason}");
            Debug.Log($"║ Confidence: {anchor.trackingConfidence}");
            Debug.Log($"║ Position: {anchor.transform.position}");
            Debug.Log($"║ Rotation: {anchor.transform.rotation.eulerAngles}");
            Debug.Log($"╚══════════════════════════════════════════════════════════");
        
            // Детальная информация по состоянию
            switch (anchor.trackingState)
            {
                case TrackingState.None:
                    Debug.LogWarning($"⚠️ Anchor {anchor.trackableId} - NOT TRACKING");
                    Debug.LogWarning($"   Reason: {anchor.trackingStateReason}");
                    break;
                
                case TrackingState.Limited:
                    Debug.Log($"🟡 Anchor {anchor.trackableId} - LIMITED TRACKING");
                    Debug.Log($"   Confidence: {anchor.trackingConfidence}");
                    break;
                
                case TrackingState.Tracking:
                    Debug.Log($"✅ Anchor {anchor.trackableId} - FULL TRACKING");
                    Debug.Log($"   Confidence: {anchor.trackingConfidence}");
                    break;
            }
        }
        
        private void OnDebugInfoUpdated(XRPersistentAnchorDebugInfo args)
        {}
        
        private void OnVpsDebuggerEvent(VpsDebuggerDataEvent args)
        {
        
        }
        
        
        private void Update()
        {
            if (SBContextManager.Instance.context == null || SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) return;
            
            //checking interaction
            HandleAnchorInteraction();
            
            if (anchorManager == null || !HasLocalicazationStarted)
            {
                return;
            }
            
            // OnAnchorLocalizationStateUpdated?.Invoke(_anchorManager.LocalizationState);
            
            if (SBContextManager.Instance.context.isEditing && spawnedAnchors < anchorsToSpawn)
            {
                return;
            }
            //check anchor engagement
            var checkDistance = CheckDistance(null);
        }
        
        // private void HandleSessionInitialized(AnyARSessionInitializedArgs args)
        // {
        //     print("HandleSessionInitialized");
        //     _arSession = args.Session;
        // }
        
        // private WayspotAnchorService CreateWayspotAnchorService()
        // {
        //     print($"1 - WayspotAnchorService");
        //     var wayspotAnchorsConfiguration = WayspotAnchorsConfigurationFactory.Create();
        //     wayspotAnchorsConfiguration.LocalizationTimeout = -1;
        //
        //     print($"2 - WayspotAnchorService > wayspotAnchorsConfiguration is null? {wayspotAnchorsConfiguration==null}");
        //     var locationService = LocationServiceFactory.Create(ARHelper.Instance.Session.RuntimeEnvironment);
        //     print($"3 - WayspotAnchorService > locationService is null? {locationService==null}");
        //     locationService?.Start();
        //     
        //     print($"4 - WayspotAnchorService > LocationService.Status={VpsCoverageManager.Instance.LocationService.Status}");
        //     var wayspotAnchorService = new WayspotAnchorService(
        //         ARHelper.Instance.Session, 
        //         locationService, //VpsCoverageManager.Instance.LocationService, 
        //         wayspotAnchorsConfiguration);
        //     return wayspotAnchorService;
        // }

        public void StartLocalization()
        {
            if (ARHelper.Instance.arSession == null)
            {
                // Debug.LogWarning($"There is no active AR session. Localization failed.");
                print("There is no active AR session. Localization failed.");
                BottomPanelManager.Instance.ShowMessagePanel("There is no active AR session. Localization failed.");
                return;
            }
            print("creating a new WayspotAnchorService...");
            // _anchorManager = CreateWayspotAnchorService();
            HasLocalicazationStarted = true;
            print("exiting StartLocalization...");
        }

        /// <summary>
        /// Pauses the polling of the _anchorManager.
        /// </summary>
        public void PauseLocalization()
        {
            HasLocalicazationStarted = false;
        }
        
        /// <summary>
        /// Stops the _anchorManager localization and also destroys that instance of the WayspotAnchorService.
        /// </summary>
        public void StopLocalization()
        {
            HasLocalicazationStarted = false;
            // _anchorManager.StopLocation();
            OnDestroy();
        }
        
        public async void RetryLocalization()
        {
            if (ARHelper.Instance.arSession == null)
            {
                // Debug.LogWarning($"There is no active AR session. Localization failed.");
                print("There is no active AR session. Localization failed.");
                BottomPanelManager.Instance.ShowMessagePanel("There is no active AR session. Localization failed.");
                return;
            }
            if (anchorManager == null) return;
            
            HasLocalicazationStarted = true;
            await AnchorManager.RestartSubsystemAsync();
        }

        public void RefreshAnchorService()
        {
            // print($"LocationService is null? {VpsCoverageManager.Instance.LocationService==null}");
            // _anchorManager = new WayspotAnchorService(
            //     ARHelper.Instance.Session,
            //     VpsCoverageManager.Instance.LocationService,
            //     WayspotAnchorsConfigurationFactory.Create()
            // );
        }

        /// <summary>
        /// ARDK 2.4.1 implementation.
        /// </summary>
        /// <param name="touchPosition"></param>
        public void PlaceAnchor(Pose pose)
        {
            if (anchorManager == null) return;
            print($"SpawnAnchorObject > touchPosition={pose.ToString()} #touchposition");
            // Pose pose = touchPosition.ToPose();

            if (AnchorManager.TryCreateAnchor(pose, out ARPersistentAnchor newAnchor)) {
                Debug.Log($"PlaceAnchor newAnchor.trackableId: {newAnchor.trackableId}");
                CreateAnchorGameObject(newAnchor, anchorInfo: null);
            }
        }

        private void CreateAnchorGameObject(ARPersistentAnchor anchor, AnchorInfoList anchorList)
        {
            var anchorInfo = anchorList.mapContent.FirstOrDefault(x => x.id == anchor.trackableId.ToString());
            CreateAnchorGameObject(anchor, anchorInfo);
        }

        private void CreateAnchorGameObject(ARPersistentAnchor anchor, AnchorInfo anchorInfo = null)
        {
            _wayspotAnchors ??= new Dictionary<TrackableId, SBWayspotAnchor>();
            print("#anchordebug 1 --*1");
            if (_wayspotAnchors.ContainsKey(anchor.trackableId))
            {
                print("anchor already exists #anchordebug");
                return;
            }
             
            var id = anchor.trackableId.ToString();
            var anchorName = $"Anchor {anchorInfo?.postInfo.Title ?? id.ToString()}";
            print($"anchorName={anchorName} #anchordebug");
            
            print($"Instantiate(AnchorObjectPrefab) #anchordebug");
            var anchorGO = Instantiate(AnchorObjectPrefab, anchor.transform);
            anchorGO.name = anchorName;
            
            anchorGO.transform.localPosition = Vector3.zero;
            anchorGO.transform.localRotation = Quaternion.identity;

            print($"adding anchor with ID: {id} | _wayspotAnchors==null? [{_wayspotAnchors==null}] | anchorInfo==null? [{anchorInfo==null}] #vpsreborndebug #anchordebug");
            _wayspotAnchors.Add(anchor.trackableId, new SBWayspotAnchor
            {
                Object = anchorGO,
                Info = anchorInfo
            });
            
            // ARPersistentAnchor tracker = anchorGO.GetComponent<ARPersistentAnchor>();
            // if (tracker == null)
            // {
            //     print("Anchor prefab is missing a WayspotAnchorTracker, adding one now. #anchordebug");
            //     tracker = anchorGO.AddComponent<ARPersistentAnchor>();
            // }
            // tracker.gameObject.SetActive(false);
            // print($"adding wayspotAnchor to the anchorTracker #anchordebug");
            // tracker.AttachAnchor(anchor);
        }
        
        // [Obsolete("Use CreateAnchorGameObject(IWayspotAnchor) instead.")]
        // private void CreateAnchorGameObjects(IWayspotAnchor[] wayspotAnchors)
        // {
        //     CreateAnchorGameObjects(wayspotAnchors, null);
        // }
        
        // [Obsolete("Use CreateAnchorGameObject(IWayspotAnchor,AnchorInfoList) instead.")]
        // private void CreateAnchorGameObjects(IWayspotAnchor[] wayspotAnchors, AnchorInfoList anchorList)
        // {
        //     Debug.Log($"1 - anchor count = {wayspotAnchors.Length} | anchorList = {(anchorList == null || anchorList.mapContent == null ? 0 : anchorList.mapContent.Length)}");
        //     _wayspotAnchors ??= new Dictionary<Guid, SBWayspotAnchor>();
        //     foreach (var wayspotAnchor in wayspotAnchors)
        //     {
        //         // print("anchorobject 1");
        //         if (_wayspotAnchors.ContainsKey(wayspotAnchor.ID))
        //         {
        //             print("anchor already exists");
        //             continue;
        //         }
        //         wayspotAnchor.TrackingStateUpdated += HandleWayspotAnchorTrackingUpdated;
        //         AnchorInfo anchorInfo = null; 
        //         var id = wayspotAnchor.ID;
        //         var anchorName = $"Anchor {id}";
        //         // Debug.Log($"Getting the anchorInfo for wayspotAnchor {wayspotAnchor.ID}...");
        //         if (anchorList is { mapContent: { } })
        //         {
        //             anchorInfo = anchorList.mapContent.FirstOrDefault(x => x.id == wayspotAnchor.ID.ToString());
        //             anchorName = $"Anchor {anchorInfo?.postInfo.Title}";
        //         }
        //         // Debug.Log($"anchorInfo for wayspotAnchor {wayspotAnchor.ID} found? = {anchorInfo != null}");
        //         
        //         //var anchor = Instantiate(AnchorObjectPrefab);
        //         var anchor = Instantiate(AnchorObjectPrefabDEBUG);
        //         anchor.SetActive(false);
        //         anchor.name = anchorName;
        //         //print($"adding anchor with ID: {id} | _wayspotAnchors==null? [{_wayspotAnchors==null}] | anchorInfo==null? [{anchorInfo==null}] #vpsreborndebug");
        //         //_wayspotAnchorGameObjects.Add(id, anchor);
        //         _wayspotAnchors.Add(id, new SBWayspotAnchor
        //         {
        //             Object = anchor,
        //             Info = anchorInfo
        //         });
        //         print($"AnchorCount={AnchorCount} | anchorInfo found? {anchorList?.mapContent?.FirstOrDefault(x => x.id == wayspotAnchor.ID.ToString()) != null} #vpsdebug");
        //     }
        // }

        // public void SpawnAnchor(IWayspotAnchor wayspotAnchor, [CanBeNull] AnchorInfoList anchorList)
        // {
        //     var sbAnchor = _wayspotAnchors[wayspotAnchor.ID];
        //     if (sbAnchor == null)
        //     {
        //         // This should not happen!
        //         print($"The wayspot anchor is missing, with ID: {wayspotAnchor.ID} #wayspot #anchor #spawnanchor");
        //         return;
        //     }
        //     
        //     // if (!wayspotAnchor.IsFresh) return;
        //
        //     SortAnchorObjByDistance();
        //     InitPulseIndicator();
        //
        //     if (SBContextManager.Instance.IsEditCreating() || SBContextManager.Instance.IsConsuming())
        //     {
        //         print($"1 - isEditing #radebug #vpsdebug #RegisterEngagedAnchor > index={closestAnchorIndex} | sbAnchor=null? {sbAnchor==null}");
        //         RebornAnchorObject(sbAnchor, postAction: (anchorController) =>
        //         {
        //             print($"Engaging anchor at index {closestAnchorIndex}...");
        //             RegisterEngagedAnchor(closestAnchorIndex);    
        //             anchorController.OnEngaged();
        //         });
        //     }
        //     else  
        //     {
        //         /////SpawnAnchorObject(_mostRecentTouchPosition.ToPosition(), id, anchor);
        //         print($"isCreating: anchorId={wayspotAnchor.ID} #vpsdebug");
        //         var payload = AnchorService.GetAllWayspotAnchors()
        //             .FirstOrDefault(x => x.ID == wayspotAnchor.ID)?.Payload;
        //         print($"null chk: LastKnownPosition={wayspotAnchor.LastKnownPosition==null} | sbAnchor={sbAnchor==null}" +
        //               $" | sbAnchorGO={sbAnchor.Object==null} | payload={payload==null}");
        //         //BornAnchorObject(sbAnchor.Info.pose.position, wayspotAnchor.ID, sbAnchor.Object, payload);
        //         BornAnchorObject(wayspotAnchor.LastKnownPosition, wayspotAnchor.ID, sbAnchor.Object, payload, postAction: (anchorController) =>
        //         {
        //             RegisterEngagedAnchor(closestAnchorIndex);    
        //             anchorController.OnEngaged();
        //         });
        //     }
        //     // wayspotAnchor.IsFresh = false;
        // }
 
        // [Obsolete("Anchor tracking is now handle with a WayspotAnchorTracker.")]
        // private void HandleWayspotAnchorTrackingUpdated(WayspotAnchorResolvedArgs wayspotAnchorResolvedArgs)
        // {
        //     if (_wayspotAnchors.Count < 1) return;
        //
        //     if (!_wayspotAnchors.ContainsKey(wayspotAnchorResolvedArgs.ID))
        //     {
        //         // This should not happen!
        //         print($"The wayspot ID is not recognized. #wayspot #anchor #spawnanchor");
        //         return;
        //     }
        //
        //     var wayspotAnchor = _wayspotAnchors[wayspotAnchorResolvedArgs.ID];
        //     if (wayspotAnchor == null)
        //     {
        //         // This should not happen!
        //         print($"The wayspot anchor is missing, with ID: {wayspotAnchorResolvedArgs.ID} #wayspot #anchor #spawnanchor");
        //         return;
        //     }
        //      
        //     var anchor = wayspotAnchor.Object.transform;
        //     // print($"wayspotAnchor.TrackingStateUpdated > current pos: {anchor.position} | new pos: {wayspotAnchorResolvedArgs.Position}");
        //
        //     if (anchor.position == wayspotAnchorResolvedArgs.Position) return;
        //     
        //     print($"HandleWayspotAnchorTrackingUpdated > wayspotAnchorResolvedArgs.position={wayspotAnchorResolvedArgs.Position.ToString()} #touchposition");
        //     anchor.position = wayspotAnchorResolvedArgs.Position;
        //     anchor.rotation = wayspotAnchorResolvedArgs.Rotation;
        //     
        //     anchor.gameObject.SetActive(true);
        //
        //     if (!wayspotAnchor.IsFresh) return;
        //
        //     if (SBContextManager.Instance.IsEditCreating() || SBContextManager.Instance.IsConsuming())
        //     {
        //         print($"2 - isEditing #radebug #vpsdebug #RegisterEngagedAnchor > index={closestAnchorIndex}");
        //         RebornAnchorObject(wayspotAnchor);
        //         RegisterEngagedAnchor(closestAnchorIndex);
        //     }
        //     else // either we are editing or consuming
        //     {
        //         /////SpawnAnchorObject(_mostRecentTouchPosition.ToPosition(), id, anchor);
        //         print("isCreating #vpsdebug");
        //         var payload = AnchorManager.GetAllWayspotAnchors()
        //             .FirstOrDefault(x => x.ID == wayspotAnchorResolvedArgs.ID)?.Payload;
        //         BornAnchorObject(anchor.position, wayspotAnchorResolvedArgs.ID, wayspotAnchor.Object, payload);
        //     }
        //     wayspotAnchor.IsFresh = false;
        // }

        // private void BornAnchorObject(Vector3 touchPosition, Guid anchorId, GameObject anchor, ARPersistentAnchorPayload payload, Action<AnchorController> postAction = null)
        // {
        //     print($"SpawnAnchorObject > position: {touchPosition.ToString()} | SelectedWayspotId={VpsCoverageManager.Instance.SelectedWayspotId}");
        //    
        //     var anchorPose = new Pose(touchPosition, Quaternion.identity);
        //     var initialAnchorInfo = new AnchorInfo //prepare an empty anchorInfo
        //     {
        //         id = anchorId.ToString(),
        //         postInfo = new PostActivityInfo { MapId = VpsCoverageManager.Instance.SelectedWayspotId },
        //         pose = anchorPose,
        //         activityInfoList = new List<IActivityInfo>(),
        //     };
        //     initialAnchorInfo.SetAnchorPayload(payload);
        //     print($"#payload={initialAnchorInfo.anchorPayload}");
        //     if (SBContextManager.Instance.context is { isPlanning: true })
        //         initialAnchorInfo.postInfo.MapLocation = SBContextManager.Instance.context.plannedLocation;
        //     
        //     //3. init anchor
        //     var anchorController = anchor.GetComponent<AnchorController>();
        //     var index = AnchorCount - 1;
        //     if (index < 0) index = 0;
        //     anchorController.Born(index, initialAnchorInfo, () => postAction?.Invoke(anchorController));
        //     
        //     //4. add to the anchor list
        //     // At this point, the anchor has already been added to _wayspotAnchorGameObjects.
        //     print($"New anchor spawned @ index: '{index}'");
        //     
        //     SpawnAnchorFinalAction(index, anchorController);
        // }

        /// <summary>
        /// Re-create an anchor object when edit/consume
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="info"></param>
        // private void RebornAnchorObject(SBWayspotAnchor anchor, int? initialIndex = null, Action<AnchorController> postAction = null)
        // {
        //     // The anchor GameObject was already created in "CreateAnchorGameObjects".
        //     // We just need to Reborn here to show the panels.
        //      
        //     //init anchor
        //     var anchorController = anchor.Object.GetComponent<AnchorController>();
        //     print($"anchorController is null? [{anchorController==null}] | initialIndex={initialIndex} | AnchorCount={AnchorCount} | anchorInfo is null? {anchor.Info == null} #vpsdebug #radebug");
        //     anchorController.Reborn(initialIndex ?? (AnchorCount - 1), anchor.Info, ()=>
        //     {
        //         // We should not control the visibility of an anchor here.
        //         // if(SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
        //         //     anchor.SetActive(false);
        //         print("Anchor reborn completed, invoking post action...");
        //         postAction?.Invoke(anchorController);
        //     });
        // }
        
        private TrackableId GetKeyOfAnchorObjectAt(int index)
        {
            return _wayspotAnchors.Keys.ElementAt(index);
        }
        
        #region Override methods

        public override int AnchorCount => _wayspotAnchors.Count; //_wayspotAnchorGameObjects.Values.Count;
        
        public override IEnumerable<string> GetActivityIds()
        {
            print($"AnchorManager.GetActivities: count={AnchorCount}");
            return _wayspotAnchors.Values.SelectMany(x =>
            {
                var info = x.Info; //  x.GetComponent<AnchorController>().GetAnchorInfo();
                var ids = info.activityInfoList.Select(a => a.Id);

                return ids;
            });

        }

        public override void LoadAnchors(AnchorInfoList anchorList)
        {
            if (!IsLocalized)
            {
                print("Must localize before loading anchors.");
                return;
            }

            print($"anchors={SBContextManager.Instance.context.anchors.Count()} | MapAnchors={SBContextManager.Instance.context.MapAnchors.Count()} | OtherAnchors={SBContextManager.Instance.context.OtherAnchors.Count()}");
            ClearAnchors();
            if (anchorList.mapContent == null || !anchorList.mapContent.Any())
            {
                print("No anchors created!");
                return;
            }
            spawnedAnchors = 0;
            anchorsToSpawn = anchorList.mapContent.Length;

            // foreach (var anchorInfo in anchorList.mapContent)
            // {
            //     print($"id={anchorInfo.id} | anchorPayload={anchorInfo.anchorPayload}");
            // }
            
            // A new ID is generated each time an anchor is created from a payload.
            // So we will use this variable to manage the ID.
            // We will map the new anchor ID to the anchorID that was generated when that anchor was first created.
            var retainAnchorId = SBContextManager.Instance.IsEditCreating() ||
                                      SBContextManager.Instance.IsConsuming() ;
            
            // var payloads = anchorList.mapContent
            //     .Where(x=> !x.anchorPayload.IsNullOrWhiteSpace())
            //     .Select(x => ARPersistentAnchorPayload.Deserialize(x.anchorPayload));
            var payloads = anchorList.mapContent
                .Where(x => !x.anchorPayload.IsNullOrWhiteSpace())
                .Select(x => x.anchorPayload)
                .ToList();
            
            // var ARPersistentAnchorPayloads = payloads as ARPersistentAnchorPayload[] ?? payloads.ToArray();
            // if (ARPersistentAnchorPayloads.Any())
            if (payloads.Any())
            {
                if (retainAnchorId)
                {
                    foreach (var payloadData in payloads)
                    {
                        var anchorInfo = anchorList.mapContent.FirstOrDefault(x => x.anchorPayload == payloadData);
            
                        var payload = new ARPersistentAnchorPayload(payloadData);
                        
                        if (anchorManager.TryTrackAnchor(payload, out ARPersistentAnchor anchor))
                        {
                            CreateAnchorGameObject(anchor, anchorInfo);
                        }
                        else
                        {
                            Debug.LogError("LoadAnchors retainAnchorId = true, Error: Failed to track anchor.");
                        }
                    }
                    
                    // Restore one anchor at a time so we can map the new anchorID
                    // with the original anchorID, when the anchor was first created.
                    // foreach (var payload in payloads)
                    // {
                    //     var anchorInfo = anchorList.mapContent.FirstOrDefault(x => x.anchorPayload == payload);
                    //     var anchor =
                    //         AnchorManager.RestoreWayspotAnchors(new[] { ARPersistentAnchorPayload.Deserialize(payload) });
                    //     
                    //     CreateAnchorGameObject(anchor[0], anchorInfo);
                    //     print("Loaded Wayspot Anchors via the payload of a previously created anchor - [editing].");
                    // }
                }
                else
                {
                    foreach (var payloadData in payloads)
                    {
                        var payload = new ARPersistentAnchorPayload(payloadData);
            
                        if (anchorManager.TryTrackAnchor(payload, out ARPersistentAnchor anchor))
                        {
                            CreateAnchorGameObject(anchor, anchorList);
                        }
                        else
                        {
                            Debug.LogError("LoadAnchors Error: Failed to track anchor.");
                        }
                    }
                    
                    // var wayspotAnchorPayloads = payloads.Select(ARPersistentAnchorPayload.Deserialize).ToArray();
                    // var wayspotAnchors = AnchorManager.RestoreWayspotAnchors(wayspotAnchorPayloads);
                    // foreach(var anchor in wayspotAnchors)
                    //     CreateAnchorGameObject(anchor, anchorList);
                    // print("Loaded Wayspot Anchors.");
                }
            }
            else
            {
                print("No anchors to load.");
            }
        }
 
        protected override GameObject GetAnchorObjectAt(int index)
        {
            print($"#debuglockanchor anchors={AnchorCount} | index={index}");
            return _wayspotAnchors.Values.ElementAt(index).Object;
        }
 
        protected override void RemoveAnchorObjectAt(int index)
        {
            var anchorKey = GetKeyOfAnchorObjectAt(index);
            _wayspotAnchors.Remove(anchorKey);

            foreach (var anchor in anchorManager.trackables) {
                if (anchorKey == anchor.trackableId) {
                    anchorManager.DestroyAnchor(anchor);
                    Debug.Log($"Remove anchor {anchor.trackableId}");
                    break;
                }
            }
        }
        
        protected override void AddAnchorObjectToList(GameObject anchor)
        {
            throw new NotImplementedException();
        }

        public override List<GameObject> GetAnchorObjectList()
        {
            return _wayspotAnchors.Values.Select(anchor => anchor.Object).ToList();
        }

        // public Guid GetKeyOfEngagedAnchor()
        // {
        //     return GetKeyOfAnchorObjectAt(engagedAnchorIndex);
        // }

        public override void ClearAnchors()
        {
            if (anchorManager.trackables.count == 0) return;
        
            foreach (var anchor in anchorManager.trackables) {
                anchorManager.DestroyAnchor(anchor);
                Debug.Log($"Remove anchor {anchor.trackableId}");
                break;
            }

            base.ClearAnchors();
        }
 
        public override bool IsReadyToSaveMap()
        {
            //there must have at least one anchor is completed (config completed)
            return AnchorCount > 0 && _wayspotAnchors.Values.Any(t => t.Object.GetComponent<AnchorController>().isConfigCompleted);
        }
        
        #endregion
    }
}

public static class Matrix4x4Extensions
{
    public static Pose ToPose(this Matrix4x4 matrix)
    {
        Vector3 position = matrix.GetColumn(3);
        Quaternion rotation = Quaternion.LookRotation(
            matrix.GetColumn(2),
            matrix.GetColumn(1)
        );
        return new Pose(position, rotation);
    }
}