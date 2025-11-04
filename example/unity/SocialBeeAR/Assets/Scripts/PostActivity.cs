using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace SocialBeeAR
{
    public class PostActivity : SBActivity
    {

        [SerializeField] private GameObject edit_title;
        [SerializeField] private GameObject edit_titleCover;

        [SerializeField] private GameObject edit_desc;
        [SerializeField] private GameObject edit_descCover;

        [SerializeField] private Text play_title;
        [SerializeField] private Text play_desc;

        [SerializeField] private GameObject panelParent;

        [SerializeField] private GameObject completeFrame;

        [SerializeField] private RectTransform descriptionCover;
        [SerializeField] private Image titleCover;
        [SerializeField] private Transform buttonsRoot;
        [SerializeField] private Sprite allRoundedSprite;
        [SerializeField] private Sprite twolRoundedSprite;

        // it need when we save post in indoor (upload thumbnail of post)
        private bool saveExeption;
        
        
        private void Update()
        {
            if (anchorController == null || isCompleted || activityInfo.Status != ActivityStatus.New)
                return;
            
            //checking for completion
            if (!SBContextManager.Instance.context.IsConsuming()) return;

            var distanceToAnchor = Vector3.Distance(transform.position, Camera.main.transform.position);
            if (distanceToAnchor >= Const.DISTANCE_TO_ENGAGE_ANCHOR) return;
                
            var aController = GetComponentInParent<AnchorController>();
            if (aController.isModeSettled || !aController.isModeSettled && !aController.flyingLock)
            {
                // print($"Anchor mode already settled! DistToEngage={Const.DISTANCE_TO_ENGAGE_ANCHOR} | distanceToAnchor={distanceToAnchor} | flyingLock={aController.flyingLock} | isModeSettled={aController.isModeSettled}");
                ConsumePost();
                return;
            }
            
            print($"Anchor mode not yet settled! DistToEngage={Const.DISTANCE_TO_ENGAGE_ANCHOR} | distanceToAnchor={distanceToAnchor} | flyingLock={aController.flyingLock}");
        }
        

        public override void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "")
        {
            print("PostActivity.Born");
            base.Born(id, type, experienceId, anchorPose, parentId, mapId, anchorPayload);

            // By default, the post activity is of "Text" type.
            ((PostActivityInfo)activityInfo).SourceType = PostType.Text;
            ((PostActivityInfo)uiValues).SourceType = PostType.Text;
            OnEditTitle(false);
            //print($"PostActivity.Born: SourceType={((PostActivityInfo)uiValues).SourceType}");             
        }
        

        public override void Reborn(IActivityInfo activityInfo)
        {            
            print($"PostActivity.Reborn - info: {(activityInfo == null ? "": ((PostActivityInfo)activityInfo).ToString())}");
            base.Reborn(activityInfo);

            // For debugging only:
            print($"PostActivity.Reborn - breadcrumb: {SBContextManager.Instance.context.Breadcrumb}");

            //update UI according to the value in activityInfo
            ApplyDataToUI();
        }

        protected override void ApplyDataToUI()
        {
            //1. updating edit UI
            ApplyDataToEditPanel();

            //2. updating preview UI
            ApplyDataToPreviewPlayPanel();
        }


        private void ApplyDataToEditPanel()
        {
            var activityInfo = (PostActivityInfo)this.activityInfo;

            edit_title.GetComponentInChildren<InputField>().text = activityInfo.Title;
            edit_desc.GetComponentInChildren<InputField>().text = activityInfo.Description;

            //reset the 'save' button to 'disabled'
            EnableSaveButton(false);
            HideDescriptionSpace(String.IsNullOrWhiteSpace(activityInfo.Description));
        }
        

        protected override void ApplyDataToPreviewPlayPanel()
        {
            var activityInfo = (PostActivityInfo)this.activityInfo;

            play_title.text = activityInfo.Title;
            play_desc.text = activityInfo.Description;
            
            FontSizeControl(play_title.gameObject, play_title, play_title.text);
            FontSizeControl(play_desc.gameObject, play_desc, play_desc.text);

            if (SBContextManager.Instance.context.IsConsuming() && activityInfo.Status == ActivityStatus.Verified)
            {
                //show complete frame                
                completeFrame.SetActive(true);
            }

            if (SBContextManager.Instance.context.IsConsuming()) {
                descriptionCover.GetComponent<Image>().sprite = twolRoundedSprite;
            }
        }

        protected override void AssignThumbnail(string thumbnail, ErrorInfo error)
        {
            print($"Anchor thumbnail > setting post thumbnail = {thumbnail}");
            if (error != null)
            {
                BottomPanelManager.Instance.ShowMessagePanel("The thumbnail for the anchor cannot be saved at this time.", false, true, () =>
                {
                    // Reset controls.
                    isSaving = false;
                    SetUIMode(UIMode.Edit);
                    playPanel.SetActive(true);
                    ActiveAnchorManager.SetInteractable(true); //re-enable interaction for ALL anchors (including their body and acticities)
                    GPSAnchorManager.Instance.SetInteractable(true); //re-enble interaction for all GPS anchors
                });
                                
                return;
            }

            // There's no error and the thumbnail was uploaded successfully.
            // Let's save the Post now.
            if (uiValues is not PostActivityInfo info) return;
            info.RelocalizationThumbnail = thumbnail;
            saveExeption = true;
            OnSave();
        }

        public void UpdateThumbnail(string thumbnail)
        {
            if (uiValues is not PostActivityInfo info) return;
            info.RelocalizationThumbnail = thumbnail;
        }

        public void OnMarkAsCheckIn(bool isCheckIn)
        {
            print($"OnMarkAsCheckIn = {isCheckIn}");

            string message = $"{(isCheckIn ? "Marking" : "Unmarking")} your post as a point of interest...";
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction(message, false));
            BottomPanelManager.Instance.ShowMessagePanel(message);
            
            SBRestClient.Instance.MarkPostAsCheckIn(activityInfo.Id, isCheckIn);
            
            base.OnSave();
        }

        //------------------------- handle edit interaction --------------------------

        // public void StartEditTitle()
        // {
        //     InputField input = this.edit_title.GetComponentInChildren<InputField>();
        //     input.interactable = true;
        //     input.ActivateInputField();
        // }
        
        public void OnEditTitle(bool showLabel = true)
        {
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                print("Start editing post title");
                if (showLabel)
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOn, altText: "Enter a title for your post");
                
                OnEditTextBox(edit_title, edit_titleCover, (input) =>
                {
                    print("End editing post title");
                    uiValues.Title = input.text;
                    if (input.wasCanceled)
                        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOff);
                    else
                        OnEditDesc();
                });    
            });
        }


        public void OnEditDesc()
        {
            BottomPanelManager.Instance.HideCurrentPanel(() =>
            {
                var uiValues = (PostActivityInfo)this.uiValues;
                print("Start editing post desc");
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOn, altText: "Enter a description for your post");
                OnEditTextBox(edit_desc, edit_descCover, (input) =>
                {
                    print("End editing post desc");
                    uiValues.Description = input.text;
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.InputLabelOff);
                });    
            });
        }

        //------------------------- bottom button interaction --------------------------
        private void ResetValidationStatus()
        { 
            edit_title.transform.Find("ErrorSign").gameObject.SetActive(false);
        }
       
        public override void OnSave()
        {
            if (!saveEditButton.interactable && !saveExeption) {
                return;
            }

            saveExeption = false;
            //validation
            ResetValidationStatus();

            if (uiValues.Title.IsNullOrWhiteSpace())
            {
                edit_title.transform.Find("ErrorSign").gameObject.SetActive(true);
                BottomPanelManager.Instance.ShowMessagePanel("Please provide a title for your post.");
                return;
            }
            
            BottomPanelManager.Instance.ShowMessagePanel("Saving your post...");
            anchorController.quickPhotoWithPostWasCompleted = true;

            print($"#anchorposition Saving post info: {uiValues}");
           
            //save
            base.OnSave();//this has to be called before the action for submitting data
            SubmitPost();
        }

        public override void OnEdit()
        {
            print($"OnEdit: isEverSaved={isEverSaved}, activityInfo.Id={activityInfo.Id}");
            uiValues = ((PostActivityInfo)activityInfo).Clone();
            print($"OnEdit: id={uiValues.Id}");            
            base.OnEdit();
            HideDescriptionSpace(false);
            OnEditTitle();
        }


        public override void OnCancelEdit()
        {
            // There's no need to restore from any instance
            // as we are only updating the "activityInfo"
            // when we are saving the data.
            ApplyDataToUI();

            base.OnCancelEdit();
        }

        #region API callbacks

        /// <summary>
        /// Post-specific processing when the API call succeeded.
        /// </summary>
        public override void OnSuccessfulSave()
        {
            print($"PostActivity.OnSuccessfulSave");
            //apply data to play/preview mode
            ApplyDataToPreviewPlayPanel();

            if (activityInfo is PostActivityInfo post) // && post.ChildrenIds != null)
            {
                // At this point we determine that there were activities
                // that was created and saved before the post was created.
                print("Getting activity option that was created, but not saved, before the post.");
                var children = GetComponentInParent<ActivityManager>()?.GetChildren();
                if (children != null)
                {
                    foreach (var child in children)
                    {                        
                        child.UpdateParentId(activityInfo.Id);
                    }
                }
                else
                {
                    print("no children found");
                }
            }

            if (!anchorController.wasPostSaved)
            {                 
                anchorController.wasPostSaved = true;
            }

            base.OnSuccessfulSave();
            HideDescriptionSpace(String.IsNullOrWhiteSpace(play_desc.text));
        }

        /// <summary>
        /// Post-specific processing when the API call fails.
        /// </summary>
        public override void OnFailedSave(ErrorInfo error)
        {            
            print($"PostActivity.OnFailedSave");            
            base.OnFailedSave(error);
        }

        /// <summary>
        /// The callback function when the API call for consuming an activity succeeded.
        /// </summary>
        /// <remarks>
        /// A consumed activity can return as incorrectly answered (trivia) or a photo that failed keywords validation.
        /// This is still the callback that will be called as those are treated as successful API calls.
        /// </remarks>
        public override void OnSuccessfulConsume()
        {
            print($"OnSuccessfulConsume id={activityInfo.Id} | title={activityInfo.Title} | isCompleted={isCompleted} | status={activityInfo.Status} | isEngaged={anchorController.isEngaged}");
            if (anchorController == null || isCompleted)
                return;

            var hasShownAllActivitiesCompletedNotification = false;
            if (anchorController.isEngaged)
            {
                float angle = Vector3.Angle(transform.forward * -1, Camera.main.transform.forward); //Todo: replace it!
                print($"angle={angle}");
                if (angle <= 30)
                {
                    OnComplete();
                    hasShownAllActivitiesCompletedNotification = true;
                }
                else
                {
                    // Otherwise just show the complete frame.
                    ShowCompleteFrame();
                }
            }
            else if (activityInfo.Status == ActivityStatus.Verified)
            {
                // The anchor is not engaged but it's closed by
                // and the post was consumed.
                // Otherwise just show the complete frame.
                ShowCompleteFrame();
            }

            if (!hasShownAllActivitiesCompletedNotification)
            {
                if (GetComponentInParent<ActivityManager>().ActivitiesCompleted)
                {
                    print("All ActivitiesCompleted");
                    MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);
                }
                else
                {
                    print("NOT All ActivitiesCompleted");
                    OffScreenIndicatorManager.Instance.ShowArrow();
                }
            }            

            if(!SBContextManager.Instance.context.IsConsuming()) {
                print("Post activity, OnSuccessfulConsume(), !SBContextManager.Instance.context.IsConsuming()");
                SetUIMode(UIMode.Preview);
            }
            else {
                SetUIMode(UIMode.Play);
            }
            base.OnSuccessfulConsume();
        }

        #endregion


        public override void OnComplete()
        {
            print($"PostActivity > OnComplete: id={activityInfo.Id}");
            isCompleted = true;

            //completion rotation effect
            panelParent.transform.DOLocalRotate(new Vector3(0, 360, 0), 0.75f, RotateMode.FastBeyond360).OnComplete(() =>
            {
                //completion explode effect
                // GetComponentInParent<ActivityManager>().PlayCheckinEffect(() =>
                // {
                //     ShowCompleteFrame();
                // });                
                ShowCompleteFrame();
                base.OnComplete();
            });
        }

        void ShowCompleteFrame()
        {
            //show complete frame
            completeFrame.SetActive(true);

            if (GetComponentInParent<ActivityManager>().ActivitiesCompleted)
            {
                print("All ActivitiesCompleted");
                MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);
            }
            else
            {
                print("NOT All ActivitiesCompleted");
                OffScreenIndicatorManager.Instance.ShowArrow();
            }
        }
        
        /// <summary>
        /// Sending Post data to SB cloud
        /// </summary>
        bool SubmitPost()
        {
            var postInfo = (PostActivityInfo)uiValues;

            print($"PostActivity.SubmitPost: submitting a Post = {postInfo.ToString()} #plannedLocation");
            
            var anchorLocation = GetComponentInParent<AnchorController>().GetSBLocationInfo();
            var input = PostActivityInput.CreateFrom(postInfo,
                SBContextManager.Instance.context.experienceId,
                SBContextManager.Instance.context.collectionId,
                anchorLocation,
                SBContextManager.Instance.context.isPlanning);
            input.ARAnchorId = GetComponentInParent<AnchorController>().GetAnchorInfo().id;

            if (input.MapLocation == null && SBContextManager.Instance.context.isPlanning)
            {
                input.MapLocation = SBContextManager.Instance.context.plannedLocation;
            }

            MessageManager.Instance.DebugMessage($"Creating PostActivity at Latitude={input.Location.Latitude}, Longitude={input.Location.Longitude}");
            print($"PostActivity.SubmitPost: activityInfo = {postInfo.ToString()} | thumbail={VpsCoverageManager.Instance.SelectedWayspot?.ImageURL} #plannedLocation");

            if (postInfo.IsEditing)
            {
                SBRestClient.Instance.UpdatePost(postInfo.Id, input);
            }                
            else
            {                
                // At this point we determine that there were activities
                // that was created and saved before the post was created.
                var ids = new List<string>();
                IEnumerable<ISBActivity> children = GetComponentInParent<ActivityManager>().GetChildren();
                input.ChildrenIds = children.Select(x => x.GetActivityId());
                if (input.RelocalizationThumbnail.IsNullOrWhiteSpace() && VpsCoverageManager.Instance.SelectedWayspot != null)
                {
                    // The relocalization thumbnail is only set once and is not editable once the post has been created.
                    // At this point, we are sure that the Post is being saved for the first time.
                    input.RelocalizationThumbnail = VpsCoverageManager.Instance.SelectedWayspot?.ImageURL;
                }
                SBRestClient.Instance.CreatePost(postInfo.Id, input);
            }                

            print("Exiting SubmitPost...");
            return true;
        }

        void ConsumePost()
        {
            if (isSaving || SBRestClient.Instance.isBusy)
                return;

            print($"ConsumePost: Start consume for id={activityInfo.Id} | isBusy={SBRestClient.Instance.isBusy} | isSaving={isSaving}");
            if (activityInfo.Status != ActivityStatus.New)
            {
                print("You have already completed this activity.");
                // There is no need to tell the user about this, just ignore the action.
                return;
            }

            //
            // ToDo: show animation here while the API call is running.
            //      The animation will be hidden in the API callbacks "OnSuccessfulSave" and "OnFailedSave".            

            isSaving = true;
            print($"ConsumePost: completing the post activity...");
            SBRestClient.Instance.ConsumePost(activityInfo.Id, new PhotoPoiConsumeInput
            {
                ExperienceId = SBContextManager.Instance.context.experienceId,                
            });
            
            if(!SBContextManager.Instance.context.isOffline) {
                SetUIMode(UIMode.Saving);
            }
        }
        
        public void StartTrackCharacters(InputField input)
        {
            FontSizeControl(input.gameObject, input.textComponent, input.text); //update the font size according to the number of characters
            UIManager.Instance.StartTrack(input); //giving alert for how much character is left
            EnableSaveButton(true);
        }


        private void FontSizeControl(GameObject go, Text text, string textValue)
        {
            if (go.name == "Title")
            {
                if (textValue.Length > 0 && textValue.Length <= 24)
                {
                    text.fontSize = 27;
                }
                else if (textValue.Length > 24 && textValue.Length <= 48)
                {
                    text.fontSize = 23;
                }
            }
            else if(go.name == "Desc")
            {
                if (text.text.Length > 0 && text.text.Length <= 96)
                {
                    text.fontSize = 25;
                }
                else if (text.text.Length > 96 && text.text.Length <= 120)
                {
                    text.fontSize = 22;
                }
                else if (text.text.Length > 120 && text.text.Length <= 144)
                {
                    text.fontSize = 20;
                }
            }
        }
        

        public void FinishTrackCharacters() {
            UIManager.Instance.FinishTrack();
        }
        
        private bool descriptionHide;
        void HideDescriptionSpace(bool hide) {
            if(descriptionHide == hide)
                return;
        
            descriptionCover.gameObject.SetActive(!hide);
            float newHeight = descriptionCover.rect.height / 2;
            RectTransform rTrOutline = completeFrame.GetComponentInChildren<Image>().GetComponent<RectTransform>();
            print($"PhotoVideoActivity HideCaptionSpace, hide = {hide}, newHeight = {newHeight}");
        
            if (hide) {
                buttonsRoot.localPosition = new Vector3(0, buttonsRoot.localPosition.y + newHeight, 0);
                playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x,playPanel.transform.localPosition.y - newHeight, playPanel.transform.localPosition.z);
                rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height - newHeight * 2);
                if(SBContextManager.Instance.IsConsuming()) {
                    titleCover.sprite = allRoundedSprite;
                }
            }
            else {
                buttonsRoot.localPosition = new Vector3(0, buttonsRoot.localPosition.y - newHeight, 0);
                playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x, playPanel.transform.localPosition.y + newHeight, playPanel.transform.localPosition.z);
                rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height + newHeight * 2);
                titleCover.sprite = twolRoundedSprite;
            }
            descriptionHide = hide;
        }
    }
}
