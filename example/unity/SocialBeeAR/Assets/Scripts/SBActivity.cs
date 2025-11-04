using System;
using GifPlayer;
using SocialBeeARDK;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    public interface ISBActivity
    {
        bool IsTypeOf(Type type);
        bool IsPost();
        bool IsTrivia();
        bool IsPhotoVideo();
        bool IsAudio();
           
        void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "");
        void OnFailedSave(ErrorInfo error);
        void OnSuccessfulSave();

        float GetHeight();
        float GetWidth();
        void UpdateActivityStatus(ActivityStatus status);
        void UpdateActivityId(string id);
        void UpdateParentId(string id);
        IActivityInfo GetActivityInfo();
        string GetActivityId();
        string GetPostTitle();
        string GetPostDescription();
    }

    public class SBActivity : MonoBehaviour, ISBActivity
    {
        // Flag that determines if the activity is still being processed by the server.
        protected bool isSaving = false;
        protected IActivityInfo uiValues;
        //activity data
        protected IActivityInfo activityInfo;
        public IActivityInfo GetActivityInfo()
        {
            return activityInfo;
        }
        /// <summary>
        /// The data of the completed activity.
        /// </summary>
        protected IActivityInfo completedActivity;

        protected bool isCompleted;
        protected bool isReborn;

        protected AnchorController anchorController;

        [HideInInspector]
        protected static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;
        public bool IsTypeOf(Type type)
        {
            return GetActivityInfo().GetType() == type;
        }

        public bool IsPost()
        {
            return GetActivityInfo().GetType() == typeof(PostActivityInfo);
        }

        public bool IsTrivia()
        {
            return GetActivityInfo().GetType() == typeof(TriviaActivityInfo);
        }

        public bool IsPhotoVideo()
        {
            return GetActivityInfo().GetType() == typeof(PhotoVideoActivityInfo);
        }

        public bool IsAudio()
        {
            return GetActivityInfo().GetType() == typeof(AudioActivityInfo);
        }

        public bool IsQuickPhotoVideo()
        {
            var info = GetActivityInfo();
            return info.GetType() == typeof(PhotoVideoActivityInfo) && ((PhotoVideoActivityInfo)info).IsQuickPhotoVideo;
        }

        public void SetAsQuickPhotoVideo()
        {
            var info = GetActivityInfo();
            ((PhotoVideoActivityInfo)info).IsQuickPhotoVideo = true;
        }

        //--------------------------------- UIMode --------------------------------
        protected UIMode mode = UIMode.Undefined;
        public enum UIMode
        {
            Undefined,
            Edit,
            Preview,
            Play,
            Completed,
            Saving
        }

        public Action<IActivityOutput> OnActivitySuccessfulSaved;

        //GameObjects
        [SerializeField] protected GameObject editPanel;
        [SerializeField] protected GameObject playPanel;
        [SerializeField] protected GameObject savingPanel;

        [SerializeField] protected GameObject editButtonPanel; //cancel/save button
        [SerializeField] protected GameObject reEditButtonPanel; //cancel/save button
        [SerializeField] protected GameObject previewButtonPanel; //edit/delete button
        [SerializeField] protected GameObject playButtonPanel; //complete/submit button
        public Button saveEditButton;
        public Button saveCreateButton;
        [HideInInspector] public bool canNotBeSaved;
        [HideInInspector] public bool activityWasSaved;

        //indicator of whether it has been saved
        protected bool isEverSaved = false;
        protected NativeCall nativeCall;

        public void Start()
        {
            SBRestClient.Instance.OnPostHasBeenMarkedAsCheckIn += OnPostHasBeenMarkedAsCheckIn;
            SBRestClient.Instance.OnActivityHasBeenSubmitted += OnActivityHasBeenSubmitted;
            SBRestClient.Instance.OnActivityHasBeenDeleted += OnActivityHasBeenDeleted;
            SBRestClient.Instance.OnActivityHasBeenCompleted += OnActivityHasBeenCompleted;
            nativeCall = NativeCall.Instance;
        }

        protected void InitUIValues()
        {
            uiValues = activityInfo.Clone();
            //print($"InitUIValues cloned uiValues = {uiValues}");
        }

        public virtual void SetUIMode(UIMode mode)
        {
            print($"SBActivity.SetUIMode = {mode}");
            this.mode = mode;
            if (SBContextManager.Instance.IsCreating()) {
                PointsBarManager.Instance.HidePointsBar();
            }

            switch (mode)
            {
                case UIMode.Saving:
                    editPanel.SetActive(false);
                    playPanel.SetActive(false);
                    ShowSavingPanel(true);

                    // Disable/hide all type of buttons.
                    editButtonPanel?.SetActive(false);
                    reEditButtonPanel?.SetActive(false);
                    previewButtonPanel?.SetActive(false);
                    playButtonPanel?.SetActive(false);
                    
                    anchorController.EnableAllSaveButtons(false, this);
                    ActivityUIFacade.Instance.EnableBackButton(false);
                    break;

                case UIMode.Edit:
                    editPanel.SetActive(true);
                    playPanel.SetActive(false);
                    ShowSavingPanel(false);

                    //note: different panels for the 1st time and other times
                    editButtonPanel?.SetActive(!isEverSaved);
                    reEditButtonPanel?.SetActive(isEverSaved);

                    previewButtonPanel?.SetActive(false);
                    playButtonPanel?.SetActive(false);
                    EnableInteractionAllActivities(false);
                    ActivityUIFacade.Instance.EnableBackButton(true);
                    break;

                case UIMode.Preview:
                    editPanel.SetActive(false);
                    playPanel.SetActive(true);
                    ShowSavingPanel(false);

                    editButtonPanel?.SetActive(false);
                    reEditButtonPanel?.SetActive(false);
                    previewButtonPanel?.SetActive(true);
                    playButtonPanel?.SetActive(false);
                    PointsBarManager.Instance.ShowPointsBar();
                    EnableInteractionAllActivities(true);
                    break;

                case UIMode.Play:
                    print($"SetUIMode > editPanel==null? {editPanel==null} | playPanel==null? {playPanel==null}  | editButtonPanel==null? {editButtonPanel==null}  " +
                          $"| reEditButtonPanel==null? {reEditButtonPanel==null}  | previewButtonPanel==null? {previewButtonPanel==null}  " +
                          $"| playButtonPanel==null? {playButtonPanel==null}  #debugconsume");
                    editPanel.SetActive(false);
                    playPanel.SetActive(true);
                    ShowSavingPanel(false);

                    editButtonPanel?.SetActive(false);
                    reEditButtonPanel?.SetActive(false);
                    previewButtonPanel?.SetActive(false);
                    if(SBContextManager.Instance.IsConsuming() && IsPost() && activityInfo is PostActivityInfo info) {
                        playButtonPanel?.SetActive(info.HasCheckIn);
                    }
                    else {
                        playButtonPanel?.SetActive(true);
                    }
                    ActivityUIFacade.Instance.EnableBackButton(true);
                    break;

                case UIMode.Completed:
                    SetUIMode(UIMode.Play);
                    playButtonPanel?.SetActive(false);
                    PointsBarManager.Instance.ShowPointsBar();
                    break;
            }
        }


        private void ShowSavingPanel(bool visible)
        {
            if (savingPanel == null)
                return;

            UnityGif uGif = savingPanel.GetComponentInChildren<UnityGif>();
            if (visible)
            {
                savingPanel.SetActive(true);
                uGif?.Play();
            }
            else
            {
                uGif?.Pause();
                savingPanel.SetActive(false);
            }
        }


        //--------------------------------- Init/Reborn --------------------------------

        /// <summary>
        /// Spawn the activity panel.
        /// </summary>
        /// <param name="id">The Id of the activity.</param>
        /// <param name="type">The type of the activity.</param>
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="anchorPose">The information about the location and rotation.</param>
        /// <param name="parentId">The Id of the parent activity. This should be empty for a post type.</param>
        /// <param name="mapId">The Id of the map.</param>
        /// <remarks>
        /// We want to break the dependency from singleton objects as much possible
        /// so that's why we are receiving the experienceId here.
        /// </remarks>
        public virtual void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "")
        {
            print($"SBActivity.Born - started: id={id}, parentId={parentId}, experienceId={experienceId}, type={type}, anchorPayload={anchorPayload} #payload");
            if (type == ActivityType.Post)
                activityInfo = new PostActivityInfo();
            else if (type == ActivityType.Trivia)
                activityInfo = new TriviaActivityInfo();
            else if (type == ActivityType.PhotoVideo || type == ActivityType.Video)
                activityInfo = new PhotoVideoActivityInfo();
            else if (type == ActivityType.Audio)
                activityInfo = new AudioActivityInfo();
            //activityInfo = default(T);
            activityInfo.ExperienceId = experienceId;
            activityInfo.Id = id;
            activityInfo.Pose = anchorPose;
            activityInfo.ParentId = parentId;
            activityInfo.MapId = mapId;
            activityInfo.AnchorPayload = anchorPayload;

            //register to activityManager, setting anchorController
            RegisterAsActiveActivity(true);
            anchorController = GetComponentInParent<AnchorController>();
            anchorController.OnThumbnailUploaded += OnThumbnailUploaded;

            //set mode
            SetUIMode(UIMode.Edit);

            //init values on UI
            InitUIValues();
            
            //hide Off-Screen indicator
            if(type == ActivityType.PhotoVideo || type == ActivityType.Video || type == ActivityType.Audio) {
                OffScreenIndicatorManager.Instance.HideArrow();
            }
        }

        void OnThumbnailUploaded(string thumbnail, ErrorInfo error)
        {            
            AssignThumbnail(thumbnail, error);
        }

        protected virtual void AssignThumbnail(string thumbnail, ErrorInfo error)
        {            
        }

        public virtual void Reborn(IActivityInfo info)
        {
            print($"SBActivity.Reborn - started: info is null? {info==null} #debugconsume");
            info.IsEditing = true;
            activityInfo = info;
            completedActivity = info;
            isEverSaved = true;

            print($"SBActivity.Reborn > RegisterAsActiveActivity #debugconsume");
            //register to activityManager, setting anchorController
            RegisterAsActiveActivity(true);
            anchorController = GetComponentInParent<AnchorController>();
            print($"SBActivity.Reborn > anchorController=null? {anchorController==null} #debugconsume");

            //set mode
            SetUIMode(SBContextManager.Instance.IsCreating() ? UIMode.Preview : UIMode.Play);

            //init values on UI
            InitUIValues();
            
            activityWasSaved = true;
        }


        protected virtual void ApplyDataToUI()
        {
        }

        protected virtual void ApplyDataToPreviewPlayPanel()
        {
        }


        //--------------------------------- size info --------------------------------

        //The width and height represent the size info of an panel, which will be used for layout management.
        [SerializeField] private float height;
        public float GetHeight()
        {
            return height;
        }


        [SerializeField] private float width;
        public float GetWidth()
        {
            return width;
        }


        //------------------- buttons in the bottom, for switching modes -------------------
        public virtual void OnSave() //'save' button in edit mode
        {
            // This ensures we will not run the Save process twice, unnecessarily.
            if (isSaving)
            {
                print("Still saving, exiting now.");
                return;
            }
            print($"1 - Memory used={System.GC.GetTotalMemory(true)}");
            //update anchor pose data for this activity, as anchor might be at a different pose after it's born
            activityInfo.Pose = anchorController.GetAnchorInfo().pose;
            uiValues.Pose = activityInfo.Pose;

            //pre-action for data saving
            isSaving = true;
            SetUIMode(UIMode.Saving); //show saving progress indicator
            // ActiveAnchorManager.SetInteractable(false); //disable interaction for ALL anchors (including their body and acticities)
            GPSAnchorManager.Instance.SetInteractable(false); //disable interaction for all GPS anchors
            
            ActivityUIFacade.Instance.SetNextOrSaveButtonsVisible(false, false, false);
            print($"2 - Memory used={System.GC.GetTotalMemory(true)}");
        }

        /// <summary>
        /// This is the method to call as the continuation for the <see cref="OnSave"/> method.
        /// This serves as a callback function after the API call that submits the activity.
        /// Call this method when the API succeed.
        /// </summary>
        public virtual void OnSuccessfulSave()
        {
            OnSuccessfulSaveHelper(false);
            print($"1 - Memory used={System.GC.GetTotalMemory(true)}");
            if (!RecordManager.Instance.isPhotoTaking) {
                RecordManager.Instance.ShowVideoOnPanel();
            }
            // This is not set in the OnSuccessfulSave of the specific activity type.
            //RecordManager.Instance.isNewMedia = false;

            OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform);
            OffScreenIndicatorManager.Instance.ShowArrow();
            
            if(anchorController.isReborn) {
                if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors) {
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextOrCompleteGPSOnlyCreation);
                }
                else {
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.FinishEdit);
                }
            }

            activityWasSaved = true;
            anchorController.CheckActivityCount();
            print($"2 - Memory used={System.GC.GetTotalMemory(true)}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isPoI">True, if the activity submitted is a POI.</param>        
        void OnSuccessfulSaveHelper(bool isPoI = false)
        {
            print($"SBActivity.OnSuccessfulSaveHelper");
            print($"1 - Memory used={System.GC.GetTotalMemory(true)}");
            isSaving = false;
            uiValues.IsEditing = true;
            activityInfo = uiValues.Clone();
            activityInfo.IsEditing = true;
            if (activityInfo is PostActivityInfo info)
            {
                info.HasCheckIn = isPoI;                
            }

            if (activityInfo is PhotoVideoActivityInfo pinfo)
            {
                print($"SBActivity.OnSuccessfulSaveHelper: IsQuickPhotoVideo={pinfo.IsQuickPhotoVideo}");
            }

            if (isPoI) {
                anchorController.anchorWasCompleted = false;
            }
            var isNew = !isEverSaved;
            isEverSaved = true;
            SetUIMode(UIMode.Preview);
            // if (anchorController.anchorWasCompleted) {
            //     //re-enable interaction for ALL anchors (including their body and acticities)
            //     ActiveAnchorManager.SetInteractable(true);
            // }

            GPSAnchorManager.Instance.SetInteractable(true); //re-enable interaction for all GPS anchors

            //apply data to play/preview mode
            ApplyDataToPreviewPlayPanel();

            //report to ActivityManager that we can start a new round            
            GetComponentInParent<ActivityManager>().SetThisRoundActCreationDone();

            //var isEditingPhoto = activityInfo is PhotoVideoActivityInfo;
            var isQuickCreate = SBContextManager.Instance.context.startWithPhotoVideo;
            if (!isPoI && (!isQuickCreate || isQuickCreate && !isNew))
            {                
                //report to AnchorController to re-enable the activity options to create            
                GetComponentInParent<AnchorController>().OnActivityCreationCompletedOrCanceled();
            }
            else if (isPoI)
            {
                GetComponentInParent<AnchorController>().EnablePoIButton();
            }
            // We only need the value of the flag for the first time.
            // Let's reset it here so other anchors will not be flagged as Quick Create.
            SBContextManager.Instance.context.startWithPhotoVideo = false;
            print($"2 - Memory used={System.GC.GetTotalMemory(true)}");
        }

        /// <summary>
        /// This is the method to call as the continuation for the <see cref="OnSave"/> method.
        /// This serves as a callback function after the API call that submits the activity fails.        
        /// </summary>
        public virtual void OnFailedSave(ErrorInfo error)
        {
            print($"ErrorHandler > SBActivity.OnFailedSave: errorCode={error?.ErrorCode ?? -911}");
            isSaving = false;
            
            SetUIMode(UIMode.Edit);
            //playPanel.SetActive(true);            
            // ActiveAnchorManager.SetInteractable(true); //re-enable interaction for ALL anchors (including their body and acticities)
            GPSAnchorManager.Instance.SetInteractable(true); //re-enble interaction for all GPS anchors
            
            var message = error?.ErrorCode == ErrorCodes.NetworkError
                ? "It seems like your connection is too slow or you are not connected to the internet."
                : error?.Message;
            if (RecordManager.Instance.isPhotoTaking && String.IsNullOrWhiteSpace(RecordManager.Instance.audioPath)) {
                print($"SBActivity   RecordManager.Instance.isPhotoTaking");
                BottomPanelManager.Instance.ShowMessagePanel(message.IsNullOrWhiteSpace()
                    ? "There was an issue submitting your photo. You may try again later."
                    : message);
                if(!IsPost()) {
                    playPanel.SetActive(true);
                }
            }
            else if(!String.IsNullOrWhiteSpace(RecordManager.Instance.audioPath)) {
                print($"SBActivity   !String.IsNullOrWhiteSpace(RecordManager.Instance.audioPath)");                
                BottomPanelManager.Instance.ShowMessagePanel(message.IsNullOrWhiteSpace()
                    ? "There was an issue submitting your audio. You may try again later."
                    : message);
            }
            else if(!RecordManager.Instance.isPhotoTaking && !String.IsNullOrWhiteSpace(RecordManager.Instance.filteredFilePath)) {
                print($"SBActivity   !RecordManager.Instance.isPhotoTaking && !String.IsNullOrWhiteSpace(RecordManager.Instance.filteredFilePath) ()");                
                BottomPanelManager.Instance.ShowMessagePanel(message.IsNullOrWhiteSpace()
                    ? "There was an issue submitting your video. You may try again later."
                    : message);
                playPanel.SetActive(true);
            }
            else  {
                print($"SBActivity   else");                
                BottomPanelManager.Instance.ShowMessagePanel(message.IsNullOrWhiteSpace()
                    ? "There was an issue submitting your trivia. You may try again later."
                    : message);
            }
        }

        protected void ContinueOnError(ErrorInfo error)
        {
            //BottomPanelManager.Instance.ShowMessagePanel("Your content cannot be uploaded at this time. Please try again later.");
            BottomPanelManager.Instance.UpdateMessage(error?.Message ?? "Your content cannot be uploaded at this time. Please try again later.");
            OnFailedSave(error);
        }

        /// <summary>
        /// This is the method to call as the continuation for the <see cref="DoDelete"/> method.
        /// This serves as a callback function after the API call that deletes the activity.
        /// Call this method when the API succeed.
        /// </summary>
        public virtual void OnSuccessfulDelete()
        {
            print("Done deleting activity from SB server!");
            
            //re-enable interaction after deletion is done
            ActiveAnchorManager.SetInteractable(true); //re-enable interaction for ALL anchors (including their body and acticities)
            GPSAnchorManager.Instance.SetInteractable(true); //re-enable interaction for all GPS anchors
            
            //delete panel object
            OnCancelCreate();
        }

        /// <summary>
        /// The callback function when the API call for consuming an activity succeeded.
        /// </summary>
        /// <remarks>
        /// A consumed activity can return as incorrectly answered (trivia) or a photo that failed keywords validation.
        /// This is still the callback that will be called as those are treated as successful API calls.
        /// </remarks>
        public virtual void OnSuccessfulConsume()
        {
            isSaving = false;
        }

        public virtual void OnCancelCreate() //'cancel' button in edit mode
        {
            EnableInteractionAllActivities(true);
            RegisterAsActiveActivity(false);

            //report to ActivityManager that we can start a new round
            GetComponentInParent<ActivityManager>().SetThisRoundActCreationDone();

            //report to AnchorController to re-enable the activity options to create
            //GetComponentInParent<AnchorController>().OnActivityCreationCompletedOrCanceled();
            
            //delete it
            GetComponentInParent<ActivityManager>().DeleteActivity(GetActivityId());
            
            OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform);
            OffScreenIndicatorManager.Instance.ShowArrow();
            
            if (IsTrivia()) {
                anchorController.TriviaExist(false);
            }
            if (IsPhotoVideo()) {
                anchorController.PhotoVideoExist(false);
            }
            if (IsAudio()) {
                anchorController.AudioExist(false);
            }
        }


        public virtual void OnEdit() //'edit' button in preview mode
        {
            RegisterAsActiveActivity(true);
            
            anchorController.anchorWasCompleted = false;
            SetUIMode(UIMode.Edit);

            // Mark the activity for editing.
            activityInfo.IsEditing = true;
            if(activityInfo.Type == ActivityType.PhotoVideo ||
               activityInfo.Type == ActivityType.Video ||
               activityInfo.Type == ActivityType.Audio) {
                OffScreenIndicatorManager.Instance.HideArrow();
            }
        }


        public virtual void OnCancelEdit() //on cancel the edit of the activity panel
        {
            SetUIMode(UIMode.Preview);
            
            RegisterAsActiveActivity(false);

            //report to ActivityManager that we can start a new round            
            GetComponentInParent<ActivityManager>().SetThisRoundActCreationDone();

            //report to AnchorController to re-enable the activity options to create            
            GetComponentInParent<AnchorController>().OnActivityCreationCompletedOrCanceled();
            OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform);
            OffScreenIndicatorManager.Instance.ShowArrow();
        }


        public virtual void OnDelete() //'delete' button in preview mode
        {
            RegisterAsActiveActivity(true);
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.DeletingActivity);
        }


        public void DoDelete()
        {
            RegisterAsActiveActivity(false);            

            //as the deletion might take time due to networking situation, disable interaction until it's done
            ActiveAnchorManager.SetInteractable(false); //disable interaction for ALL anchors (including their body and acticities)
            GPSAnchorManager.Instance.SetInteractable(false); //disable interaction for all GPS anchors
            
            print("Deleting activity from SB server...");
            SBRestClient.Instance.DeleteActivity(activityInfo.ExperienceId, activityInfo.Id);
        }


        public virtual void OnComplete() // 'submit', or 'complete' button in play mode
        {
            RegisterAsActiveActivity(false);

            SetUIMode(UIMode.Completed);


            //Todo: update data (activityInfo) to cloud here (user submission)
            //...

            // Transform tr = ActiveAnchorManager.GetNeededConsumeAnchorController().transform;
            // ArrowForMoveDeviceManager.Instance.SetTarget(tr);
            // ArrowForMoveDeviceManager.Instance.ShowArrow();
        }


        //----------------------- edit interaction ------------------------


        protected void OnEditTextBox(GameObject textBoxObj, GameObject coverObj, Action<InputField> postAction)
        {
            RegisterAsActiveActivity(true);
            
            //disable cover
            coverObj.SetActive(false);

            // Activate input field
            InputField input = textBoxObj.GetComponentInChildren<InputField>();
            input.interactable = true;
            input.ActivateInputField();
            // print("keyboard launched");
            var ph = textBoxObj.GetComponentInChildren<Text>();
            if (input.touchScreenKeyboard != null)
            {
                input.touchScreenKeyboard.text = ph != null ? ph.text : input.text;    
            }
            
            input.onEndEdit.AddListener(delegate { OnEditTextBoxDone(textBoxObj, coverObj, postAction); });
        }


        protected void OnEditTextBoxDone(GameObject textBoxObj, GameObject coverObj, Action<InputField> postAction)
        {
            //disable input field
            InputField input = textBoxObj.GetComponentInChildren<InputField>();
            input.DeactivateInputField();
            input.interactable = false;

            //re-enable cover, so that user can edit again
            coverObj.SetActive(true);

            //post action: e.g. using the input value to update some attributes
            postAction?.Invoke(input);
        }


        //------------------- other -------------------
        void UpdateExperiencePoints(int points)
        {
            activityInfo.Points = points;                    
            if (!activityInfo.IsEditing || SBContextManager.Instance.IsConsuming())
            {
                SBContextManager.Instance.UpdatePoints(points);                
            }
        }

        public void UpdateActivityStatus(ActivityStatus status)
        {
            activityInfo.Status = status;
            completedActivity.Status = status;
        }
       
        public void UpdateActivityId(string id)
        {
            if (uiValues != null)
                uiValues.Id = id;
            if (activityInfo != null)
                activityInfo.Id = id;
        }

        public void UpdateParentId(string id)
        {
            if (uiValues != null)
                uiValues.ParentId = id;
            if (activityInfo != null)
                activityInfo.ParentId = id;
        }

        public string GetActivityId()
        {
            return activityInfo == null ? "" : activityInfo.Id;
        }

        public string GetPostTitle()
        {
            return activityInfo == null ? "" : activityInfo.Title;
        }

        public string GetPostDescription()
        {
            if (activityInfo == null)
                return string.Empty;

            if (activityInfo is PostActivityInfo info)
                return info.Description;

            return string.Empty;
        }

        protected void RegisterAsActiveActivity(bool isActive)
        {
            ActivityManager activityManager = transform.parent.gameObject.GetComponent<ActivityManager>();
            if (isActive)
                activityManager.RegisterAsActiveActivity(activityInfo.Id);
            else
                activityManager.ResetActiveActivity();
        }

        private void OnEnable()
        {
            //if (activityInfo != null)
            //    print($"SBActivity.OnEnable: {activityInfo.ToString()}.");
            //else
            //    print("SBActivity.OnEnable: activityInfo is null.");
        }

        private void OnDestroy()
        {
            //Aggregates must be removed when GameObject is destroyed.
            SBRestClient.Instance.OnPostHasBeenMarkedAsCheckIn -= OnPostHasBeenMarkedAsCheckIn;
            SBRestClient.Instance.OnActivityHasBeenSubmitted -= OnActivityHasBeenSubmitted;
            SBRestClient.Instance.OnActivityHasBeenDeleted -= OnActivityHasBeenDeleted;
            SBRestClient.Instance.OnActivityHasBeenCompleted -= OnActivityHasBeenCompleted;             
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isCheckIn"></param>
        protected void OnPostHasBeenMarkedAsCheckIn(string id, bool isCheckIn, ErrorInfo error)
        {
            // this method is called on all anchors
            if (!anchorController.anchorWasCompleted) {
                if (error != null) {
                    // Then the activity was not updated.
                    // Use "error" to notify the user what happened.
                    print($"SBActivity.OnPostHasBeenMarkedAsCheckIn > Error updating the activity: {error}");

                    OnFailedSave(error);
                    return;
                }

                print($"SBActivity.OnPostHasBeenMarkedAsCheckIn > success.");

                OnSuccessfulSaveHelper(true);
                
                nativeCall.OnWillUpdatePost(id, isCheckIn);
            }
        }

        protected void OnActivityHasBeenSubmitted(IActivityOutput output, string referenceId, ErrorInfo error)
        {
            print($"1 SBActivity.OnActivityHasBeenSubmitted: {(output == null ? "No output" : output.Text)}");
            
            if (error != null)
            {
                // Then the activity was not created.
                // Use "error" to notify the user what happened.
                print($"SBActivity.OnActivityHasBeenSubmitted > Error creating the activity: {error}");

                //if (referenceId.IsNullOrWhiteSpace())
                //{
                //    print($"SBActivity.OnActivityHasBeenSubmitted > referenceId IsNullOrWhiteSpace");
                //    // Let's deliberately throw an error to remind the devs that the implementation is wrong.
                //    throw new ArgumentNullException("referenceId cannot be null or empty.");
                //}

                //if (referenceId != activityInfo.Id)
                //{
                //    print($"error!=null > SBActivity.OnActivityHasBeenSubmitted - exiting now: referenceId={referenceId}, myId={activityInfo.Id}");
                //    return;
                //}

                print($"SBActivity.OnActivityHasBeenSubmitted > calling OnFailedSave.");
                OnFailedSave(error);
                return;
            }

            // It's either there is an error or there is an output
            // but we want to be safe here to prevent run-time error.
            if (output == null)
            {
                // We really should not encounter this but let's handle this worst-case scenario.
                print($"SBActivity.OnActivityHasBeenSubmitted > Both error and output are null.");
                return;
            }

            if (output.ReferenceId.IsNullOrWhiteSpace())
            {
                // Let's deliberately cause an exception as this should not happen!
                throw new ArgumentException("ReferenceId cannot be null or empty.");
            }

            // At this point we are guaranteed that the activity was successfully created or updated.
            print($"2 SBActivity.OnActivityHasBeenSubmitted > Created activity ID = {output.UniqueId}, points earned = {output.PointsCreation}, UniqueId={output.UniqueId}, ReferenceId={output.ReferenceId},  activityInfo.Id={activityInfo.Id}");
            var updatedId = output.ReferenceId.IsNullOrWhiteSpace() ? output.UniqueId : output.ReferenceId;
            if (updatedId != activityInfo.Id)
            {
                print($"SBActivity.OnActivityHasBeenSubmitted - exiting now: referenceId={referenceId}, myId={activityInfo.Id}");
                return;
            }

            print($"3 SBActivity.OnActivityHasBeenSubmitted > UpdateActivityId with = {output.UniqueId}");
            UpdateActivityId(output.UniqueId);
            UpdateExperiencePoints(output.PointsCreation);            
            OnSuccessfulSave();            

            print($"4 SBActivity.OnActivityHasBeenSubmitted > OnActivityHasBeenSubmitted: {output.ToJson()}");

            // ****************************************************
            // ToDo: Disabled while profiling memory consumption
            // ****************************************************
            nativeCall.OnActivitySubmitted(output.ToJson(), ((int)output.Type).ToString());
            
            anchorController.EnableAllSaveButtons(true, this);
        }

        protected void OnActivityHasBeenCompleted(IConsumedActivityOutput output, string referenceId, ErrorInfo error)
        {
            print($"SBActivity.OnActivityHasBeenCompleted: {output.ActivityId}");

            // It's either there is an error or there is an output
            // but we want to be safe here to prevent run-time error.
            if (output == null)
            {
                // We really should not encounter this but let's handle this worst-case scenario.
                print($"SBActivity.OnActivityHasBeenCompleted > Both error and output are null.");
                return;
            }

            if (referenceId != activityInfo.Id)
            {
                // This activity is not the one that was completed.
                print($"SBActivity.OnActivityHasBeenCompleted - exiting now: This activity is not the one that was completed. referenceId={referenceId}, myId={activityInfo.Id}");
                return;
            }

            if (error != null)
            {
                // Then the activity was not completed.
                // Use "error" to notify the user what happened.
                print($"SBActivity.OnActivityHasBeenCompleted > Error completing the activity: {error}");

                OnFailedSave(error);
                return;
            }

            // At this point we are guaranteed that the activity was successfully consumed.
            print($"SBActivity.OnActivityHasBeenCompleted > Completed activity ID = {output.UniqueId}, points earned = {output.Points}, status = {output.Status}");
            UpdateInfo(output);
            UpdateExperiencePoints(output.Points);
            // We are calling success as long as the activity is completed either with a wrong or correct answer
            // or a accepted or failed image keywords.
            OnSuccessfulConsume();

            //update the appearance on the anchor object
            ActivityManager activityManager = GetComponentInParent<ActivityManager>();
            activityManager.UpdateCompletionObj(false);
            activityManager.LightUp(true);

            print($"SBActivity.OnActivityHasBeenCompleted > OnActivityHasBeenCompleted: {completedActivity.ToJson()}");
            nativeCall.OnActivityCompleted(completedActivity.ToJson(), ((int)output.Type).ToString());
        }

        void UpdateInfo(IConsumedActivityOutput output)
        {
            print($"UpdateInfo > Status={output.Status}");
            activityInfo.Status = output.Status;
            completedActivity.Status = output.Status;
            completedActivity.DateCompleted = output.DateCompleted;
            completedActivity.PointsEarned = output.Points;
            completedActivity.CompletedId = output.UniqueId;
        }

        protected void OnActivityHasBeenDeleted(string id, ErrorInfo error)
        {
            print("SBActivity.OnActivityHasBeenDeleted");
            
            if (id != activityInfo.Id)
                return;

            if (error != null)
            {
                // Then the activity was not created.
                // Use "error" to notify the user what happened.
                print($"SBActivity.OnActivityHasBeenDeleted > Error deleting the activity: {error}");
 
                OnFailedSave(error);
                return;
            }
              
            // At this point we are guaranteed that the activity was successfully deleted.
            print("SBActivity.OnActivityHasBeenDeleted");

            OnSuccessfulDelete();

            print($"SBActivity.OnActivityHasBeenDeleted: {id}");
            nativeCall.OnActivityDeleted(id);
        }

        void EnableInteractionAllActivities(bool interaction) {

            print($"EnableInteractionAllActivities interaction = {interaction}");
            // enable only current anchor, while it will be completed
            if (!anchorController.anchorWasCompleted && interaction) {
                ActivityUIFacade.Instance.EnableBackButton(interaction);
                anchorController.SetInteractable(interaction);
                return;
            }
            
            ActivityUIFacade.Instance.EnableBackButton(interaction);
            
            foreach (var anchor in ActiveAnchorManager.GetAnchorObjectList()) {
                var ac = anchor.GetComponent<AnchorController>();
                // enable all anchors
                if (interaction) {
                    ac.SetInteractable(interaction);
                }
                // disable anchors, except anchor which was born
                else if (ac.isScanningCompletedOrSkipped || SBContextManager.Instance.context.isEditing && ac.anchorWasCompleted) {
                    // disable other anchors
                    if (ac.GetAnchorInfo().id != anchorController.GetAnchorInfo().id) {
                        ac.SetInteractable(interaction);
                    }
                }
            }
        }
        
        public void EnableSaveButton(bool enable) {
            print($"-=- SBActivity EnableSaveButton() enable = {enable}, canNotBeSaved = {canNotBeSaved}");
            if(!canNotBeSaved || !enable) {
                saveCreateButton.interactable = enable;
                saveEditButton.interactable = enable;
            }
        }

        public void TapSound() {
            AudioManager.Instance.PlayAudio(AudioManager.AudioOption.Tap);
        }
    }
}
