using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Elasticsearch.Net.Specification.CrossClusterReplicationApi;
using UnityEngine;
using UnityEngine.Serialization;


namespace SocialBeeAR
{
    
    public enum ActivityLayout
    {
        Undefined,
        Hidden,
        Vertical, //vertical
        Horizontal, //horizontal, editing order(last one in center), non-curved
        HorizontalEdit, //horizontal, editing order(last one in center), curved
        HorizontalConsume, //horizontal, consuming order(first one/post in center), curved
    }
    
    
    /// <summary>
    /// ActivityManager is for managing the activity panels(or other kind of 3d objects), for example, switching the
    /// layout for different modes. Every activity has its own panel(or other kind of 3d object), from a prefab file.
    /// </summary>
    public class ActivityManager : MonoBehaviour
    {

        //All the activities including the default PoI panel, and this is the only place we store activity objects 
        private List<GameObject> activityObjList = new List<GameObject>();
        private int activeIndex = -1;

        //layout config, the integer value represents the ID of panels
        private List<GameObject> hiddenSlots = new List<GameObject>();
        private List<GameObject> verticalSlots = new List<GameObject>();
        private List<GameObject> horizontalSlotsLeft = new List<GameObject>();
        private List<GameObject> horizontalSlotsRight = new List<GameObject>();
        private List<GameObject> horizontalEditSlotsLeft = new List<GameObject>();
        private List<GameObject> horizontalEditSlotsRight = new List<GameObject>();

        //the position value of the layout above, which is calculated according to every panel's size info
        private List<Vector3> hiddenSlotsPos = new List<Vector3>();
        private List<Vector3> verticalSlotsPos = new List<Vector3>();
        private List<Vector3> horizontalSlotsLeftPos = new List<Vector3>();
        private List<Vector3> horizontalSlotsRightPos = new List<Vector3>();
        private List<Vector3> horizontalEditSlotsLeftPos = new List<Vector3>();
        private List<Vector3> horizontalEditSlotsRightPos = new List<Vector3>();

        //layout management parameters
        private ActivityLayout fromLayout = ActivityLayout.Undefined;
        private ActivityLayout currentLayout = ActivityLayout.Undefined;
        private Vector3 originDown; //the position is anchor's body center
        private Vector3 originUp; //the poision is originDown + YOffset
        private float verticalGap = 160f; //35f
        private float horizontalGap = 35f;
        private float transformTime = 1f;

        //PoI panel (which is existing by default)
        [SerializeField] private GameObject postPanel;

        //panel prefabs
        [SerializeField] private GameObject triviaPrefab;
        [SerializeField] private GameObject photoVideoPrefab;
        [SerializeField] private GameObject photoVideoConsumePrefab;
        [SerializeField] private GameObject audioPrefab;

        //indicating whether it's the new round of activity creation.
        private bool newRound = true;

        //anchor completion object on the top
        [SerializeField] private GameObject planningTopPanel;
        [SerializeField] private GameObject completionObj;
        [SerializeField] private GameObject glowObj;
        [SerializeField] private GameObject completedGlowObj;
        [SerializeField] private GameObject completionEffectPrefab;
        private Vector3 completionObjGap = new Vector3(0, 250f, 0);
        private Vector3 completionObjExtraGap4Planning = new Vector3(0, 200, 0);
        [SerializeField] private Material poiMat;
        [SerializeField] private Material nonPoiMat;
        [SerializeField] private Material completedMat;
        
        //watching target
        [SerializeField] private GameObject watchingTarget;
        private Vector3 lookAtTargetPos;

        [HideInInspector] public bool wasStartWithPhotoVideo;
        [HideInInspector] public List<SBActivity> activitySBList = new List<SBActivity>();

        //This is redundant data, anchor pose can be dynamically retrieved from AnchorController by: 
        //GetComponentInParent<AnchorController>().GetAnchorInfo().pose;
        // /// <summary>
        // ///
        // /// </summary>
        // /// <remarks>
        // /// We are making this a class-level member because we need this information per activity.
        // /// If the anchor where the activities being created will move
        // /// then the <see cref="AnchorManager"/> should update this member.
        // /// We will need to expose a method that will update this member.
        // /// </remarks>
        // Pose anchorPose;

        //------------------------------ monobehaviour methods ------------------------------

        private void Awake()
        {
            originDown = Vector3.zero;

            if (SBContextManager.Instance.context.IsConsuming()) {
                originUp = new Vector3(0, 680, 0);
            }
            else {
                originUp = new Vector3(0, 850, 0);
            }
        }
        
        
        public void Start()
        {
            print($"ActivityManager.Start");

            // poIPanel.GetComponent<SBActivity>().Init(Const.ACTIVITY_DEFAULT_ID, ActivityType.PoI);
            // activityObjList.Add(poIPanel);

            //disable the glow effect by default
            LightUp(false);
        }


        //------------------------------ create/reborn/delete activities ------------------------------

        public bool CreateActivity(ActivityType type) //when tapping the 'create' button on the anchor
        {
            print($"CreateActivity: type={type}");
            print($"IsAnyActivityCreated={IsAnyActivityCreated()}");
            print($"HasAlreadyCreatedThisType={HasAlreadyCreatedThisType(type)}");
            if (IsAnyActivityCreated() && HasAlreadyCreatedThisType(type))
            {
                BottomPanelManager.Instance.UpdateMessage("You can only create one copy of this activity.");
                return false;
            }

            // if (!newRound) return false;

            newRound = false;

            //init object from prefab 
            GameObject panelObj = InitPanelObj(type);
            if (panelObj)
            {
                //print($"CreateActivity - Adding to activityObjList: panelObj is null? {panelObj == null}");
                activityObjList.Add(panelObj);

                //init (e.g. generate ID)
                string uniqueId = System.Guid.NewGuid().ToString("N");
                SBActivity sbActivity = panelObj.GetComponent<SBActivity>();

                activitySBList.Add(sbActivity);

                var parentId = "";
                if (type != ActivityType.Post)
                {
                    parentId = GetPostActivityId();
                }

                Pose anchorPose = GetComponentInParent<AnchorController>().GetAnchorInfo().pose;
                if (SBContextManager.Instance.context.mapId.IsNullOrWhiteSpace())
                {
                    SBContextManager.Instance.context.mapId = VpsCoverageManager.Instance.SelectedWayspotId;
                }
                sbActivity.Born(uniqueId, type, SBContextManager.Instance.context.experienceId, anchorPose, parentId, SBContextManager.Instance.context.mapId);

                wasStartWithPhotoVideo = RecordManager.Instance.startWithPhotoVideo;

                //calculate and update layout
                SetLayout(ActivityLayout.HorizontalEdit, true, () =>
                {
                    //open camera UI for photo/video activity
                    if (type == ActivityType.PhotoVideo && !RecordManager.Instance.startWithPhotoVideo) 
                    {
                        //init RecordManager for photo/video (performance intensive!)
                        RecordManager.Instance.ClearPaths();
                        
                        //start UI for photo/video
                        //UIManager.Instance.StartCameraUI();
                        UIManager.Instance.FadeInCameraUI();
                    }
                    else if (type == ActivityType.Audio)
                    {
                        //init RecordManager for audio (performance intensive!)
                        AudioActivity audioActivity = panelObj.GetComponent<AudioActivity>();
                        RecordManager.Instance.SetPanel(audioActivity, true);
                        
                        //start UI for audio
                        //UIManager.Instance.StartAudioUI();
                        UIManager.Instance.FadeInAudioUI();
                    }
                    
                    RecordManager.Instance.startWithPhotoVideo = false;    
                    wasStartWithPhotoVideo = false;
                });

                if(type != ActivityType.Audio) {
                    //update off-screen indicator
                    OffScreenIndicatorManager.Instance.SetTarget(panelObj.transform);
                    OffScreenIndicatorManager.Instance.ShowArrow();
                }
            }

            return true;
        }


        public void DeleteActivity(string id)
        {
            int indexToBeDeleted = -1;
            for (int i = 0; i < activityObjList.Count; i++)
            {
                SBActivity sbActivity = activityObjList[i].GetComponent<SBActivity>();
                if (sbActivity.GetActivityId() == id)
                {
                    indexToBeDeleted = i;
                    break;
                }
            }

            GameObject panelObjToBeDeleted = activityObjList[indexToBeDeleted];

            //remove from the list
            if (indexToBeDeleted != -1) {
                activityObjList.RemoveAt(indexToBeDeleted);
                activitySBList.RemoveAt(indexToBeDeleted);
            }

            //Destroy object!
            Destroy(panelObjToBeDeleted);
            
            //report to anchor controller
            GetComponentInParent<AnchorController>()?.OnActivityCreationCompletedOrCanceled();
            
            //update layout
            SetLayout(currentLayout, true);
        }


        public void SetCurrLayout()
        {
            SetLayout(currentLayout, true);
        }


        public void InitActivities()
        {
            print($"clearing activityObjList... isPlanning={SBContextManager.Instance.context.isPlanning}");
            this.activityObjList.Clear();

            //create Post activity
            if (SBContextManager.Instance.context.isPlanning)
            {
                postPanel.SetActive(false);
            }
            else
            {
                print($"postPanel is null? {postPanel==null}");
                postPanel.SetActive(true);
                print("calling BornPostActivity...");
                BornPostActivity();
            }

            //before one anchor is configured completed, it's not lighted-up.       
            LightUp(false);
        }


        public void BornPostActivity()
        {
            print("BornPostActivity started.");
            var c = GetComponentInParent<AnchorController>();
            // print($"AnchorController=null? {c==null}.");

            if (c == null)
            {
                c = GetComponent<AnchorController>();
                // print($"2 - AnchorController=null? {c==null}.");
            }
            
            var anchorInfo = c.GetAnchorInfo();
            // print($"anchorInfo=null? {anchorInfo==null}."); 
            var post = postPanel.GetComponentInChildren<PostActivity>();
            // print($"BornPostActivity postPanel=null? {post==null}.");
            post.Born(Const.ACTIVITY_DEFAULT_ID, 
                ActivityType.Post, 
                SBContextManager.Instance.context.experienceId, 
                anchorInfo.pose, 
                mapId: VpsCoverageManager.Instance.SelectedWayspotId, 
                anchorPayload: anchorInfo.anchorPayload);            
            activityObjList.Add(postPanel);
            activitySBList.Clear();
            activitySBList.Add(postPanel.GetComponent<SBActivity>());
        }


        private GameObject RebornAnyTriviaPanel(IActivityInfo activityInfo)
        {
            print($"activityInfo is a Trivia.");
            var panelObj = Instantiate(triviaPrefab, transform);
            panelObj.GetComponent<TriviaActivity>().Reborn(activityInfo);
            print($"panelObj is null? ${panelObj==null}");
            return panelObj;
        }

        private GameObject RebornAnyPhotoVideoPanel(IActivityInfo activityInfo)
        {
            GameObject panelObj;
            print($"activityInfo is a PhotoVideo.");
            if (SBContextManager.Instance.context.IsConsuming())
            {
                print($"Instantiate photoVideoConsumePrefab...");
                panelObj = Instantiate(photoVideoConsumePrefab, transform);
                panelObj.GetComponent<PhotoVideoActivityForConsume>().Reborn(activityInfo);
                //// assignment to var is for debugging only.
                //var comp = panelObj.GetComponent<PhotoVideoActivityForConsume>();
                //if (comp == null)
                //    print("panelObj.GetComponent<PhotoVideoActivityForConsume> yields null!");
                //else
                //    panelObj.GetComponent<PhotoVideoActivityForConsume>().Reborn(activityInfo);
            }
            else
            {
                print($"Instantiate photoVideoPrefab...");
                panelObj = Instantiate(photoVideoPrefab, transform);
                panelObj.GetComponent<PhotoVideoActivity>().Reborn(activityInfo);
            }

            // panelObj.GetComponent<PhotoVideoActivity>().Reborn(activityInfo);
            print($"panelObj is null? ${panelObj==null}");
            return panelObj;
        }
        
        private GameObject RebornAnyAudioPanel(IActivityInfo activityInfo)
        {
            GameObject panelObj;
            print($"activityInfo is an Audio.");
            if (SBContextManager.Instance.context.IsConsuming())
            {
                print($"Instantiate audioPrefab...");
                panelObj = Instantiate(audioPrefab, transform);
            }
            else
            {
                print($"Instantiate audioPrefab...");
                panelObj = Instantiate(audioPrefab, transform);
            }

            panelObj.GetComponent<AudioActivity>().Reborn(activityInfo);
            print($"panelObj is null? ${panelObj==null}");
            return panelObj;
        }
        
        /// <summary>
        /// The key logic here is to create an PoI panel for the post info, which should be added into the activityObjList,
        /// to be managed together with all other activities.
        /// </summary>
        /// <param name="anchorActivityInfo"></param>
        public void RebornActivities(AnchorActivityInfo anchorActivityInfo)
        {
            this.activityObjList.Clear();
            
            //1. reborn the Post panel
            //postPanel.SetActive(true);
            postPanel.SetActive(false);

            PostActivity post = postPanel.GetComponentInChildren<PostActivity>();
            var postActivityInfo = anchorActivityInfo.postInfo.Clone();
            if (postActivityInfo == null)
            {
                print($"ActivityManager.RebornActivities: postActivityInfo is null.");
            }
            else
            {                 
                post.Reborn(postActivityInfo);
            }         
            print($"Before this.activityObjList.Add(postPanel);");
            this.activityObjList.Add(postPanel);
            activitySBList.Clear();
            activitySBList.Add(postPanel.GetComponent<SBActivity>());
 
            //2. reborn other panels.
            for (int i = 0; i < anchorActivityInfo.activityInfoList.Count; i++)
            {
                print($"anchorActivityInfo.activityInfoList index = {i}.");
                var activityInfo = anchorActivityInfo.activityInfoList[i];
                print($"activityInfo is null? {activityInfo==null} | Title={activityInfo?.Title}");

                GameObject panelObj = null;
                if (activityInfo is PostActivityInfo)
                {
                    print($"activityInfo is a Post, skipping, already reborn.");
                    // We have already reborn the post activity so let's skip this.
                    continue;
                }
                else if (activityInfo is TriviaActivityInfo)
                {
                    panelObj = RebornAnyTriviaPanel(activityInfo);
                }
                else if (activityInfo is PhotoVideoActivityInfo)
                {
                    panelObj = RebornAnyPhotoVideoPanel(activityInfo);
                }
                else if (activityInfo is AudioActivityInfo)
                {
                    panelObj = RebornAnyAudioPanel(activityInfo);
                }

                if (!panelObj) continue;
                print($"RebornActivities - Adding to activityObjList: panelObj for other types is null? {panelObj == null}");
                activityObjList.Add(panelObj);
                activitySBList.Add(panelObj.GetComponent<SBActivity>());
                print($"RebornActivities activityObjList.Count = {activityObjList.Count}");
            }

            //3. update diamond color (it changes according to a) whether all activities completed, b) whether it's PoI)
            PostActivityInfo postInfo = (PostActivityInfo) postActivityInfo;
            UpdateCompletionObj(postInfo.HasCheckIn);
            
            //for consumer, lights up
            LightUp(true);
        }


        private GameObject InitPanelObj(ActivityType type)
        {
            GameObject newActivityObj = null;
            switch (type)
            {
                case ActivityType.Trivia:
                    newActivityObj = Instantiate(triviaPrefab, transform);
                    break;

                case ActivityType.PhotoVideo:
                    newActivityObj = Instantiate(photoVideoPrefab, transform);
                    break;

                case ActivityType.Audio:
                    newActivityObj = Instantiate(audioPrefab, transform);
                    break;
            }

            return newActivityObj;
        }
        

        //------------------------------ active activity management ------------------------------

        
        public void RegisterAsActiveActivity(string activityId)
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {
                if (activityObjList[i].GetComponent<ISBActivity>().GetActivityId() == activityId)
                {
                    if (activeIndex != i)
                    {
                        activeIndex = i;
                        
                        //Show off-screen indicator
                        OffScreenIndicatorManager.Instance.ShowArrow();
                        OffScreenIndicatorManager.Instance.SetTarget(activityObjList[i].transform);
                    }
                }
            }
        }


        public void ResetActiveActivity()
        {
            activeIndex = -1;
            
            //Hide off-screen indicator
            OffScreenIndicatorManager.Instance.HideArrow();
        }


        public GameObject GetActiveActivity()
        {
            if (activeIndex != -1)
                return activityObjList[activeIndex];
            else
                return null;
        }

        public PostActivity GetPostActivity()
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {
                try
                {
                    var sbActivity = activityObjList[i].GetComponent<ISBActivity>();
                    if (sbActivity != null && sbActivity.IsPost())
                    {                        
                        print($"ActivityManager.GetPostActivity: post activity found.");
                        return (PostActivity)sbActivity;
                    }
                }
                catch (Exception ex)
                {
                    print($"ActivityManager.GetPostActivity Exception: {ex.Message}");
                }
            }

            return null;
        }

        public PhotoVideoActivity GetPhotoVideoActivity()
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {                
                try
                {
                    var sbActivity = activityObjList[i].GetComponent<ISBActivity>();                    
                    if (sbActivity != null && sbActivity.IsPhotoVideo())
                    {                        
                        return (PhotoVideoActivity)sbActivity;
                    }
                }
                catch (Exception ex)
                {
                    print($"ActivityManager.GetPostActivity Exception: {ex.Message}");
                }
            }

            return null;
        }

        /// <summary>
        /// Return the created activity options
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISBActivity> GetChildren()
        {
            var activities = new List<ISBActivity>();
            for (int i = 0; i < activityObjList.Count; i++)
            {
                try
                {
                    var sbActivity = activityObjList[i].GetComponent<ISBActivity>();
                    if (sbActivity != null && !sbActivity.IsPost())
                    {
                        activities.Add(sbActivity);
                    }
                }
                catch (Exception ex)
                {
                    print($"ActivityManager.GetPostActivity Exception: {ex.Message}");
                }
            }

            return activities;
        }

        public ISBActivity GetMediaBasedActivity()
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {
                try
                {
                    var sbActivity = activityObjList[i].GetComponent<ISBActivity>();                    
                    if (sbActivity != null && (sbActivity.IsPhotoVideo() || sbActivity.IsAudio()))
                    {                        
                        return sbActivity;
                    }
                }
                catch (Exception ex)
                {
                    print($"ActivityManager.GetPostActivity Exception: {ex.Message}");
                }
            }

            return null;
        }

        public void SetInteractable(bool interactable)
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {
                if (i != activeIndex)
                {
                    Utilities.SetCanvasGroupInteractable(activityObjList[i], interactable);
                }
            }
        }
        
        
        public void SetInteractable(int index, bool interactable)
        {
            if (index >= activityObjList.Count)
                return;

            GameObject activityObj = activityObjList[index];
            Utilities.SetCanvasGroupInteractable(activityObj, interactable);
        }


        public bool ActivitiesCompleted
        {
            get
            {
                var completed = true;
                foreach (var activity in activityObjList)
                {
                    try
                    {
                        var sbActivity = activity.GetComponent<ISBActivity>();
                        // If GetComponent fails for all activities then the status will be returned as completed.
                        if (sbActivity == null)
                        {
                            print("activity.GetComponent failed! #debugconsume");
                            continue;
                        }

                        var info = sbActivity.GetActivityInfo();
                        // If we cannot get an info for all activities then the status will be returned as completed.
                        if (info == null)
                        {
                            print("sbActivity.GetActivityInfo returned null");
                            continue;
                        }

                        // An activity is considered complete regardless if it's right or wrong (in case of a trivia)
                        // or the photo is invalid for photo challenges.
                        completed = completed && info.Status != ActivityStatus.New;
                        print($"ActivitiesCompleted > Id={info.Id} | Status={info.Status} | Type={info.Type.ToString()}");
                    }
                    catch (Exception ex)
                    {
                        print($"ActivityManager.ActivitiesCompleted Exception: {ex.Message}");
                    }
                }

                return completed;
            }
        }

        //------------------------------ layout management ------------------------------

        private void BuildLayouts()
        {
            //calculate slots for all modes
            CalculateSlots();

            //calculate target object position for all modes
            CalculateTargetPosition();
        }


        private void CalculateSlots()
        {
            //1. reset layout slots
            ResetSlots();

            //2. build the 'layout' (put objects to slots)
            //prepare slot for hidden layout slots
            for (int i = activityObjList.Count - 1; i >= 0; i--)
            {
                hiddenSlots.Add(activityObjList[i]);
            }

            //prepare slot for vertical layout slots
            for (int i = activityObjList.Count - 1; i >= 0; i--)
            {
                verticalSlots.Add(activityObjList[i]);
            }

            //prepare slot for horizontal layout slots
            for (int i = 0; i < activityObjList.Count; i++)
            {
                if (i % 2 == 0)
                    horizontalSlotsLeft.Add(activityObjList[i]);
                else
                    horizontalSlotsRight.Add(activityObjList[i]);
            }

            //prepare slot for horizontal(edit mode) layout slots
            int index = 0;
            for (int i = activityObjList.Count - 1; i >= 0; i--)
            {
                if (index % 2 == 0)
                    horizontalEditSlotsLeft.Add(activityObjList[i]);
                else
                    horizontalEditSlotsRight.Add(activityObjList[i]);
                index++;
            }
        }
        

        private void CalculateTargetPosition()
        {
            //0. calculate target position for each slot: hidden layout
            hiddenSlotsPos = CalculateHiddenPos();

            //1. calculate target position for each slot: vertical layout
            verticalSlotsPos = CalculateVerticalPos();

            //2. calculate target position for each slot: horizontal layout
            horizontalSlotsLeftPos = CalculateHorizontalLeftPos(horizontalSlotsLeft);
            horizontalSlotsRightPos = CalculateHorizontalRightPos(horizontalSlotsRight);
            
            horizontalEditSlotsLeftPos = CalculateHorizontalLeftPos(horizontalEditSlotsLeft);
            horizontalEditSlotsRightPos = CalculateHorizontalRightPos(horizontalEditSlotsRight);
        }


        private List<Vector3> CalculateHiddenPos()
        {
            List<Vector3> tempPosList = new List<Vector3>();
            for (int i = 0; i < verticalSlots.Count; i++)
            {
                tempPosList.Add(originDown);
            }

            return tempPosList;
        }


        private List<Vector3> CalculateVerticalPos()
        {            
            List<Vector3> tempPosList = new List<Vector3>();

            for (int i = 0; i < verticalSlots.Count; i++)
            {
                Vector3 targetPos = Vector3.zero;
                if (i == 0)
                {
                    targetPos = originUp;
                }
                else {
                    float lastObjHeight;
                    float currentObjHeight;
                    if(verticalSlots[i - 1].GetComponent<SBActivity>() != null) {
                        lastObjHeight = verticalSlots[i - 1].GetComponent<SBActivity>().GetHeight();
                    }
                    else {
                        lastObjHeight = verticalSlots[i - 1].GetComponent<SBActivityForConsume>().GetHeight();
                    }

                    if (verticalSlots[i].GetComponent<SBActivity>() != null) {
                        currentObjHeight = verticalSlots[i].GetComponent<SBActivity>().GetHeight();
                    }
                    else {
                        currentObjHeight = verticalSlots[i].GetComponent<SBActivityForConsume>().GetHeight();
                    }

                    //calculate how much to move
                    float moveUp = (lastObjHeight / 2f) + verticalGap + (currentObjHeight / 2f);
                    Vector3 moveUpVector3 = new Vector3(0, moveUp, 0);

                    //set target position
                    targetPos = tempPosList[tempPosList.Count - 1] + moveUpVector3; //just add based on the last one
                }
                tempPosList.Add(targetPos);
            }

            return tempPosList;
        }


        private List<Vector3> CalculateHorizontalLeftPos(List<GameObject> horizSlotsLeft)
        {
            List<Vector3> tempPosList = new List<Vector3>();

            for (int i = 0; i < horizSlotsLeft.Count; i++)
            {
                Vector3 targetPos = Vector3.zero;
                if (i == 0)
                {
                    targetPos = originUp;
                }
                else
                {
                    float lastObjWidth;
                    float currentObjWidth;
                    if (horizSlotsLeft[i - 1].GetComponent<SBActivity>() != null) {
                        lastObjWidth = horizSlotsLeft[i - 1].GetComponent<SBActivity>().GetWidth();
                    }
                    else {
                        lastObjWidth = horizSlotsLeft[i - 1].GetComponent<SBActivityForConsume>().GetWidth();
                    }

                    if (horizSlotsLeft[i].GetComponent<SBActivity>() != null) {
                        currentObjWidth = horizSlotsLeft[i].GetComponent<SBActivity>().GetWidth();
                    }
                    else {
                        currentObjWidth = horizSlotsLeft[i].GetComponent<SBActivityForConsume>().GetWidth();
                    }
                    
                    //calculate how much to move
                    float move = (lastObjWidth / 2f) + horizontalGap + (currentObjWidth / 2f);
                    move = move * -1; //since it's left
                    Vector3 moveVector3 = new Vector3(move, 0, 0);

                    //set target position
                    targetPos = tempPosList[tempPosList.Count - 1] + moveVector3;
                }
                tempPosList.Add(targetPos);
            }

            return tempPosList;
        }


        private List<Vector3> CalculateHorizontalRightPos(List<GameObject> horizSlotsRight)
        {
            List<Vector3> tempPosList = new List<Vector3>();

            for (int i = 0; i < horizSlotsRight.Count; i++) {
                float poiPanelWidth;
                float lastObjWidth;
                float currentObjWidth;
                if (postPanel.GetComponent<SBActivity>() != null) {
                    poiPanelWidth = postPanel.GetComponent<SBActivity>().GetWidth();
                }
                else {
                    poiPanelWidth = postPanel.GetComponent<SBActivityForConsume>().GetWidth();
                }
                
                if (i == 0) {
                    lastObjWidth = poiPanelWidth;
                }
                else if (horizSlotsRight[i - 1].GetComponent<SBActivity>() != null){
                    lastObjWidth = horizSlotsRight[i - 1].GetComponent<SBActivity>().GetWidth();
                }
                else {
                    lastObjWidth = horizSlotsRight[i - 1].GetComponent<SBActivityForConsume>().GetWidth();
                }

                if (horizSlotsRight[i].GetComponent<SBActivity>() != null) {
                    currentObjWidth = horizSlotsRight[i].GetComponent<SBActivity>().GetWidth();
                }
                else {
                    currentObjWidth = horizSlotsRight[i].GetComponent<SBActivityForConsume>().GetWidth();
                }
                
                //calculate how much to move
                float move = (lastObjWidth / 2f) + horizontalGap + (currentObjWidth / 2f);
                Vector3 moveVector3 = new Vector3(move, 0, 0);

                //set target position
                Vector3 targetPos = (i == 0) ? originUp + moveVector3 : (tempPosList[tempPosList.Count - 1] + moveVector3);
                tempPosList.Add(targetPos);
            }

            return tempPosList;
        }


        private void ResetSlots()
        {
            //hidden
            hiddenSlots.Clear();
            hiddenSlotsPos.Clear();

            //vertical
            verticalSlots.Clear();
            verticalSlotsPos.Clear();
            
            //horizontal
            horizontalSlotsLeft.Clear();
            horizontalSlotsRight.Clear();

            horizontalSlotsLeftPos.Clear();
            horizontalSlotsRightPos.Clear();
            
            //horizontal edit
            horizontalEditSlotsLeft.Clear();
            horizontalEditSlotsRight.Clear();

            horizontalEditSlotsLeftPos.Clear();
            horizontalEditSlotsRightPos.Clear();
        }


        /// <summary>
        /// Do move panels according to the layout configuration.
        /// </summary>
        /// <param name="postAction"></param>
        public void ApplyLayout(Action postAction = null)
        {
            Sequence sequence = DOTween.Sequence();

            if(currentLayout == ActivityLayout.Hidden)
            {
                //update position of the planning top panel
                if (SBContextManager.Instance.context.isPlanning)
                {
                    Tween planningTopT = planningTopPanel.transform.DOLocalMove(originUp - new Vector3(0, 300, 0), transformTime);
                    sequence.Join(planningTopT);
                }

                //update the position of completion object on top
                completionObj.SetActive(true);
                Vector3 newCompleteObjPos = (SBContextManager.Instance.context.isPlanning ? originUp + new Vector3(0, 100, 0) : originUp);
                Tween completionT = completionObj.transform.DOLocalMove(newCompleteObjPos, transformTime);
                sequence.Join(completionT);

                for (int i = 0; i < hiddenSlots.Count; i++)
                {
                    ////rotate to be as same as the center panel
                    //Tween rotationT = RotateToAsAnchorBody(hiddenSlots[i]);
                    //sequence = sequence.Join(rotationT);

                    //move
                    GameObject panel = hiddenSlots[i];
                    Vector3 targetPos = hiddenSlotsPos[i];
                    Tween panelMoveT = panel.transform.DOLocalMove(targetPos, transformTime).SetEase(Ease.OutQuint);
                    sequence.Join(panelMoveT);

                    //scale
                    Tween panelScaleT = panel.transform.DOScale(Vector3.zero, transformTime).SetEase(Ease.OutCirc);
                    sequence.Join(panelScaleT);
                }

                StartCoroutine(WaitAndHidePanels());
            }
            else if (currentLayout == ActivityLayout.Vertical)
            {
                SetAllPanelEnabled(true);
                
                //update position of the planning top panel
                if (SBContextManager.Instance.context.isPlanning)
                {
                    Vector3 newPlanningTopPanelPos = CalculatePlanningTopPanelPos(verticalSlotsPos[verticalSlotsPos.Count - 1], verticalSlots[verticalSlots.Count - 1]);
                    Tween planningTopT = planningTopPanel.transform.DOLocalMove(newPlanningTopPanelPos, transformTime);
                    sequence.Join(planningTopT);
                }

                //update the position of completion object on top
                completionObj.SetActive(true);
                Vector3 newCompleteObjPos = CalculateCompleteObjPos(verticalSlotsPos[verticalSlotsPos.Count - 1], verticalSlots[verticalSlots.Count - 1]);
                Tween completionT = completionObj.transform.DOLocalMove(newCompleteObjPos, transformTime);
                sequence.Join(completionT);
                
                for (int i = 0; i < verticalSlots.Count; i++)
                {
                    GameObject panel = verticalSlots[i];
                    Vector3 targetPos = verticalSlotsPos[i];
                    Tween panelMoveT = panel.transform.DOLocalMove(targetPos, transformTime).SetEase(Ease.OutQuint);
                    sequence.Join(panelMoveT);
                }
            }
            else if (currentLayout == ActivityLayout.HorizontalEdit)
            {
                SetAllPanelEnabled(true);
                
                //update position of the planning top panel
                if (SBContextManager.Instance.context.isPlanning )
                {
                    Vector3 newPlanningTopPanelPos = CalculatePlanningTopPanelPos(horizontalEditSlotsLeftPos[0], horizontalEditSlotsLeft[0]);
                    Tween planningTopT = planningTopPanel.transform.DOLocalMove(newPlanningTopPanelPos, transformTime);
                    sequence.Join(planningTopT);
                }

                //update the position of completion object on top
                completionObj.SetActive(true);
                Vector3 newCompleteObjPos = CalculateCompleteObjPos(horizontalEditSlotsLeftPos[0], horizontalEditSlotsLeft[0]);
                Tween completionT = completionObj.transform.DOLocalMove(newCompleteObjPos, transformTime);
                sequence.Join(completionT);

                Sequence moveHorizS = MoveToHorizontal(horizontalEditSlotsLeft, horizontalEditSlotsLeftPos, horizontalEditSlotsRight, horizontalEditSlotsRightPos);

                if(fromLayout == ActivityLayout.Hidden)
                {
                    for (int i = 0; i < hiddenSlots.Count; i++)
                    {
                        GameObject panel = hiddenSlots[i];
                        Tween panelScaleT = panel.transform.DOScale(Vector3.one, transformTime);
                        sequence.Join(panelScaleT);
                    }
                }

                sequence.Join(moveHorizS);
            }
            else if (currentLayout == ActivityLayout.HorizontalConsume)
            {
                SetAllPanelEnabled(true);
                
                //update position of the planning top panel
                if (SBContextManager.Instance.context.isPlanning)
                {
                    Vector3 newPlanningTopPanelPos = CalculatePlanningTopPanelPos(horizontalSlotsLeftPos[0], horizontalSlotsLeft[0]);
                    Tween planningTopT = planningTopPanel.transform.DOLocalMove(newPlanningTopPanelPos, transformTime);
                    sequence.Join(planningTopT);
                }

                //update the position of completion object on top
                completionObj.SetActive(true);
                Vector3 newCompleteObjPos = CalculateCompleteObjPos(horizontalSlotsLeftPos[0], horizontalSlotsLeft[0]);
                Tween completionT = completionObj.transform.DOLocalMove(newCompleteObjPos, transformTime);
                sequence = sequence.Join(completionT);

                Sequence moveHorizS = MoveToHorizontal(horizontalSlotsLeft, horizontalSlotsLeftPos, horizontalSlotsRight, horizontalSlotsRightPos);

                if (fromLayout == ActivityLayout.Hidden)
                {
                    for (int i = 0; i < hiddenSlots.Count; i++)
                    {
                        GameObject panel = hiddenSlots[i];
                        Tween panelScaleT = panel.transform.DOScale(Vector3.one, transformTime);
                        sequence.Join(panelScaleT);
                    }
                }

                sequence.Join(moveHorizS);
            }

            //sequence.AppendInterval(transformTime * 0.75f);
            if (currentLayout != ActivityLayout.Hidden)
            {
                //watch player, for all panels

                //Sequence watchPlayerS = DOTween.Sequence();
                //for (int i = 0; i < activityObjList.Count; i++)
                //{
                //    Tween t = WatchPlayer(activityObjList[i]);
                //    watchPlayerS.Join(t);
                //}
                //sequence.Append(watchPlayerS);

                sequence.AppendCallback(() =>
                {
                    for (int i = 0; i < activityObjList.Count; i++)
                    {
                        Tween t = WatchPlayer(activityObjList[i]);
                        sequence.Join(t);
                    }
                });
            }
            

            //call post action
            sequence.AppendCallback(() =>
            {
                //call post action
                postAction?.Invoke();
            });
        }


        private Sequence MoveToHorizontal(
            List<GameObject> slotsLeft, List<Vector3> slotsLeftPos,
            List<GameObject> slotsRight, List<Vector3> slotsRightPos)
        {
            Sequence sequence = DOTween.Sequence();

            //left
            for (int i = 0; i < slotsLeft.Count; i++)
            {
                GameObject panel = slotsLeft[i];
                if (i == 1)
                {
                    horizontalEditSlotsLeftPos[i] = new Vector3(-521.0f, horizontalEditSlotsLeftPos[i].y, horizontalEditSlotsLeftPos[i].z);
                }

                float zMove = CalculateZMove(slotsLeftPos[i]);
                Vector3 targetPos = slotsLeftPos[i] + new Vector3(0, 0, zMove);

                Tween panelMoveT = panel.transform.DOLocalMove(targetPos, transformTime).SetEase(Ease.OutQuint);
                sequence = sequence.Join(panelMoveT);
            }

            //right
            for (int i = 0; i < slotsRight.Count; i++)
            {
                GameObject panel = slotsRight[i];
                if (i == 0)
                {
                    horizontalEditSlotsRightPos[i] = new Vector3(521.0f, horizontalEditSlotsRightPos[i].y, horizontalEditSlotsRightPos[i].z);
                }

                float zMove = CalculateZMove(slotsRightPos[i]);
                Vector3 targetPos = slotsRightPos[i] + new Vector3(0, 0, zMove);

                Tween panelMoveT = panel.transform.DOLocalMove(targetPos, transformTime).SetEase(Ease.OutQuint);
                sequence = sequence.Join(panelMoveT);
            }

            return sequence;
        }


        private Tween RotateToAsAnchorBody(GameObject panel)
        {
            GameObject bodyCenter = GetComponentInParent<AnchorController>().bodyCenter;
            Quaternion rotation = Quaternion.LookRotation(bodyCenter.transform.forward);
            Tween rotationT = panel.transform.DORotateQuaternion(rotation, transformTime * 0.5f);
            return rotationT;
        }


        private Tween WatchPlayer(GameObject panelObj)
        {
            //GameObject target = mainCam;
            GameObject target = watchingTarget;
            lookAtTargetPos.Set(target.transform.position.x, panelObj.transform.position.y, target.transform.position.z);
            Tween t = panelObj.transform.DOLookAt(lookAtTargetPos, 0.3f); 
            return t;
        }
        

        private Vector3 CalculatePlanningTopPanelPos(Vector3 centerPos, GameObject centerPanelObj)
        {
            return centerPos 
                   + new Vector3(0, centerPanelObj.GetComponent<SBActivity>().GetHeight() / 2, 0) 
                    + completionObjExtraGap4Planning;
        }
        

        private Vector3 CalculateCompleteObjPos(Vector3 centerPos, GameObject centerPanelObj)
        {
            return centerPos + 
                new Vector3(0, centerPanelObj.GetComponent<SBActivity>().GetHeight() / 2, 0) + 
                (SBContextManager.Instance.context.isPlanning ? completionObjGap + completionObjExtraGap4Planning + new Vector3(0, 100, 0) : completionObjGap);
        }


        private float CalculateZMove(Vector3 panelSlotPosition)
        {
            //print($"-=- panelSlotPosition = {panelSlotPosition}");
            double radius = 800f;
            if (activityObjList.Count > 3) {
                radius = 2400f;
            }
            double distanceToCenter = Vector3.Distance(panelSlotPosition, originUp);
            double zMove = radius - Math.Sqrt(Math.Pow(radius, 2f) - Math.Pow(distanceToCenter, 2f));
            
            return (float)zMove;
        }
        
        
        //------------------------------ transform ------------------------------


        public void SetLayout(ActivityLayout layout, bool forceUpdate = false, Action postAction = null)
        {
            if (!forceUpdate)
            {
                if (currentLayout == layout)
                    return;
            }

            fromLayout = currentLayout;
            currentLayout = layout;
            
            //build (calculate)
            BuildLayouts();

            //apply
            ApplyLayout(postAction);
        }


        private void SetAllPanelEnabled(bool enabled)
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {
                if(enabled) {
                    activityObjList[i].SetActive(true);
                }
                else if (activityObjList[i].GetComponent<TriviaActivity>() != null ||
                         activityObjList[i].GetComponent<PostActivity>() != null) {
                    activityObjList[i].SetActive(false);
                }
                else if (activityObjList[i].GetComponent<PhotoVideoActivityForConsume>() != null){
                    if(activityObjList[i].GetComponent<PhotoVideoActivityForConsume>().isAllClipLoaderTimerWork) {
                        activityObjList[i].GetComponent<PhotoVideoActivityForConsume>().OnPauseButton();
                    }
                }
                else if (activityObjList[i].GetComponent<AudioActivity>() != null){
                    activityObjList[i].GetComponent<AudioActivity>().OnPauseButton();
                }
            }
        }


        private IEnumerator WaitAndHidePanels()
        {
            yield return new WaitForSeconds(transformTime * 0.2f);
            SetAllPanelEnabled(false);
        }


        //------------------------------------------ Completion effect ----------------------------------------

        public void UpdateCompletionObj(bool isPoI)
        {
            completionObj.SetActive(true);

            if (this.ActivitiesCompleted)
            {
                completionObj.GetComponent<MeshRenderer>().material = completedMat;
            }
            else
            {
                if (isPoI)
                {
                    completionObj.GetComponent<MeshRenderer>().material = poiMat;
                }
                else
                {
                    completionObj.GetComponent<MeshRenderer>().material = nonPoiMat;
                }    
            }
        }
        

        public void LightUp(bool isLightUp)
        {
            if (isLightUp)
            {
                if (this.ActivitiesCompleted)
                {
                    glowObj.SetActive(false);
                    completedGlowObj.SetActive(true);
                }
                else
                {
                    glowObj.SetActive(true);
                    completedGlowObj.SetActive(false);
                }
            }
            else
            {
                glowObj.SetActive(false);
                completedGlowObj.SetActive(false);
            }
        }


        public void PlayCheckinEffect(Action callback = null)
        {
            StartCoroutine(EffectLoop(callback));
        }

        private float loopTimeLimit = 2.0f;
        IEnumerator EffectLoop(Action callback = null)
        {
            GameObject effectPlayer = (GameObject)Instantiate(
                completionEffectPrefab, completionObj.transform.position, completionObj.transform.rotation);
            yield return new WaitForSeconds(loopTimeLimit);

            Destroy(effectPlayer);

            //disable the completing object
            //completionObj.SetActive(false); //according to Martin: the complete object will be kept 

            //loopping
            //PlayCheckinEffect();
            callback?.Invoke();
        }


        //------------------------------- Completetion: conclude ActivityInfo list --------------------------------

        public AnchorActivityInfo ConcludeActivityInfoList()
        {
            //Take out the default PoI(Post) and leave other activity panels to the activityInfoList
            AnchorActivityInfo resultInfo = new AnchorActivityInfo();

            var activityInfoList = new List<IActivityInfo>();
            for (int i = 0; i < this.activityObjList.Count; i++)
            {
                var activityInfo = activityObjList[i].GetComponent<ISBActivity>().GetActivityInfo();

                if (activityInfo.Id == Const.ACTIVITY_DEFAULT_ID && activityInfo is PostActivityInfo info)
                {
                    resultInfo.postInfo = info;
                }
                else
                {
                    activityInfoList.Add(activityInfo);
                }
            }
            resultInfo.activityInfoList = activityInfoList;

            return resultInfo;
        }

        //------------------------------ other ------------------------------


        public ActivityLayout GetCurrentLayout()
        {
            return this.currentLayout;
        }


        public void ToggleLayout()
        {
            if (!SBContextManager.Instance.context.IsConsuming() && !SBContextManager.Instance.context.isEditing) //if it's purely creator
            {
                if (!GetComponentInParent<AnchorController>().isConfigCompleted)
                    return;
            }

            if (currentLayout == ActivityLayout.Hidden)
            {
                SetLayout(SBContextManager.Instance.IsCreating()
                    ? ActivityLayout.HorizontalEdit
                    : ActivityLayout.HorizontalConsume);
                // if (SBContextManager.Instance.IsEditCreating()) {
                //     AnchorManager.Instance.RotateAllAnchors();
                //     GetComponentInParent<AnchorController>().StopRotate(true);
                // }
                GetComponentInParent<AnchorController>().StopRotate(true);//TODO to be replaced with engagement enhancement
            }
            else
            {
                SetLayout(ActivityLayout.Hidden);
                // if (SBContextManager.Instance.IsEditCreating()) {
                //     GetComponentInParent<AnchorController>().StopRotate(false);
                // }
                GetComponentInParent<AnchorController>().StopRotate(false);//TODO to be replaced with engagement enhancement
            }
        }


        /// <summary>
        /// Indicating whether any activity other than a Post is created.
        /// </summary>
        /// <returns></returns>
        public bool IsAnyActivityCreated()
        {            
            return this.activityObjList.Count > 1;            
        }
         
        public bool HasCheckIn()
        {
            if (IsAnyActivityCreated())
                return false;

            var post = (PostActivityInfo)(GetPostActivity()?.GetActivityInfo());
            
            return post!=null && post.HasCheckIn;
        }

        ///// <summary>
        ///// Returns true if there is at least one saved photo/video or audio activity in the AR session.
        ///// </summary>
        ///// <returns></returns>
        //public bool HasSubmittedMedia()
        //{
        //    var hasSubmittedMedia = false;
        //    var activity = GetMediaBasedActivity();
        //    if (activity != null)
        //    {                
        //        print($"hasmedia > activityId = {activity.GetActivityId()}");
        //    }
            
        //    var photoActivity = (PhotoVideoActivity)activity;
        //    if (photoActivity != null)
        //    {                
        //        hasSubmittedMedia = photoActivity.GetActivityId() != Const.ACTIVITY_DEFAULT_ID;
        //        print($"hasmedia > HasSubmittedMedia (photovideo) = {hasSubmittedMedia}");
        //    }
        //    else
        //    {
        //        var audioActivity = (AudioActivity)activity;
        //        if (audioActivity != null)
        //        {
        //            hasSubmittedMedia = audioActivity.GetActivityId() != Const.ACTIVITY_DEFAULT_ID;
        //            print($"hasmedia > HasSubmittedMedia (audio) = {hasSubmittedMedia}");
        //        }
        //    }

        //    return hasSubmittedMedia;
        //}

        public void SetThisRoundActCreationDone()
        {
            newRound = true;
        }

        public bool HasAlreadyCreatedThisType(ActivityType type)
        {
            for (int i = 0; i < activityObjList.Count; i++)
            {
                try
                {                    
                    var sbActivity = activityObjList[i].GetComponent<ISBActivity>();                    
                    if (sbActivity != null &&
                        (type == ActivityType.Trivia && sbActivity.IsTrivia()
                        || type == ActivityType.PhotoVideo && sbActivity.IsPhotoVideo()
                        || type == ActivityType.Audio && sbActivity.IsAudio()))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    print($"ActivityManager.GetPostActivityFromTheAnchor Exception: {ex.Message}");                    
                }
            }

            return false;
        }
         
        #region SBRestClient callbacks
          
        string GetPostActivityId()
        {
            // Get the submitted activity from the list.
            print($"ActivityManager.GetPostActivityFromTheAnchor: activityObjList.Count = {activityObjList.Count}");
            for (int i = 0; i < activityObjList.Count; i++)
            {
                try
                {
                    var sbActivity = activityObjList[i].GetComponent<ISBActivity>();
                    if (sbActivity != null && sbActivity.IsPost())
                    {
                        var id = sbActivity.GetActivityId();
                        print($"ActivityManager.GetPostActivityFromTheAnchor: post activity found with ID={id}.");
                        return id;
                    }
                }
                catch (Exception ex)
                {
                    print($"ActivityManager.GetPostActivityFromTheAnchor Exception: {ex.Message}");
                }
            }

            print($"ActivityManager.GetPostActivityFromTheAnchor: post activity CANNOT be found.");
            return string.Empty;
        }

        #endregion

        public void EnableAllSaveButtons(bool enable, SBActivity activity) {
            print($"ActivityManager EnableAllSaveButtons enable = {enable}, activitySBList.Count = {activitySBList.Count}");
            foreach (var sbActivity in activitySBList) {
                if(sbActivity != null) {
                    sbActivity.saveEditButton.interactable = enable;
                    sbActivity.saveCreateButton.interactable = enable;
                    sbActivity.canNotBeSaved = !enable;
                }
            }
            if(!enable) {
                activity.canNotBeSaved = enable;
            }
        }

    }
}
