using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using ARLocation;
using PrimitivePlus;
using SocialBeeARDK;
using UnityEngine.Networking;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


namespace SocialBeeAR
{
     
    
    /// <summary>
    /// This is the class for controlling the behaviour and interaction of the anchor object (a.k.a. 3d icon)
    /// </summary>
    public class AnchorController : MonoBehaviour
    {
        
        /// <summary>
        /// The mode of anchor object's UI
        /// </summary>
        public enum UIMode
        {
            //Busy, //the state where we want to disable the anchor elements (i.e. +, delete, etc) while we are waiting for a response from the API. //no need, as we control it with CanvasGroup
            Creating_Spawned, //just spawned, user can move or delete, no other interaction
            Creating_Scanning, //when scanning
            Creating_EditingPost, //when scanning completed and editing PoI(post) info 
            Creating_ReadyToAddActivities, //when PoI is completed, ready to add more activities
            Creating_Done, //when user tap 'check-sign' button, editing completed.
            Consuming, //consumer mode
            Creating_PhotoVideo //when AR session was started from photo or video taking
        }
        

        /// <summary>
        /// The mode anchor object's behaviour
        /// </summary>
        public enum BehaviourMode
        {
            Creating_Spawned, //when dragging anchor
            Creating_Scanning, //when user is scanning the anchor object, 'LookAt player' is disabled.
            Creating_SettingActivities, //when scanned, and user is editing its activities
            Creating_Done, //for creator, when the whole anchor is done
            Consuming, //by default rotating, unless player is close enough and watching at it.
        }
        

        // This is set to -1 when instantiated, and assigned when changing the anchor, index is the sequence adding to the anchor list of AnchorManager.
        public int index = -1;

        
        //controlling lookAt and rotation
        private bool enableRotation;
        private bool watchingPlayer;
        private Vector3 lookAtTargetPos = Vector3.zero;
        private bool autoReaction;
        private Quaternion anchorBodyInitialRotation = Quaternion.identity;
        
        
        //for saving to cloud
        private AnchorInfo anchorInfo;
        
        //current modes
        private AnchorController.UIMode currUIMode;
        private BehaviourMode currBehaviourMode;
        
        //dragging control
        private ObjectDragger handleDragger;
        
        //activity layout manager!
        private ActivityManager activityManager;

        //activity buttons controller
        private ActivityButtonsController activityButtonsController;

        //indicates if the activity setting is completed
        private bool isActivitySettingDone;
        
        //indicator for if this anchor is born or reborn
        public bool isReborn { private set; get; }

        //indicator of whether scanning has been done at least once.
        [FormerlySerializedAs("isScanningCompleted")] public bool isScanningCompletedOrSkipped;
        public bool isConfigCompleted;
        private bool isPoI = false;
        
        private Vector3 activityButtonsHiddenPose = new Vector3(0, 0, -140);
        private Vector3 activityButtonsExpandedPose = new Vector3(0, 0, 0);
        
        //flat of whether this anchor is ready to 'complete' config, (when any activity is saved, including post, it can be completed)
        private bool isReadyToCompleteConfig;

        public bool isEngaged { private set; get; }

        private Quaternion bottomPlateOriginLocalRot;
        private Vector3 lockOriginLocalPos; //will be set when Start()
        private bool isBornCompleted; //this flag will be set to true only when anchor is completely born or reborn (after animation)

        //flag indicating if this anchor (body plus activities) is interactable
        private bool isInteractable;
        
        //flag indicating if this anchor (body plus activities) is interactable
        [HideInInspector] public int sortOrder;
        
        //ARKit anchor
        private ARAnchor ARRefPoint { get; set; }
        
        //Count of activities after activity was created. Without post panel
        int activityCount;

        //flag indicating if activity in this anchor is creating or editing activities
        [HideInInspector] public bool anchorWasCompleted;
        
        //flag indicating that after quick photo post panel was saved correct
        [HideInInspector] public bool quickPhotoWithPostWasCompleted;
        
        [HideInInspector] public bool wasPostSaved;
        private bool allActivitiesWasSaved;

        private Vector3 lastPosition = Vector3.zero;
        private float timeToUpdateMinimap = 0;

        private Camera mainCam;
        private bool scaleCompleted = false;

        public GameObject markerObj;

        //---------------------anchor elements, start---------------------------
        private List<GameObject> mutualElements;

        //activity panels parent
        [SerializeField] private GameObject activityPanelsParent;

        //body
        [SerializeField] private GameObject anchorObj;
        [SerializeField] private GameObject anchorBody;
        
        //edit elements
        [SerializeField] private GameObject editButtons;
        [SerializeField] private GameObject moreButton;
        [SerializeField] private GameObject lessButton;
        [SerializeField] private GameObject deleteButton;
        
        [SerializeField] private GameObject activityButtonParent;
        
        [SerializeField] private GameObject buttonPoI;        
        [SerializeField] private GameObject buttonTrivia;
        [SerializeField] private GameObject buttonPhotoVideo;
        [SerializeField] private GameObject buttonAudio;

        //covers, be ware of that the sequence below are aligned with the prefab objects
        [SerializeField] private GameObject coverDragging;
        [SerializeField] private GameObject coverScanning;
        [SerializeField] private GameObject coverDefault;
        [SerializeField] private GameObject coverToComplete;
        [SerializeField] private GameObject coverCreate;
        [SerializeField] private GameObject coverReturn;

        //anchor lock
        public GameObject lockerPlate;
        [SerializeField] private GameObject anchorLockButton;
        [SerializeField] private GameObject anchorUnLockButton;
        //[SerializeField] private GameObject returnButton;
        
        //focus effect
        [SerializeField] private GameObject focusEffect;
        
        //others
        [SerializeField] private Text positionText;
        [SerializeField] private Text distanceText;

        //color control
        [SerializeField] private GameObject outter;
        [SerializeField] private GameObject core;
        [SerializeField] private GameObject support;
        [SerializeField] private Material matWhite;
        [SerializeField] private Material matPurple;
        [SerializeField] private Material matOrange;

        //body center and bottom center
        public GameObject bodyCenter;
        public GameObject bottomCenter;

        [SerializeField] private GameObject upbody;
        [SerializeField] private GameObject planningBottom;
        [SerializeField] private RawImage planningMapViewImage;
        [SerializeField] private Text draggingCoverText;
        
        [HideInInspector]
        private static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;
        
        //---------------------------------------- MonoBehaviour methods ---------------------------------------
        

        private void Awake()
        {
            print("#mutualElements 1");
            handleDragger = GetComponentInChildren<ObjectDragger>();
            activityManager = GetComponentInChildren<ActivityManager>();
            activityButtonsController = GetComponentInChildren<ActivityButtonsController>();
            
            mutualElements = new List<GameObject>
            {
                activityPanelsParent,
                editButtons,
                moreButton,
                lessButton,
                deleteButton,
                activityButtonParent,
                coverDragging,
                coverScanning,
                coverCreate,
                coverDefault,
                coverToComplete,
                coverReturn,
                lockerPlate
            };

            // print($"AnchorController.Awake > disabling focusEffect...");
            // focusEffect.SetActive(false);
        }


        private void Start()
        {
            print($"AnchorController.Start");
            anchorBodyInitialRotation = this.anchorObj.transform.localRotation;
            
            lockOriginLocalPos = anchorLockButton.transform.localPosition;
            bottomPlateOriginLocalRot = lockerPlate.transform.localRotation;
            //buttonPoI.GetComponent<ActivityButton>().OnButtonStarted += OnButtonStarted;
            
            mainCam = Camera.main;
        }

        //private void OnDestroy()
        //{
        //    buttonPoI.GetComponent<ActivityButton>().OnButtonStarted -= OnButtonStarted;
        //}

        //private void OnButtonStarted(ActivityButton button)
        //{
        //    print($"name={button.name} | tag={button.tag} | tos={button.ToString()}"); 
        //}
        
        
        private void Update()
        {
            //these info are for debug only
            if(positionText.gameObject.activeSelf)
                positionText.text = transform.position.ToString();
             
            //check distance
            float distance = Vector3.Distance(mainCam.transform.position, transform.position);
            if (distanceText.gameObject.activeSelf)
                distanceText.text = distance.ToString("F2");
            
            //rotation control
            if (enableRotation)
            {
                //anchorObj.transform.Rotate(Vector3.up, Time.deltaTime * Const.ANCHOR_ROTATION_SPEED);
                anchorBody.transform.Rotate(Vector3.up, Time.deltaTime * Const.ANCHOR_ROTATION_SPEED);
            }

            //watching-player control
            if(watchingPlayer)
            {
                WatchPlayer();
            }
            
            //show corrected position on MiniMap
            //to avoid calling 'AddExistConsumeAnchor' many times, we correct the position on Minimap after 1.5 seconds
            if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors && lastPosition != transform.position) {
                //MessageManager.Instance.DebugMessage($">>> anchor '{anchorInfo.id}' position changed: '{lastPosition}'-->'{transform.position}'");
                lastPosition = transform.position;
                if(Time.time > timeToUpdateMinimap) {
                    timeToUpdateMinimap = Time.time + 1.5f;
                    StartCoroutine(MiniMapManager.Instance.CorrectPositionDelay(this, anchorInfo.id));
                    // MessageManager.Instance.DebugMessage($">>>>>> creating new thread for updating Minimap...");
                }
            }
        }


        private void WatchPlayer()
        {
            lookAtTargetPos.Set(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);
            transform.LookAt(lookAtTargetPos); ;
        }


        private void ResetBodyPose()
        {
            anchorBody.transform.localPosition = Vector3.zero;
            anchorBody.transform.localRotation = Quaternion.identity;
        }


        public bool IsReadyToEngage()
        {
            return isBornCompleted
                && autoReaction
                && ((DateTime.Now - engagedTimestamp).TotalMilliseconds >= 2000); //more than 1.2 seconds
        }


        private DateTime engagedTimestamp;
        public void OnEngaged()
        {
            print("AnchorController.OnEngaged");
            if (isEngaged)
                return;
            else
                isEngaged = true;

            engagedTimestamp = DateTime.Now;
            enableRotation = false;
            watchingPlayer = true;

            //anchorObj.transform.localRotation = this.anchorBodyInitialRotation;
            ResetBodyPose();
            focusEffect.SetActive(true); //focus effect
                    
            //update activity layout
            if (SBContextManager.Instance.IsCreating())
            {
                activityManager.SetLayout(ActivityLayout.HorizontalEdit);    
            }
            else
            {
                activityManager.SetLayout(ActivityLayout.HorizontalConsume);
            }
            
            //register this anchor as the focused anchor
            if (GetBehaviourMode() == BehaviourMode.Consuming) //don't do registration for creation session
            {
                print("BehaviourMode.Consuming #RegisterEngagedAnchor");
                ActiveAnchorManager.RegisterEngagedAnchor(index);
            }
        }


        public void OnUnengaged()
        {
            if (!isEngaged)
                return;
            else
                isEngaged = false;
            
            Reset4AutoReaction();
            
            //clear the focused anchor
            if (GetBehaviourMode() == BehaviourMode.Consuming) //don't do registration for creation session
            {
                ActiveAnchorManager.ClearCurrentAnchor();
            }
            
            //update layout
            activityManager.SetLayout(ActivityLayout.Hidden);
        }

        
        //dynamically according to player (for engagement)
        private void EnableAutoReaction(bool enable)
        {
            autoReaction = enable;
            if(enable)
                Reset4AutoReaction();
        }
        

        private void Reset4AutoReaction()
        {
            //the default state for dynamicReaction
            enableRotation = true;
            watchingPlayer = false;
            focusEffect.SetActive(false);
        }

        
        //------------------------------------------ Born/Reborn ----------------------------------------------

        public void Born(int index, AnchorInfo anchorInfo, Action postAction = null)
        {
            print($"AnchorController.Born: started. isPlanning={SBContextManager.Instance.context.isPlanning} | index={index} #lightship #debugvpsedit");
            isReborn = false;
             
            //init anchor pose
            //Init(index, anchorInfo, arSpace);
            Init(index, anchorInfo);
            
            //set mode
            SetUIMode(AnchorController.UIMode.Creating_Spawned);
            SetBehaviourMode(AnchorController.BehaviourMode.Creating_Spawned);

            //spawn animation
            if (SBContextManager.Instance.context.isPlanning)
            {
                PlayBornAnimation4Planning(() =>
                {
                    //ask activityManager to handle the born of activities
                    activityManager.InitActivities();
                    
                    activityPanelsParent.SetActive(true);
                    planningTopPanel.SetActive(true);
                    activityManager.SetLayout(ActivityLayout.Hidden);

                    //update anchor post and bottom plate pose
                    lockerPlate.SetActive(false);
                   
                    //update mini map
                    // MiniMapManager.Instance.AddAnchorInMiniMap(transform.position);
                    
                    postAction?.Invoke();
                });
            }
            else
            {
                PlayBornAnimation(() =>
                {
                    //ask activityManager to handle the born of activities
                    activityManager.InitActivities();

                    //update anchor post and bottom plate pose
                    lockerPlate.SetActive(true);
                    
                    //update mini map
                    // MiniMapManager.Instance.AddAnchorInMiniMap(transform.position);
                    postAction?.Invoke();
                });
            }
            
            if (SBContextManager.Instance.context.IsCreatingInConsume()) {
                // Remove the Trivia icon from the anchor
                activityButtonsController.SetCreateInConsumeButtons();
            }
        }
          
        public void Reborn(int index, AnchorInfo anchorInfo, Action postAction)
        {
            print($"AnchorController.Reborn: started. 10 | activities={anchorInfo.activityInfoList.Count} | index={index} #debugvpsedit");
            isReborn = true;
            quickPhotoWithPostWasCompleted = true;
            
            Init(index, anchorInfo); //Init(index, anchorInfo, arSpace);
            //Add ARKit anchor
            //AddARAnchor(anchorInfo.pose.position);

            var anchorActivityInfo = new AnchorActivityInfo()
            {
                postInfo = anchorInfo.postInfo,               
                activityInfoList =  anchorInfo.activityInfoList
            };

            //set modes for anchor and activities
            print($"AnchorController.Reborn: isCreating? {SBContextManager.Instance.IsCreating()}");
            if (SBContextManager.Instance.IsCreating())
            {
                if (SBContextManager.Instance.context.isPlanning) //if it's planning
                {
                    SetUIMode(AnchorController.UIMode.Creating_ReadyToAddActivities);
                    SetBehaviourMode(AnchorController.BehaviourMode.Creating_Spawned);
                    
                    PlayBornAnimation4Planning(() =>
                    {
                        // //ask activityManager to handle the born of activities
                        // activityManager.InitActivities();
                        //
                        // activityPanelsParent.SetActive(true);
                        // planningTopPanel.SetActive(true);
                        // activityManager.SetLayout(ActivityLayout.Hidden);
                        //
                        // //update anchor post and bottom plate pose
                        // lockerPlate.SetActive(false);
                        // handleDragger.UpdateAnchorRotation(plane);

                        activityManager.RebornActivities(anchorActivityInfo);
                        activityPanelsParent.SetActive(true);
                        planningTopPanel.SetActive(true);
                        activityManager.SetLayout(ActivityLayout.HorizontalEdit);
                        
                        //1. Update activity panel layout
                        if (!ActiveAnchorManager.IsTheClosestAnchor(this.index))
                        {
                            activityManager.SetLayout(ActivityLayout.Hidden, true, postAction);
                        }

                        //2. update buttons on anchor
                        print($"expand the activity buttons by default");
                        ToggleMoreLessButton(true);

                        //auto-select the POI button. We use false here as it will be negated inside the function.                
                        isPoI = activityManager.HasCheckIn();
                        DoSelectPoI(isPoI, true);
                        if (isPoI)
                        {
                            DeSelectAllActivityTypes();
                        }
                    });
                }
                else //if it's edit
                {
                    print($"AnchorController.Reborn: Edit Mode");
                    SetUIMode(AnchorController.UIMode.Creating_ReadyToAddActivities);
                    SetBehaviourMode(AnchorController.BehaviourMode.Consuming);
 
                    //spawn animation
                    PlayBornAnimation(() =>
                    {
                        //Below UI update should be executed after the animation!
                        
                        //ask activityManager to handle the born of activities
                        activityManager.RebornActivities(anchorActivityInfo);
                    
                        //1. Update activity panel layout
                        // if (!AnchorManager.Instance.IsTheClosestAnchor(this.index))
                        // {
                        float distanceToCam = Vector3.Distance(transform.position, mainCam.transform.position);
                        if (distanceToCam > Const.DISTANCE_TO_ENGAGE_ANCHOR)
                        {
                            activityManager.SetLayout(ActivityLayout.Hidden, true, postAction);    
                        }
                        // }
                        
                        //2. update buttons on anchor
                        print($"expand the activity buttons by default");
                        ToggleMoreLessButton(true);
                    
                        //auto-select the POI button. We use false here as it will be negated inside the function.                
                        isPoI = activityManager.HasCheckIn();
                        DoSelectPoI(isPoI, true);
                        if (isPoI)
                        {
                            DeSelectAllActivityTypes();
                        }
                        
                        postAction?.Invoke();
                    });
                }

                if (SBContextManager.Instance.context.IsCreatingInConsume()) {
                    // Remove the Trivia icon from the anchor
                    activityButtonsController.SetCreateInConsumeButtons();
                }
            }
            else //if it's purely a consumer
            {
                SetUIMode(AnchorController.UIMode.Consuming);
                SetBehaviourMode(AnchorController.BehaviourMode.Consuming);
                
                // //ask activityManager to handle the born of activities
                // activityManager.RebornActivities(anchorActivityInfo, anchorInfo.pose);
                
                PlayBornAnimation(() =>
                {
                    //Below UI update should be executed after the animation!
                    //ask activityManager to handle the born of activities
                    activityManager.RebornActivities(anchorActivityInfo);

                    //Update activity panel layout after the anchor born animation
                    if (!ActiveAnchorManager.IsTheClosestAnchor(this.index))
                    {
                        activityManager.SetLayout(ActivityLayout.Hidden, true, postAction);
                    }

                    if(activityManager.ActivitiesCompleted) {
                        MiniMapManager.Instance.SetGreenPoint(anchorInfo.id);
                    }
                    
                    postAction?.Invoke();
                });
            }
            
            anchorWasCompleted = true;

            MiniMapManager.Instance.ShowMiniMap();
            if(!SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
                MiniMapManager.Instance.AddExistConsumeAnchor(transform.position, anchorInfo.id);
            }
        }
          
        private void Init(int index, AnchorInfo anchorInfo)
        {
            print($"AnchorController.Init: started. position={anchorInfo.pose.position} #anchorposition #touchposition");
            //set values
            this.index = index;
            this.anchorInfo = anchorInfo;
            
            //set the initial pose and scale
            // transform.parent = transform;
            // transform.localPosition = anchorInfo.pose.position;
            // transform.localRotation = anchorInfo.pose.rotation;
            var transform1 = transform;
            transform1.position = anchorInfo.pose.position;
            transform1.rotation = anchorInfo.pose.rotation;
            
            transform1.localScale = Const.ANCHOR_SCALE * 0.01f; //1/100 of the normal size

            planningBottom.SetActive(false);
            upbody.SetActive(false);
        }

        private void PlayBornAnimation(Action postAction)
        {
            upbody.SetActive(true);

            //if (SBContextManager.Instance.context.isEditing && SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
            transform.DOScale(Const.ANCHOR_SCALE, 0.75f).SetEase(Ease.OutBounce).OnComplete(() =>
            {
                scaleCompleted = true;
                postAction?.Invoke();
                isBornCompleted = true;
            });  
        }
        

        private void PlayBornAnimation4Planning(Action postAction)
        {
            //transform.localScale = Const.ANCHOR_SCALE;
            upbody.transform.localPosition = new Vector3(0, -550, 0);
            upbody.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            
            //play scale (bottom) animation
            planningBottom.SetActive(true);
            transform.DOScale(Const.ANCHOR_SCALE, 0.75f).SetEase(Ease.OutQuart).OnComplete(() =>
            {
                scaleCompleted = true;
                
                //play fade-in bottom animation
                FadeInPlanningBottom(() =>
                {
                    upbody.SetActive(true);
                
                    //play grew animation
                    upbody.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBounce);
                    upbody.transform.DOLocalMove(Vector3.zero, 1f).SetEase(Ease.OutBounce).OnComplete(() =>
                    {
                        planningBottom.GetComponentInChildren<ObjectDragger>().enabled = true;
                        postAction.Invoke();
                        
                        isBornCompleted = true;
                    });
                });
            });
        }


        private IEnumerator fadeInPlanningBottomThread;
        public void FadeInPlanningBottom(Action postAction)
        {
            if (fadeInPlanningBottomThread != null)
            {
                return;
                //StopCoroutine(fadeInPlanningBottomThread);
            }
            fadeInPlanningBottomThread = FadeInAlpha(planningBottom, 0, 1, postAction);
            StartCoroutine(fadeInPlanningBottomThread);
        }
        
        
        IEnumerator FadeInAlpha(GameObject uiObj, float initAlpha = -1, float fadeTime = -1, Action postAction = null)
        {
            //safe check
            if (uiObj == null)
                yield return null;
            CanvasGroup canvasGroup = uiObj.GetComponent<CanvasGroup>();
            if(canvasGroup == null)
                yield return null;

            canvasGroup.alpha = (initAlpha == -1 ? 0 : initAlpha);
            yield return new WaitForSeconds(0.1f);
            
            //fade in
            float finalFadeTime = fadeTime == -1 ? 1f : fadeTime;
            while (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.deltaTime / finalFadeTime;
                yield return null;
            }

            //done
            fadeInPlanningBottomThread = null;
            canvasGroup.interactable = true;
            yield return null;
            
            postAction?.Invoke();
        }


        // public void CheckNormnal()
        // {
        //     Vector3 groundNormal = PlaneManager.Instance.GetGroundNormal();
        //
        //     float angle = -1;
        //     if (groundNormal != Vector3.zero)
        //     {
        //         angle = Vector3.Angle(groundNormal, transform.up);
        //         print(string.Format(
        //             "AnchorNormal=\'{0}\', groundNormal=\'{1}\', angle=\'{2}\'", transform.up, groundNormal, angle));
        //     }
        //     else
        //     {
        //         print("Couldn't find goundNormal");    
        //     }
        //
        //     //fix it
        //     transform.up = groundNormal;
        // }


        //----------------------------------------- UI mode or events ------------------------------------------

        public void EnableRotation(bool enabled)
        {
            enableRotation = enabled;
        }
        public void EnableDragging(bool enabled)
        {
            handleDragger.enabled = enabled;
        }
        public void EnableWatchingPlayer(bool enabled)
        {
            watchingPlayer = enabled;
        }


        public void SetUIMode(AnchorController.UIMode mode)
        {
            print("#mutualElements 2");
            //set UIMode for the anchor object
            currUIMode = mode;
            ApplyUIMode();
            
            //set UIMode for activity panels, currently no need
            //activityManager.SetUIMode(mode);
        }
        public AnchorController.UIMode GetUIMode()
        {
            return currUIMode;
        }


        private void SetAnchorObjColor(Material  material)
        {
            outter.GetComponent<MeshRenderer>().material = material;
            core.GetComponent<MeshRenderer>().material = material;
            support.GetComponent<MeshRenderer>().material = material;
        }


        /// <summary>
        /// Set the interactable for the anchor body
        /// </summary>
        /// <param name="interactable"></param>
        public void SetAnchorBodyInteractable(bool interactable)
        {
            Utilities.SetCanvasGroupInteractable(gameObject, interactable);
        }


        /// <summary>
        /// Set the interactable for the whole anchor including the body and the activities
        /// </summary>
        /// <param name="interactable"></param>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            
            SetAnchorBodyInteractable(interactable); //for the anchor body
            activityManager.SetInteractable(interactable); //for all the activities    
        }

        
        //------------------------------ planning, begin ----------------------------------
        
        
        [SerializeField] private GameObject planningTopPanel;
        [SerializeField] private Text planningTopTitle;
        [SerializeField] private Text planningTopRatingText;
        [SerializeField] private Text planningTopLocationText;
        [SerializeField] private RawImage planningThumbnail;
        
        
        private void InitPlanningTopPanel()
        {
            MapLocationInfo locInfo = SBContextManager.Instance.context.plannedLocation;

            if (locInfo == null)
            {
                Debug.Log("Error: no data from \'SBContextManager.Instance.context.plannedLocation\'");
                return;
            }

            planningTopTitle.text = locInfo.Name;
            planningTopRatingText.text = locInfo.Rating.ToString();
            planningTopLocationText.text = locInfo.FormattedAddress;

            InitPlanningThumbnail();
        }
        
        
        private void InitPlanningThumbnail()
        {
            if (!this.planningThumbnail && SBContextManager.Instance.context.plannedLocation.Thumbnail == null)
                return;

            StartCoroutine(LoadPlanningThumbnailImage4Top(planningThumbnail));
        }
        
        
        // IEnumerator LoadPlanningThumbnailImage4Top (RawImage rawImage)
        // {
        //     string thumbnailPath = SBContextManager.Instance.context.plannedLocation.Thumbnail;
        //     if (SBContextManager.Instance.context.isEditing)
        //     {                
        //         var parts = thumbnailPath.Split('/');
        //         thumbnailPath = thumbnailPath.EndsWith(".jpg")
        //             ? $"{Application.persistentDataPath}/{parts[parts.Length - 2]}/{parts[parts.Length - 1]}"
        //             : $"{Application.persistentDataPath}/{parts[parts.Length - 1]}";
        //     }
        //     print($"#gml thumbnailPath: {thumbnailPath}");
        //     
        //     //retrieve from local first
        //     WWW www = new WWW (thumbnailPath);
        //     while(!www.isDone)
        //         yield return null;
        //
        //     Texture2D t = www.texture;
        //     if (t != null)
        //     {
        //         float newW = t.height * 180 / 225; //the ratio of the rawimage
        //         Texture2D squareT = Utilities.ScaleTextureCutOut(t,(t.width - newW)/2, 0, newW, t.height);
        //         
        //         rawImage.texture = squareT;
        //     }
        // }

        
        IEnumerator LoadPlanningThumbnailImage4Top(RawImage rawImage) 
        {
            string thumbnailPath = SBContextManager.Instance.context.plannedLocation.Thumbnail;
            if (SBContextManager.Instance.context.isEditing)
            {                
                var parts = thumbnailPath.Split('/');
                thumbnailPath = thumbnailPath.EndsWith(".jpg")
                    ? $"{Application.persistentDataPath}/{parts[parts.Length - 2]}/{parts[parts.Length - 1]}"
                    : $"{Application.persistentDataPath}/{parts[parts.Length - 1]}";

            }
            print($"#gml thumbnailPath: {thumbnailPath}");
            
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(thumbnailPath);
            yield return www.SendWebRequest();

            if(www.isNetworkError || www.isHttpError) 
            {
                Debug.Log(www.error);
                defaultThumbnailBeeIcon.SetActive(true);
                rawImage.color = defaultThumbnailBackgroundColor;
            }
            else 
            {
                Texture2D t = ((DownloadHandlerTexture)www.downloadHandler).texture;
                if (t != null)
                {
                    float newW = t.height * 180 / 225; //the ratio of the rawimage
                    Texture2D squareT = Utilities.ScaleTextureCutOut(t,(t.width - newW)/2, 0, newW, t.height);
                
                    defaultThumbnailBeeIcon.SetActive(false);
                    rawImage.color = Color.white;
                    rawImage.texture = squareT;
                }
                else
                {
                    Debug.Log("Error: Still could not retrieve texture for thumbnail image, check the URL!");
                    defaultThumbnailBeeIcon.SetActive(true);
                    rawImage.color = defaultThumbnailBackgroundColor;
                }
            }
        }

        [SerializeField] private GameObject defaultThumbnailBeeIcon;
        private Color defaultThumbnailBackgroundColor = new Color32(244, 149, 99, 255);
        
        
        private void InitPlanningMapBottom()
        {
            if (!this.planningMapViewImage || SBContextManager.Instance.context.plannedLocation == null)
                return;
            
            StartCoroutine(LoadPlanningMapImage4Bottom(planningMapViewImage));
        }
        
        
        // IEnumerator LoadPlanningMapImage4Bottom (RawImage rawImage)
        // {
        //     string imageLocalPath = SBContextManager.Instance.context.plannedLocation.Map3DLocalIdentifier;
        //     string imageServerPath = SBContextManager.Instance.context.plannedLocation.Map3DUrl;
        //     Debug.Log($"LoadPlanningMapImage4Bottom: imageLocalPath='{imageLocalPath}'");
        //     Debug.Log($"LoadPlanningMapImage4Bottom: imageServerPath='{imageServerPath}'");
        //     
        //     //retrieve from local first
        //     WWW www = new WWW (imageLocalPath);
        //     while(!www.isDone)
        //         yield return null;
        //
        //     //get from server if not retrieved locally
        //     if (www.texture == null)
        //     {
        //         www = new WWW (imageServerPath);
        //         while(!www.isDone)
        //             yield return null;
        //     }
        //     
        //     Texture2D t = www.texture;
        //     Texture2D squareT = Utilities.ScaleTextureCutOut(t, 0, (t.height - t.width)/2, t.width, t.width);
        //
        //     rawImage.texture = squareT;
        // }
        
        
        IEnumerator LoadPlanningMapImage4Bottom (RawImage rawImage)
        {
            string imageLocalPath = SBContextManager.Instance.context.plannedLocation.Map3DLocalIdentifier;
            string imageServerPath = SBContextManager.Instance.context.plannedLocation.Map3DUrl;
            Debug.Log($"LoadPlanningMapImage4Bottom: imageLocalPath='{imageLocalPath}'");
            Debug.Log($"LoadPlanningMapImage4Bottom: imageServerPath='{imageServerPath}'");
            
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageLocalPath);
            yield return www.SendWebRequest();

            if(www.isNetworkError || www.isHttpError) 
            {
                Debug.Log(www.error);
            }
            else 
            {
                Texture2D t = ((DownloadHandlerTexture)www.downloadHandler).texture;
                if (t == null)
                {
                    www = UnityWebRequestTexture.GetTexture(imageServerPath);
                    yield return www.SendWebRequest();
                    
                    if(www.isNetworkError || www.isHttpError) 
                    {
                        Debug.Log(www.error);
                    }
                    else
                    {
                        t = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    }
                }

                if (t != null)
                {
                    Texture2D squareT = Utilities.ScaleTextureCutOut(t, 0, (t.height - t.width)/2, t.width, t.width);
                    rawImage.texture = squareT;    
                }
                else
                {
                    Debug.Log("Error: Still could not retrieve texture for bottom map image, check the URL!");
                }
            }
        }
        

        //------------------------------ planning, end ----------------------------------

        
        private void ApplyUIMode()
        {
            print(string.Format("AnchorObject UIMode -> '{0}'", currUIMode.ToString()));            
            DisableAllManagedComponents();
            switch(currUIMode)
            {
                case AnchorController.UIMode.Creating_Spawned:
                    EnableToggleEars = false;
                    EnableDragging(true);
                    
                    editButtons.SetActive(true);
                    deleteButton.SetActive(true);
                    
                    coverDragging.SetActive(true);
                    SetAnchorObjColor(matPurple);
                    
                    lockerPlate.SetActive(false); LockAnchor(false);
                    if (SBContextManager.Instance.context.isPlanning)
                    {
                        //1. init planningMapBottom
                        InitPlanningMapBottom(); //heavy
                        this.planningBottom.SetActive(true);
                        this.planningBottom.GetComponent<CanvasGroup>().alpha = 0;
                        this.planningBottom.GetComponent<CanvasGroup>().interactable = false;
                        planningBottom.GetComponentInChildren<ObjectDragger>().enabled = false;
                        draggingCoverText.text = "Tap to Edit";
                        
                        //2. init planningTopPanel
                        InitPlanningTopPanel();
                    }
                    else
                        draggingCoverText.text = "Tap to Lock";
                    
                    break;
                
                case AnchorController.UIMode.Creating_Scanning:
                    EnableToggleEars = false;
                    EnableDragging(false);

                    coverScanning.SetActive(true);
                    SetAnchorObjColor(matPurple);
                    break;
                
                case AnchorController.UIMode.Creating_EditingPost:
                    EnableToggleEars = false;
                    EnableDragging(false);

                    editButtons.SetActive(true); 
                    deleteButton.SetActive(true);
                    
                    activityPanelsParent.SetActive(true);

                    coverDefault.SetActive(true);
                    SetAnchorObjColor(matPurple);
                    
                    lockerPlate.SetActive(false); LockAnchor(true);
                    EnableEditButtons();
                    
                    if (SBContextManager.Instance.context.isPlanning)
                    {
                        activityManager.BornPostActivity();
                    }
                    activityManager.SetLayout(ActivityLayout.HorizontalEdit, false, () =>
                    {
                        PostActivity postActivity = GetComponentInChildren<PostActivity>();
                        postActivity.OnEditTitle();
                    });
                    
                    break;
                
                case AnchorController.UIMode.Creating_ReadyToAddActivities:
                    EnableToggleEars = false;
                    EnableDragging(false);
                    activityPanelsParent.SetActive(true);
                    
                    editButtons.SetActive(true); 
                    moreButton.SetActive(true);
                    deleteButton.SetActive(true);

                    lockerPlate.SetActive(false); LockAnchor(true);
                    
                    coverDefault.SetActive(true);
                    SetAnchorObjColor(matPurple);
     
                    if (SBContextManager.Instance.context.isEditing && SBContextManager.Instance.context.isPlanning)
                    {
                        //1. init planningMapBottom
                        InitPlanningMapBottom(); //heavy
                        this.planningBottom.SetActive(true);
                        this.planningBottom.GetComponent<CanvasGroup>().alpha = 0;
                        this.planningBottom.GetComponent<CanvasGroup>().interactable = false;
                        planningBottom.GetComponentInChildren<ObjectDragger>().enabled = false;
                        //draggingCoverText.text = "Tap to Edit";
                        
                        //2. init planningTopPanel
                        InitPlanningTopPanel();
                    }
                    else
                        //draggingCoverText.text = "Tap to Lock";

                    // PointsBarManager.Instance.ShowPointsBar();
                    MiniMapManager.Instance.ShowMiniMap();
                    break;
                
                case AnchorController.UIMode.Creating_Done:
                    EnableToggleEars = true;
                    EnableDragging(false);
                    activityPanelsParent.SetActive(true);
                    
                    editButtons.SetActive(false); 
                    lockerPlate.SetActive(false); 
                    
                    coverDefault.SetActive(true);
                    SetAnchorObjColor(matPurple);
                    
                    break;
                
                case AnchorController.UIMode.Consuming:
                    EnableToggleEars = false;
                    EnableDragging(false);
                    activityPanelsParent.SetActive(true);
                    
                    editButtons.SetActive(false); 
                    lockerPlate.SetActive(false); LockAnchor(true);

                    coverDefault.SetActive(true);
                    SetAnchorObjColor(matPurple);
                    break;

                case AnchorController.UIMode.Creating_PhotoVideo:
                    EnableToggleEars = false;
                    EnableDragging(false);
                    
                    editButtons.SetActive(true); 
                    deleteButton.SetActive(true);
                    
                    activityPanelsParent.SetActive(true);

                    coverDefault.SetActive(true);
                    SetAnchorObjColor(matPurple);
                    
                    lockerPlate.SetActive(false); LockAnchor(true);
                    selectedActivityType = ActivityType.PhotoVideo;
                    CreateSelectedActivity();
                    
                    if (SBContextManager.Instance.context.isPlanning)
                    {
                        InitPlanningTopPanel();
                        planningTopPanel.SetActive(true);
                        
                        activityManager.SetLayout(ActivityLayout.HorizontalEdit);
                    }
                    break;
            }
        }

        void EnableEditButtons() {
            //reset activity type buttons selection
            DeSelectAllActivityTypes();
            UpdatePoIButton();                      
            //It is important that we set this here after all button state have been updated.
            selectedActivityType = ActivityType.Undefined;
            
            //expand the activity buttons by default
            ToggleMoreLessButton(true);  
        }
        
        
        private void DisableAllManagedComponents()
        {
            // print($"mutualElements is null? {mutualElements==null}");
            foreach (GameObject component in mutualElements)
            {
                // print($"mutualElements > component is null? {component==null}");
                if(component)
                    component.SetActive(false);
            }

            // if (SBContextManager.Instance.IsCreating()) {
            //     PointsBarManager.Instance.HidePointsBar();
            // }
            // print($"mutualElements > MiniMapManager.Instance.HideMiniMap");
            MiniMapManager.Instance.HideMiniMap();
        }
        
        
        //--------------------------------- Behaviour mode ----------------------------------


        public void SetBehaviourMode(BehaviourMode mode)
        {
            currBehaviourMode = mode;
            ApplyBehaviourMode();
        }
        public BehaviourMode GetBehaviourMode()
        {
            return currBehaviourMode;
        }


        private void ApplyBehaviourMode()
        {
            print(string.Format("AnchorObject BehaviourMode -> '{0}'", currBehaviourMode.ToString()));            
            switch (currBehaviourMode)
            {
                case BehaviourMode.Creating_Spawned:
                    EnableAutoReaction(false);
                    EnableRotation(false);
                    EnableWatchingPlayer(true);
                    break;

                case BehaviourMode.Creating_Scanning:
                    EnableAutoReaction(false);
                    EnableRotation(false);
                    EnableWatchingPlayer(false); //during scanning, we set anchor still.
                    break;
                
                case BehaviourMode.Creating_SettingActivities:
                    EnableAutoReaction(false);
                    EnableRotation(false);
                    EnableWatchingPlayer(true);
                    break;

                case BehaviourMode.Creating_Done:
                    EnableAutoReaction(true);
                    break;
                
                case BehaviourMode.Consuming:
                    EnableAutoReaction(true);
                    break;
            }
        }

        public void StopRotate(bool stop) {
            //anchorObj.transform.localRotation = this.anchorBodyInitialRotation;
            enableRotation = !stop;
            watchingPlayer = stop;
            if(stop)
                ResetBodyPose();
        }

        //------------------------------------ Events ----------------------------------------
 
        //--------------- Handle edit/delete buttons---------------
        private bool EnableToggleEars = false;
        public void ToggleEditButtons()
        {
            if (EnableToggleEars)
            {
                if (editButtons.activeSelf)
                    TurnOffButtons();
                else
                    TurnOnButtons();
            }
        }


        public void TurnOnButtons()
        {
            editButtons.SetActive(true);
            moreButton.SetActive(true);
            deleteButton.SetActive(true);
        }

        
        public void TurnOffButtons()
        {
            editButtons.SetActive(false);
            moreButton.SetActive(false);
            deleteButton.SetActive(false);
        }
        

        //------------------ delete buttons --------------
        public void OnDeleteButtonClick()
        {
            //register as focus
            ActiveAnchorManager.RegisterEngagedAnchor(index);
            
            //pop-up confirmation dialog before deleting
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.DeletingAnchor);
            ActiveAnchorManager.SetInteractable(true);
        }
        

        public void DoDeleteAnchor(Action postAction = null)
        {
            print(string.Format("Delete button of anchor index '{0}' is clicked!", index));
            
            // PlaneManager.Instance.SetARPlanesVisible(false);
            ARHelper.Instance.StopPlaneDetection();
            ActiveAnchorManager.SetInteractable(false); //disable interaction for ALL anchors (including their body and acticities)
            GPSAnchorManager.Instance.SetInteractable(false); //disable interaction for all GPS anchors
            
            BottomPanelManager.Instance.ShowMessagePanel("Deleting activities...", true, false, () =>
            {
                SBRestClient.Instance.DeleteActivity(SBContextManager.Instance.context.experienceId, anchorInfo.id, (error)=>
                {
                    ActiveAnchorManager.SetInteractable(true, index); //re-enable interaction for ALL anchors (including their body and acticities)
                    GPSAnchorManager.Instance.SetInteractable(true, index); //re-enable interaction for all GPS anchors
                    if (error!=null)
                    {
                        BottomPanelManager.Instance.ShowMessagePanel(error.Message);
                        return;
                    }
                    else
                    {
                        BottomPanelManager.Instance.ShowMessagePanel("Activities deleted...", false, true, () =>
                        {
                            NativeCall.Instance.OnAnchorDeleted(anchorInfo.id);
                            if (index >= 0)
                            {
                                ActiveAnchorManager.DeleteAnchorObject(index);
                            }
                                                                                    
                            if (ActiveAnchorManager.AnchorCount < 1 && SBContextManager.Instance.context.isEditing)
                            {
                                InteractionManager.Instance.DoBackToNative(true);
                                return;
                            }
                            
                            if (postAction != null) {
                                postAction.Invoke();
                                return;
                            }

                            // If there are no more anchors then we do not need to do the following.
                            // We currently do not support extending the map and so we cannot create a new anchor.
                            // Also, the current code has a bug anyway. After the "+" icon is tapped, scanning is stuck at 0%.
                            // Again, that is because we do not support extending the map.
                            if (ActiveAnchorManager.IsReadyToSaveMap())
                            {
                                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextAnchorOrSaveMap);
                            }
                            else
                            {
                                if (SBContextManager.Instance.IsEditCreating())
                                {
                                    if (SBContextManager.Instance.context.isPlanning)
                                    {
                                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent
                                            .CompletePlanning);
                                    }

                                    if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
                                    {
                                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent
                                            .NextOrCompleteGPSOnlyCreation);
                                    }
                                    else
                                    {
                                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.FinishEdit);
                                    }
                                }
                                else
                                {
                                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextAnchor);
                                }
                            }
                        });
                    }
                });
            });
        }


        //------------------- Anchor lock/unlock ---------------------

        public void Tap2LockOrEdit()
        {
            if (SBContextManager.Instance.context.isPlanning)
            {
                GoPlanningWithoutLockingAnchor();
            }
            else
            {
                LockAnchorWithAnimation(true);
            }
        }


        private void GoPlanningWithoutLockingAnchor()
        {
            //update anchor's pose with its locked pose
            anchorInfo.pose = new Pose(transform.position, transform.rotation);
                
            //retrieve GPS location from GPS asset, based on anchor's position
            anchorInfo.locationInfo = new ARLocation.Location(anchorInfo.postInfo.MapLocation.Latitude,
                anchorInfo.postInfo.MapLocation.Longitude, anchorInfo.postInfo.MapLocation.Altitude);
                
            //proceed to edit!
            InteractionManager.Instance.OnScanningSkiped();
        }
        
        
        /// <summary>
        /// Animation of moving the 'lock'
        /// </summary>
        /// <param name="toLock">true: lock to move down, false: lock to move up</param>
        public void LockAnchorWithAnimation(bool toLock)
        {
            if (toLock)
            {
                //update anchor's pose with its locked pose
                anchorInfo.pose = new Pose(transform.position, transform.rotation);
                
                //retrieve GPS location from GPS asset, based on anchor's position
                // LocationInfo tempLocInfo = Input.location.lastData;
                // MessageManager.Instance.DebugMessage($">>>>>>> [DEVICE] Location:'{tempLocInfo.latitude}' - '{tempLocInfo.longitude}' / '{tempLocInfo.altitude}'");
                anchorInfo.locationInfo = ARLocationManager.Instance.GetLocationForWorldPosition(transform.position);
                //MessageManager.Instance.DebugMessage($">>>>>>> [ANCHOR] Location:'{anchorInfo.locationInfo.Latitude}' - '{anchorInfo.locationInfo.Longitude}' / '{anchorInfo.locationInfo.Altitude}'");
                print($">>>>>>> [ANCHOR] Location:'{anchorInfo.locationInfo.Latitude}' - '{anchorInfo.locationInfo.Longitude}' / '{anchorInfo.locationInfo.Altitude}'");
                
                
                
                StartCoroutine(GetLocationInfo(anchorInfo.locationInfo.Latitude, anchorInfo.locationInfo.Longitude));

                anchorLockButton.transform.DOMove(anchorUnLockButton.transform.position, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    // anchorLockButton.SetActive(false);
                    LockAnchor(true);
                
                    //trigger action
                    if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AnchorLockTappedGPSOnly);
                    else
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AnchorLockTapped);
                });
            }
            else
            {
                //reset anchor lock position
                anchorLockButton.transform.DOLocalMove(lockOriginLocalPos, 0.15f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    LockAnchor(false);
                });
            }
        }

        IEnumerator GetLocationInfo(double latitude, double longitude)
        {
            print($"AnchorController.GetLocationInfo: coord={latitude},{longitude}");
            
            InteractionManager.Instance.OnWillGetLocationInfo(latitude, longitude);
            yield return null;
        }

        private void LockAnchor(bool toLock)
        {
            anchorLockButton.SetActive(!toLock);
            anchorUnLockButton.SetActive(toLock);
        }

        //------------------- Select activity type options -------------------

        public void ToggleMoreLessButton(bool expand) //toggle more/less
        {
            print(string.Format("More/Less button of anchor index '{0}' clicked!", index));
            
            if (expand)
            {
                //rotate out
                activityButtonsController.CheckingVisibility(true);
                activityButtonParent.transform.localRotation = Quaternion.Euler(activityButtonsHiddenPose);
                activityButtonParent.SetActive(true);
                activityButtonParent.transform.DOLocalRotate(activityButtonsExpandedPose, 0.75f, RotateMode.Fast).SetEase(Ease.InOutBack).OnComplete(() => {
                    PostToggleMostLessButton();
                    activityButtonsController.CheckingVisibility(false);
                });
            }
            else
            {
                DeSelectAllActivityTypes();
                
                //rotate in
                activityButtonsController.CheckingVisibility(true);
                activityButtonParent.transform.localRotation = Quaternion.Euler(activityButtonsExpandedPose);
                activityButtonParent.SetActive(true);
                activityButtonParent.transform.DOLocalRotate(activityButtonsHiddenPose, 0.75f, RotateMode.Fast).SetEase(Ease.InOutBack).OnComplete(() => {
                    activityButtonParent.SetActive(false);        
                    PostToggleMostLessButton();
                    activityButtonsController.CheckingVisibility(false);
                });
            }
        }

        
        private void PostToggleMostLessButton()
        {
            moreButton.SetActive(!activityButtonParent.activeSelf); //update more
            lessButton.SetActive(activityButtonParent.activeSelf); //update less
        }


        public void RegisterEngagedAnchor()
        {
            //light-weight register
            print("AnchorController > RegisterEngagedAnchor #debugconsume ");
            ActiveAnchorManager.RegisterEngagedAnchor(index);
        }


        private ActivityType selectedActivityType;
        private void SelectActivity(ActivityType type, GameObject typeButtonObj)
        {
            print("SelectActivity: ActivityType...");
            if (selectedActivityType == type)
            {                
                DeSelectAllActivityTypes(); //disable all circles
                selectedActivityType = ActivityType.Undefined;
            }
            else
            {                            
                //update UI
                DeSelectAllActivityTypes(); //disable all circles
                SetActivityButtonSelected(typeButtonObj, true); //enable circle
                selectedActivityType = type;
            }
            
            // if(SBContextManager.Instance.IsEditCreating()) {
            //     if (selectedActivityType == ActivityType.Undefined) {
            //         StopRotate(false);
            //     }
            //     else {
            //         AnchorManager.Instance.RotateAllAnchors();
            //         StopRotate(true);
            //     }
            // }
            
            if (activityManager.IsAnyActivityCreated() && activityManager.HasAlreadyCreatedThisType(type))
            {
                BottomPanelManager.Instance.ShowMessagePanel("You can only create one copy of this activity.");
                coverCreate.SetActive(false);
                return;
            }
            //update 'create' button
            CheckEnableCreateButton();
        }

        
        private void DeSelectAllActivityTypes()
        {
            print("DeSelectAllActivityTypes");
            //SetActivityButtonSelected(buttonPoI, false); 
            SetActivityButtonSelected(buttonTrivia, false);
            SetActivityButtonSelected(buttonPhotoVideo, false);
            SetActivityButtonSelected(buttonAudio, false);
        }
        
        
        private void SetActivityButtonSelected(GameObject buttonObj, bool selected)
        {
            print($"SetActivityButtonSelected: button={buttonObj.name} | selected={selected}");
            ActivityButton activityButton = buttonObj.GetComponent<ActivityButton>();
            
            //circle visible/invisible
            activityButton.enabledObj.transform.GetChild(1).gameObject.SetActive(selected);
        }

        
        private void CheckEnableCreateButton()
        {
            if (selectedActivityType != ActivityType.Undefined)
            {
                coverCreate.SetActive(true);
                SetAnchorObjColor(matOrange);
            }
            else
            {
                coverCreate.SetActive(false);
                if (SBContextManager.Instance.IsEditCreating() || !wasPostSaved) {
                    SetAnchorObjColor(matPurple);
                }
                else {
                    SetAnchorObjColor(matWhite);
                }
            }
        }

        void DoSelectPoI(bool isPoi, bool skipApi)
        {            
            var message = isPoi ? "Marking as a point of interest..." : "Unmarking as a point of interest...";
            print(message);
            
            if (!skipApi)
            {
                //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction(message, false));
                BottomPanelManager.Instance.ShowMessagePanel(message);

                var post = activityManager.GetPostActivity();
                if (post != null)
                {
                    post.OnMarkAsCheckIn(isPoi);
                }
            }

            if(!isReborn) {
                print($"Toggling the POI button: {isPoi}");
                
                //handle other activity buttons
                SetActivityButtonSelected(buttonPoI, isPoi);
                buttonPoI.GetComponent<ActivityButton>().SetEnabled(isPoi);
                buttonPoI.GetComponent<ActivityButton>().SetExisting(isPoi);

                print($"Toggling the non-POI buttons: {!isPoi}");
                buttonTrivia.GetComponent<ActivityButton>().SetEnabled(!isPoi);
                buttonPhotoVideo.GetComponent<ActivityButton>().SetEnabled(!isPoi);
                buttonAudio.GetComponent<ActivityButton>().SetEnabled(!isPoi);
                
                //handle the cover
                coverCreate.SetActive(false);
                coverToComplete.SetActive(true);
                SetAnchorObjColor(matWhite);
            }
            else {
                StartCoroutine(SetPoiAfterReborn(isPoi));
            }
        }

        public void SetPoiAfterDelay(bool isPoi) {
            StartCoroutine(SetPoiAfterReborn(isPoi, 0.3f));
        }
        
        IEnumerator SetPoiAfterReborn(bool isPoi, float delay = 0.1f) {
            yield return new WaitForSeconds(delay);
            buttonPoI.GetComponent<ActivityButton>().SetEnabled(isPoi);
            buttonPoI.GetComponent<ActivityButton>().SetExisting(isPoi);
            
            buttonTrivia.GetComponent<ActivityButton>().SetEnabled(!isPoi);
            buttonPhotoVideo.GetComponent<ActivityButton>().SetEnabled(!isPoi);
            buttonAudio.GetComponent<ActivityButton>().SetEnabled(!isPoi);

            yield return null;
        }

        public void SelectPoI() 
        {
            anchorWasCompleted = false;
            
            //Selecting PoI is different from selecting other activity icons.
            isPoI = !isPoI;
            
            // if(SBContextManager.Instance.IsEditCreating()) {
            //     if (!isPoI) {
            //         StopRotate(false);
            //     }
            //     else {
            //         AnchorManager.Instance.RotateAllAnchors();
            //         StopRotate(true);
            //     }
            // }
            
            //update the look of the top completion object
            activityManager.UpdateCompletionObj(isPoI);
            
            //select/deselect
            DoSelectPoI(isPoI, false);            
        }
         
        public void SelectTrivia()
        {                        
            SelectActivity(ActivityType.Trivia, buttonTrivia);
            UpdatePoIButton();
        }
        
        
        public void SelectPhotoVideo()
        {             
            SelectActivity(ActivityType.PhotoVideo, buttonPhotoVideo);
            UpdatePoIButton();
        }
         
        public void SelectAudio()
        {             
            SelectActivity(ActivityType.Audio, buttonAudio);
            UpdatePoIButton();
        }

        public void TriviaExist(bool isExist) {
            buttonTrivia.GetComponent<ActivityButton>().SetExisting(isExist);
            if(isExist)
                activityCount++;
            else
                activityCount--;
            print($"TriviaExist={isExist} #debugconsume");
        }
        
        public void PhotoVideoExist(bool isExist) {
            buttonPhotoVideo.GetComponent<ActivityButton>().SetExisting(isExist);
            if(isExist)
                activityCount++;
            else
                activityCount--;
        }
        
        public void AudioExist(bool isExist) {
            buttonAudio.GetComponent<ActivityButton>().SetExisting(isExist);
            if(isExist)
                activityCount++;
            else
                activityCount--;
        }

        public void CheckActivityCount() {
            print($"activityCount={activityCount}");
            foreach (var sbActivity in activityManager.activitySBList)
            {
                if (sbActivity.activityWasSaved != false) continue;
                allActivitiesWasSaved = false;
                return;
            }

            allActivitiesWasSaved = true;
            SetAnchorObjColor(matWhite);
            coverToComplete.SetActive(true);
            
            if (activityCount == 2) {
                if (SBContextManager.Instance.context.IsCreatingInConsume()) {
                    OnAnchorConfigCompleted(false);  
                }
            }
            else if (activityCount == 3) {
                OnAnchorConfigCompleted(false);  
            }
        }

        public void UpdatePoIButton()
        {
            print($"UpdatePoIButton: selectedActivityType={selectedActivityType} | hasCheckIn={activityManager.HasCheckIn()}");            
            if (activityManager.HasCheckIn() || !activityManager.HasCheckIn() && !activityManager.IsAnyActivityCreated())
            {
                // before the post is saved, we do Not enable POI
                if(wasPostSaved) {
                    print("UpdatePoIButton: true");
                    buttonPoI.GetComponent<ActivityButton>().SetEnabled(true);
                }
            }                
            else
            {
                print("UpdatePoIButton: false");
                buttonPoI.GetComponent<ActivityButton>().SetEnabled(false);
            }
        }

        public void EnablePoIButton(bool enable = true)
        {
            buttonPoI.GetComponent<ActivityButton>().SetEnabled(enable);
        }


        //------------------- Create activity panel  -------------------


        public void CreateSelectedActivity() //when tapping the 'Create button' on the anchor
        {
            //disable options to avoid user to create more
            coverCreate.SetActive(false);
            coverToComplete.SetActive(false);//disable the completion button when editing an activity!
            // editButtons.SetActive(false);
            SetAnchorObjColor(matPurple);

            //create activity through ActivityManager
            if (activityManager.CreateActivity(selectedActivityType))
            {
                EnableEditButtons();   
                ToggleMoreLessButton(true);
            }                                 
        }

        
        public void OnActivityCreationCompletedOrCanceled()
        {
            print($"OnActivityCreationCompletedOrCanceled");   
            if(!quickPhotoWithPostWasCompleted) {
                SetUIMode(UIMode.Creating_EditingPost);
                SetBehaviourMode(BehaviourMode.Creating_SettingActivities);
                return;
            }
            
            SetUIMode(UIMode.Creating_ReadyToAddActivities);
            
            //reset activity type buttons selection
            DeSelectAllActivityTypes();
            UpdatePoIButton();                      
            //It is important that we set this here after all button state have been updated.
            selectedActivityType = ActivityType.Undefined;

            //change anchor UI to be 'ready to complete'.
            isReadyToCompleteConfig = true;
            coverToComplete.SetActive(true);
            SetAnchorObjColor(matWhite);
            
            //expand the activity buttons by default
            ToggleMoreLessButton(true);            
            ShowPreviewImageForVideoActivity();
            
            var photoActivity = activityManager.GetPhotoVideoActivity();
            //print($"[IsQuickPhotoVideo] OnActivityCreationCompletedOrCanceled: photoActivity is null? {photoActivity == null}");
            if (photoActivity != null)
            {
                //print($"photoActivityInfo is null? = {(info1 as PhotoVideoActivityInfo) != null}");
                //print($"IsQuickPhotoVideo={(info1 as PhotoVideoActivityInfo).IsQuickPhotoVideo}");
                if (photoActivity.GetActivityInfo() is PhotoVideoActivityInfo info && info.IsQuickPhotoVideo)
                {
                    //print($"OnActivityCreationCompletedOrCanceled: buttonPoI=disabled");
                    buttonPoI.GetComponent<ActivityButton>().SetEnabled(false);
                    StartCoroutine(SetPoiAfterReborn(false));
                }
            }             
        }


        /// <summary>
        /// When tapping the 'complete' button on the anchor
        /// </summary>
        /// <param name="isUserFarAway"></param>
        /// <param name="postAction"></param>
        public void OnAnchorConfigCompleted(bool isUserFarAway)
        {
            //hide thumbnail debug(preview)
            ThumbnailManager.Instance.Reset();

            //stop off-screen indicator
            OffScreenIndicatorManager.Instance.HideArrow();
            
            //collect all panel data into activityInfo
            AnchorActivityInfo anchorActivityInfo = activityManager.ConcludeActivityInfoList();

            var post = activityManager.GetPostActivity();
            if (activityManager.IsAnyActivityCreated() &&
                (post == null || post.GetActivityId() == Const.ACTIVITY_DEFAULT_ID))
            {
                BottomPanelManager.Instance.ShowMessagePanel("You need to edit the post panel and provide a title for your post.");
                return;
            }
            
            anchorInfo.postInfo = anchorActivityInfo.postInfo;
            anchorInfo.activityInfoList = anchorActivityInfo.activityInfoList;

            //play audio
            AudioManager.Instance.PlayAudio(AudioManager.AudioOption.CompleteSetup);

            //set activity layout
            activityManager.SetLayout(ActivityLayout.Hidden, true, () =>
            {
                if (isUserFarAway) return;
                //play config completion effect
                AnchorConfigCompletionEffectController.Instance.RunEffect(lockerPlate.transform.position);

                //light-up the torch!
                StartCoroutine(LightUp());
                    
                //if it is on-call, automatically return it
                if(isOnCall)
                    OnReturn();

                //update main UI, only for creator mode
                if (!isReborn)
                    StartCoroutine(ShowNextAnchorOrSaveMapDialog());
            });
            
            //update anchor UI mode and behaviour mode
            SetUIMode(UIMode.Creating_Done);
            SetBehaviourMode(BehaviourMode.Creating_Done);

            isConfigCompleted = true;
            if(!isUserFarAway) {
                if(!isReborn) {
                    MiniMapManager.Instance.AddThisAnchors(anchorInfo);
                }
                MiniMapManager.Instance.ShowMiniMap();
                PointsBarManager.Instance.ShowPointsBar();
            }

            ShowPreviewImageForVideoActivity();

            anchorWasCompleted = true;
            ActiveAnchorManager.SetInteractable(true);
            ActivityUIFacade.Instance.EnableBackButton(true);

            // if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
            //     ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextOrCompleteGPSOnlyCreation);
            // }
        }



        //--------------- code for uploading to SB cloud ----------------

        //uploading thumbnail to SB cloud
        public void UploadThumbnail() {
            if (String.IsNullOrWhiteSpace(GetAnchorInfo().thumbnail)) {
                print($"Anchor thumbnail > UploadThumbnail fail, GetAnchorInfo().thumbnail is EMPTY!");
                return;
            }
            
            SubmitThumbnail();
        }


        //callback
        public Action<string, ErrorInfo> OnThumbnailUploaded;
        /// <summary>
        /// Method called when the thumbnail is uploaded successfully to the BLOB storage.
        /// </summary>
        /// <param name="blobUrl">The blob URL of the thumbnail.</param>
        private void OnThumbnailUploadedSuccessfully(string blobUrl)
        {
            print($"Anchor thumbnail > AnchorController OnThumbnailUploadedSuccessfully()");            
            OnThumbnailUploaded(blobUrl, null);
        }


        //callback
        private void OnThumbnailUploadedFailed()
        {
            print($"Anchor thumbnail > OnThumbnailUploadedFailed()");
            // The delegate handler will show the error message.
            OnThumbnailUploaded("", new ErrorInfo
            {
                ErrorCode = 100,
                Message = "The thumbnail was not uploaded.",
            });
        }
        

        //--------------------------------------------------------------

        
        /// <summary>
        /// Convert GPS+AR's location info to SB location info
        /// </summary>
        /// <returns></returns>
        public Location GetSBLocationInfo()
        {
            print($"GetSBLocationInfo > anchorInfo.locationInfo == null? {(this.anchorInfo.locationInfo == null)}");

            if (SBContextManager.Instance.context.isPlanning && SBContextManager.Instance.context.plannedLocation != null)
            {
                return SBContextManager.Instance.context.plannedLocation;
            }

            if (this.anchorInfo.locationInfo == null)
                return null;

            // At this point, we have already retrieved the location info from the native app.
            Location sbLocationInfo = new Location
            {
                Name = $"{this.anchorInfo.locationInfo.Latitude},{this.anchorInfo.locationInfo.Longitude}",
                Latitude = this.anchorInfo.locationInfo.Latitude,
                Longitude = this.anchorInfo.locationInfo.Longitude,
                Altitude = this.anchorInfo.locationInfo.Altitude,                
            };

            if (SBContextManager.Instance.context.UserLocation != null)
            {
                sbLocationInfo.City = SBContextManager.Instance.context.UserLocation.City;
                sbLocationInfo.State = SBContextManager.Instance.context.UserLocation.State;
                sbLocationInfo.Country = SBContextManager.Instance.context.UserLocation.Country;
                sbLocationInfo.Neighborhood = SBContextManager.Instance.context.UserLocation.Neighborhood;                
            }            

            return sbLocationInfo;
        }


        IEnumerator LightUp()
        {
            yield return new WaitForSeconds(1.5f);
            activityManager.LightUp(true); //light-up the torch!
        }


        IEnumerator ShowNextAnchorOrSaveMapDialog()
        {
            print("#finish");
            yield return new WaitForSeconds(2.5f);

            if (SBContextManager.Instance.context.isPlanning)
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.CompletePlanning);
            else 
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextOrCompleteGPSOnlyCreation);
            // else if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
            //     ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextOrCompleteGPSOnlyCreation);
            // else
            //     ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextAnchorOrSaveMap);
            
            //start phone pose detection, when user walk to somewhere else to place another anchor
            PhonePoseManager.Instance.EnablePoseWarning(true);
        }


        //------------------- Photo/Video activity panel -------------------


        public void OnPhotoTaken(string contentPath)
        {
            PhotoVideoActivity activity = activityManager.GetPhotoVideoActivity();
            if (activity != null)
            {
                activity.OnPhotoTaken(contentPath); 
            }
        }
        
        
        public void OnVideoTaken()
        {
            PhotoVideoActivity activity = activityManager.GetPhotoVideoActivity();
            if (activity != null)
            {
                activity.OnVideoTaken();
            }
        }
        
        public void BeforeOnVideoTaken()
        {
            PhotoVideoActivity activity = activityManager.GetPhotoVideoActivity();
            if (activity != null)
            {
                activity.BeforeOnVideoTaken();
            }
        }

        /// <summary>
        /// The method to call after the keywords have been pulled from the native app.
        /// </summary>
        /// <param name="keywords"></param>
        public void OnKeywordsGenerated(string keywords)
        {
            print($"-=- SBContextManager.Instance.context = {SBContextManager.Instance.context}");
            
            if (SBContextManager.Instance.context.IsConsuming()) {
                // PhotoVideoActivityForConsume[] photoVideoConsumes = GetComponentsInChildren<PhotoVideoActivityForConsume>();
                // foreach (var photoVideoConsume in photoVideoConsumes) {
                //     // ToDo: Will this show they keywords on other photoVideo panels too?
                //     print($"-=- photoVideoConsume");
                //
                //     if (photoVideoConsume.isActivePanel) {
                //         photoVideoConsume.ShowKeywords(keywords);
                //     }
                // }

                PhotoVideoActivityForConsume photoVideoActivityForConsume = GetComponentInChildren<PhotoVideoActivityForConsume>();
                if (photoVideoActivityForConsume != null) {
                    photoVideoActivityForConsume.ShowKeywords(keywords);
                }
            }
            else {
                GameObject activeActivityObj = activityManager.GetActiveActivity();
                if (activeActivityObj)
                {
                    activeActivityObj.GetComponent<PhotoVideoActivity>().ShowKeywords(keywords);   
                }
                // GetComponentInChildren<PhotoVideoActivity>().ShowKeywords(keywords);
            }

            // ToDo: Why are we calling this twice? I temporarily removed this code.
            //  Removing this has no side-effect on the app.
            //GameObject activeActivityObj = activityManager.GetActiveActivity();
            //if (activeActivityObj)
            //{
            //    activeActivityObj.GetComponent<PhotoVideoActivity>().ShowKeywords(keywords);
            //}
        }

        /// <summary>
        /// This method shows a preview of all video activities on this anchor
        /// </summary>
        void ShowPreviewImageForVideoActivity() {
            PhotoVideoActivity[] activities = activityPanelsParent.GetComponentsInChildren<PhotoVideoActivity>();
            foreach (var photoVideoActivity in activities) {
                photoVideoActivity.ShowPreviewImage();
            }
        }

        public void ShowVideoThumbnail(Texture2D tex) {
            GameObject activeActivityObj = activityManager.GetActiveActivity();
            if (activeActivityObj != null)
            {
                activeActivityObj.GetComponent<PhotoVideoActivity>().ShowThumbnail(tex);
            }
        }

        //------------------------------------------ Check-in effect ----------------------------------------
        // private bool isCheckedIn;
        // public void OnCheckIn()
        // {
        //     if (!isCheckedIn)
        //     {
        //         isCheckedIn = true;
        //         activityManager.PlayCheckinEffect();
        //     }
        // }
        
        
        //------------------------------------------ Scanning effect ----------------------------------------
        // [SerializeField] private GameObject body;
        // [SerializeField] private GameObject bodyCovers;
        //[SerializeField] private GameObject bodyCore;
        
        // public void EnableScanningAnimation(bool enable)
        // {
        //     SFX_MaterialAdder sfx = body.GetComponent<SFX_MaterialAdder>();
        //     if (enable)
        //     {
        //         bodyCovers.SetActive(false);
        //         sfx.Run();
        //     }
        //     else
        //     {
        //         sfx.Stop();
        //         bodyCovers.SetActive(true);
        //     }
        // }

        
        //---------------------------------- Generate anchor info (config) -------------------------------------
        
        public AnchorInfo GetAnchorInfo()
        {
            return anchorInfo;
        }

        //------------------------------- others-------------------------------
        public ActivityManager GetActivityManager()
        {
            return this.activityManager;
        }


        public bool IsReadyToCompleteConfig()
        {
            return isReadyToCompleteConfig;
        }
        
        public void OnFinishButtonPressed() {
            print("-=- AnchorController OnFinishButtonPressed()");
            InteractionManager.Instance.OnBackToNativeClicked();
        }

        public void OnAddButtonPressed() {
            print("-=- AnchorController OnAddButtonPressed()");
        }


        public void AttachToPlane(Vector3 planeNormal)
        {
            if (transform.forward != planeNormal)
            {
                //rotate anchor body and bottom plate
                Quaternion rotation = Quaternion.identity;
                rotation.SetLookRotation(planeNormal, Vector3.up);
                
                transform.rotation = rotation;
                lockerPlate.transform.rotation = rotation;
            }
        }
        
        public void ResetBottomPlate()
        {
            lockerPlate.transform.localRotation = bottomPlateOriginLocalRot;
        }

        public bool AllActivitiesCompleted() {
            // print("AllActivitiesCompleted()");
            return activityManager.ActivitiesCompleted;
            print("AllActivitiesCompleted() 1");
            var anchorInfo = GetAnchorInfo();
            if (anchorInfo == null)
                return false;
//doesn't check post panel
            print($"AllActivitiesCompleted() anchorInfo.postInfo.Title = {anchorInfo.postInfo.Title}");
            foreach (var activity in anchorInfo.activityInfoList)
            {
                print($"activity = {activity.Type.ToString("G")}");
                if (!activity.IsCompleted())
                    return false;
            }
            print("AllActivitiesCompleted(), true");
            return true;
        }
        
        private Transform anchorOriginParent;
        public void AddARAnchor()
        {
            // Vector3 position = transform.position;
            //
            // //1. create an AR anchor
            // Pose pose = new Pose(position, Quaternion.identity);
            // ARAnchor refPoint = AnchorManager.Instance.arRefPointManager.AddAnchor(pose);
            // if (refPoint != null)
            // {
            //     print("Reference point creation successfully.");
            //     ARRefPoint = refPoint;
            //     
            //     //2. set anchor object as parent
            //     anchorOriginParent = transform.parent;
            //     transform.parent = ARRefPoint.transform;
            // }
            // else
            // {
            //     print("Reference point creation failed!");
            // }
        }
        
        // public void RemoveARAnchor()
        // {
        //     if (!ARRefPoint)
        //         return;
        //
        //     //1. restore anchor object's parent
        //     transform.parent = anchorOriginParent;
        //     
        //     //2. remove anchor
        //     if (AnchorManager.Instance.arRefPointManager.RemoveAnchor(ARRefPoint))
        //     {
        //         print("AR Anchor removal successfully.");
        //         ARRefPoint = null;
        //     }
        //     else
        //     {
        //         print("Anchor removal failed!");
        //     }
        // }

        public void EnableAllSaveButtons(bool enable, SBActivity activity) {
            activityManager.EnableAllSaveButtons(enable, activity);
            if (allActivitiesWasSaved || !enable || SBContextManager.Instance.IsEditCreating()) {
                if (enable) {
                    SetAnchorObjColor(matWhite);
                }
                else {
                    SetAnchorObjColor(matPurple);
                }
                coverToComplete.SetActive(enable);
            }
        }

        public void EnableSaveButtonOnPost() {
            activityManager.GetPostActivity().EnableSaveButton(true);
        }

        #region Thumbnail management
         
        void SubmitThumbnail()
        {
            print($"Anchor thumbnail > SubmitThumbnail started.");
            var experienceId = SBContextManager.Instance.context.experienceId;            
            var refreshPolicy = SBContextManager.Instance.context.UploadedMedia < 1;
            // Upload the audio to our blob server.        
            StartCoroutine(SBRestClient.Instance.GetExperienceContainerUrlIEnumerator(experienceId, refreshPolicy, OnSasUrlReceived, OnSasUrlError));
        }

        /// <summary>
        /// The callback for when the SASURL was successfully retrieved.
        /// </summary>
        /// <param name="sasURL">The URL where the thumbnail can be uploaded.</param>
        void OnSasUrlReceived(string sasURL)
        {
            // This should be a url or path on the device that the code can access.
            // @Cliff, inside UploadBlobIEnumerator, we do a "File.ReadAllBytes(thumbnailPath)",
            var thumbnailPath = GetAnchorInfo().thumbnail;
            print($"Anchor thumbnail > SubmitThumbnail > OnSasUrlReceived started: thumbail={thumbnailPath}");
            StartCoroutine(SBRestClient.Instance.UploadBlobIEnumerator(sasURL, Guid.NewGuid().ToString(), "", AssetType.Thumbnail, ContinueSubmitThumbnail, ContinueOnError, thumbnailPath));
        }

        /// <summary>
        /// The callback for when the SASURL cannot be retrieved.
        /// </summary>
        void OnSasUrlError(ErrorInfo error)
        {
            print($"Anchor thumbnail > SubmitThumbnail > OnSasUrlError.");
            BottomPanelManager.Instance.ShowMessagePanel("Your content cannot be uploaded at this time. Please try again later.");
        }

        /// <summary>
        /// The callback for a successful thumbnail upload.
        /// </summary>
        /// <param name="caption">This can be ignored in this method.</param>
        /// <param name="blobURL">The URL of the thumbnail that was uploaded.</param>
        void ContinueSubmitThumbnail(string caption, string blobURL)
        {
            print($"Anchor thumbnail > ContinueSubmitThumbnail started.");
            if (blobURL.IsNullOrWhiteSpace())
            {
                string message = "The thumbnail cannot be uploaded at this time. Please try again later.";
                print($"Anchor thumbnail > ContinueSubmitThumbnail error: {message}");
                BottomPanelManager.Instance.ShowMessagePanel(message);
                return;
            }

            // Let's not set the thumbnail here to lessen object dependency.
            //// Whether we started with a quick created photo or not,
            //// there should be a postInfo at this point.
            //var anchorInfo = GetAnchorInfo();
            //// But just in case we don't so we won't run into a null run-time exception.
            //if (anchorInfo != null && anchorInfo.postInfo != null)
            //{
            //    print($"Anchor thumbnail > ContinueSubmitThumbnail > Pass the thumbnail URL to the post panel: url={blobURL}");
            //    // Pass the thumbnail URL to the post panel.
            //    anchorInfo.postInfo.RelocalizationThumbnail = blobURL;
            //}

            // Add necessary codes after this line.
            OnThumbnailUploadedSuccessfully(blobURL);
        }

        /// <summary>
        /// The callback for a failed thumbnail upload.
        /// </summary>
        protected void ContinueOnError(ErrorInfo error)
        {            
            BottomPanelManager.Instance.UpdateMessage(error?.Message ?? "The thumbnail cannot be uploaded at this time. Please try again later.");
            OnThumbnailUploadedFailed();
        }

        #endregion


        //------------------------------------ OnCall/OnReturn ------------------------------------
        
        
        // private IEnumerator ContinuousAttachToCam()
        // {
        //     while (isOnCall)
        //     {
        //         yield return new WaitForSeconds(1.5f);
        //
        //         FlyToCam(true, null);
        //         
        //         // go to next frame
        //         yield return null;
        //     }
        // }

        
        private Vector3 anchorPosBeforeFlyover;

        public void OnTapDefaultCover()
        {
            float distanceToCam = Vector3.Distance(transform.position, mainCam.transform.position);
            if (distanceToCam < Const.DISTANCE_4_FLYOVER) //if it close
            {
                ToggleEditButtons();
                activityManager.ToggleLayout();
                
                //CheckOnCallSingleTap(true);
            }
            else //if it is too far away
            {
                // if (flyingLock)
                //     return;
            
                OnCall(); //trigger on call
            }
            
            Event.current.Use();
        }


        public void OnTapReturnCover()
        {
            // if (flyingLock)
            //     return;
            
            OnReturn();

            Event.current.Use();
        }


        //-------------------------- OnCall, begin ------------------------------


        private bool isOnCall;
        
        public bool flyingLock { get; private set; } //indicating this anchor object is flying
        
        public void OnCall()
        {
            if (ActiveAnchorManager.IsAnyAnchorFlying())
                return;
            
            MessageManager.Instance.DebugMessage($">>>>>>>>> OnCall!");
            // print(">>>>>>>>> OnCall! #flyingLock #debugconsume");
            flyingLock = true;

            //if there is other anchor called, release it first
            if (ActiveAnchorManager.CalledAnchorIndex != -1 && ActiveAnchorManager.CalledAnchorIndex != index)
            {
                GameObject calledAnchorObj = ActiveAnchorManager.GetCalledAnchor();
                calledAnchorObj.GetComponent<AnchorController>().OnReturn();
            }
            ActiveAnchorManager.CalledAnchorIndex = index;
            
            //save position
            anchorPosBeforeFlyover = transform.position;
                
            //transform!
            TurnOffButtons();
            enableRotation = false;
            ResetBodyPose();
            WatchPlayer();
            this.support.SetActive(false);
            
            StartCoroutine(DelayPlaySound4OnCall(true)); //play sound
            activityManager.SetLayout(ActivityLayout.Hidden, true, ()=>
            {
                //then fly over
                FlyToCam(false, () =>
                {
                    //then transform again!
                    activityManager.SetLayout(ActivityLayout.HorizontalEdit, true);
                        
                    coverReturn.SetActive(true);
                    WatchPlayer();
                    TurnOnButtons();
                    flyingLock = false;
                    isOnCall = true;
                    
                    //attach to cam
                    //StartCoroutine(ContinuousAttachToCam());
                });
            });
        }


        public void OnReturn()
        {
            if (flyingLock)
                return;
            
            MessageManager.Instance.DebugMessage($">>>>>>>>> OnReturn!"); 
            // print($">>>>>>>>> OnReturn! #flyingLock #debugconsume");
            flyingLock = true;
            
            ActiveAnchorManager.CalledAnchorIndex = -1;
            
            this.isOnCall = false;
            //StopCoroutine(ContinuousAttachToCam());
            
            //transform!
            TurnOffButtons();
            WatchPlayer();
            coverReturn.SetActive(false);
            
            StartCoroutine(DelayPlaySound4OnCall(false)); //play sound
            activityManager.SetLayout(ActivityLayout.Hidden, true, ()=>
            {
                //then fly back
                FlyBack(() =>
                {
                    //then transform again!
                    this.support.SetActive(true);
                    WatchPlayer();
                    TurnOnButtons();
                    enableRotation = true;
                    flyingLock = false;
                });
            });
        }
        
        
        private void FlyToCam(bool attachMode, Action postAction)
        {
            float scaleFactor = 1.75f; //this is scale factor, just to make it not as close as 1 meter away from user.
            Vector3 camForwardPos = mainCam.transform.position + (mainCam.transform.forward * scaleFactor); 
            Vector3 destPos = new Vector3(camForwardPos.x, transform.position.y, camForwardPos.z);
            if (!attachMode)
            {
                transform.DOMove(destPos, 2f).SetEase(Ease.InOutQuart).OnComplete(() =>
                {
                    postAction?.Invoke();
                });
            }
            else
            {
                transform.DOMove(destPos, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    postAction?.Invoke();
                });
            }
        }


        private void FlyBack(Action postAction)
        {
            transform.DOMove(anchorPosBeforeFlyover, 2f).SetEase(Ease.InOutQuart).OnComplete(() =>
            {
                postAction?.Invoke();
            });
        }
        
        
        private IEnumerator DelayPlaySound4OnCall(bool isOnCall)
        {
            yield return new WaitForSeconds(0.75f);
            if (isOnCall)
            {
                AudioManager.Instance.PlayAudio(AudioManager.AudioOption.OnCall);
            }
            else
            {
                AudioManager.Instance.PlayAudio(AudioManager.AudioOption.OnReturn);
            }
        }


        public void HideActivities() {
            //update layout
            activityManager.SetLayout(ActivityLayout.Hidden);
        }

        //------------------------------------- marker control -------------------------------

        public bool isModeSettled = false;
        
        public enum AnchorMode
        {
            Anchor, Marker, Indicator, Undefined
        }

        private AnchorMode currAnchorMode = AnchorMode.Undefined;

        public void SetAnchorMode(AnchorMode newMode, bool withAnimation = true, Action postAction = null)
        {
            if (!this.markerObj)
                return;

            if (this.currAnchorMode == newMode)
                return;

            this.currAnchorMode = newMode;
            MarkerController mController = this.markerObj.GetComponent<MarkerController>();
            switch (this.currAnchorMode)
            {
                case AnchorMode.Anchor:
                    mController.EnableIdleAnimation(false);
                    if (withAnimation)
                    {
                        mController.MarkerFadeOff(() =>
                        {
                            this.gameObject.SetActive(true);
                            if(this.activityManager.GetCurrentLayout() == ActivityLayout.Undefined)
                                this.activityManager.ToggleLayout();
                            WatchPlayer();
                            
                            postAction?.Invoke();
                        });    
                    }
                    else
                    {
                        mController.MarkerOff(() =>
                        {
                            this.gameObject.SetActive(true);
                            if(this.activityManager.GetCurrentLayout() == ActivityLayout.Undefined)
                                this.activityManager.ToggleLayout();
                            WatchPlayer();
                            
                            postAction?.Invoke();
                        });
                    }
                    break;
                
                case AnchorMode.Marker:
                    mController.EnableIdleAnimation(true);
                    if (withAnimation)
                    {
                        mController.MarkerFadeOn(() =>
                        {
                            this.gameObject.SetActive(false);
                            postAction?.Invoke();
                        });    
                    }
                    else
                    {
                        mController.MarkerOn(() =>
                        {
                            this.gameObject.SetActive(false);
                            postAction?.Invoke();
                        });
                    }
                    break;
                
                case AnchorMode.Indicator:
                    mController.EnableIdleAnimation(false);
                    mController.MarkerOff(() =>
                    {
                        this.gameObject.SetActive(false);
                        postAction?.Invoke();
                    });
                    break;
            }
        }

    }
}
