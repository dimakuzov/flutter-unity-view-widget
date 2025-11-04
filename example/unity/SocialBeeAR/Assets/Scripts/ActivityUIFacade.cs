using System;
using System.Collections;
using GifPlayer;
using SocialBeeAR;
using SocialBeeARDK;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    
    public class ActivityUIFacade : MonoBehaviour
    {
        public enum UIEvent
        {
            Undefined,
            AskIndoorOrOutdoor,
            ShowNearbyVpsMapPanel,
            ShowVpsRequestPanel,
            EnableVps,
            MovingReticle,
            ReticleTapped,
            AnchorLockTappedGPSOnly,
            AnchorLockTapped,
            CancelStartScan,
            StartScan,
            AbortScan,
            ScanCompleted,
            DeletingAnchor,
            DeletingActivity,
            BackToNative,
            UserTooFarAway,
            NextAnchor,
            NextAnchorOrSaveMap,
            NextOrCompleteGPSOnlyCreation,
            CompletePlanning,
            SavingMap,
            NavigateBeforeLocalization,
            PreLocalizationAfterNavigation,
            PreLocalization,
            Localizing,
            Localized,
            RetryLocalizing,
            StartBlobUpload,
            ConfirmToLoadNewMap,
            WrongChallengeKeywords,
            AllActivitiesComplete,
            ShareMemories,
            FinishEdit,
            AskDoConnectToInternet,
            TestARSession,
            InputLabelOn,
            InputLabelOff
        }
        private UIEvent currentUiEvent = UIEvent.Undefined;
        
        //pre-create
        [SerializeField] private GameObject askIndoorOrOutdoorBottomPanel;
        [SerializeField] private GameObject nearbyVpsPanel; // UIEvent.ShowNearbyVpsMap
        // public GameObject mapBoxEssentials;
        [SerializeField] private GameObject askDoConnectToInternet;
        
        //pre-scan elements
        [SerializeField] private Animator tapToPlaceAnimation;
        [SerializeField] private GameObject confirmScanBottomPanel;
        [SerializeField] private GameObject confirmLockBottomPanel;
    
        //scanning elements
        [SerializeField] private Animator moveDeviceAnimation;
        [SerializeField] private GameObject progressInfo;
        [SerializeField] private GameObject scanningBottomPanel;
        
        //post-scan elements
        [SerializeField] private GameObject postScanBottomPanel;

        //next anchor or save map
        //[SerializeField] private GameObject nextOrSaveBottomPanel;
        public GameObject nextOrSaveButtons;
        public GameObject nextOrCompleteGPSOnlyCreationButtons;
        public GameObject planningCompleteButton;
        public GameObject nextButton;
        
        //saving map 
        // [SerializeField] private GameObject savingMapBottomPanel;
        [SerializeField] private GameObject savedMapBottomPanel;

        //localization
        [SerializeField] private GameObject preLocalizationButtonPanel;
        [SerializeField] private Button localizationButton;
        [SerializeField] private GameObject localizingBottomPanel;
        [SerializeField] private GameObject localized4ConsumerBottomPanel;
        [SerializeField] private GameObject localized4CreatorBottomPanel;
        [SerializeField] private GameObject retryLocalizationBottomPanel;
        
        //alerts
        [SerializeField] private GameObject deleteAnchorAlertBottomPanel;
        [SerializeField] private GameObject deleteActivityAlertBottomPanel;
        [SerializeField] private GameObject backToNativeAlertBottomPanel;
        [SerializeField] private GameObject tooFarAlertBottomPanel;
        [SerializeField] private GameObject loadNewMapConfirmBottomPanel;
        [SerializeField] private GameObject wrongChallengeKeywordsPanel;
        [SerializeField] private GameObject allActivitiesComplete;
        [SerializeField] private GameObject activitiesCompleteNextTo;
        [SerializeField] private GameObject shareMemoriesPanel;
        [SerializeField] private GameObject finishButton;
        [SerializeField] private GameObject editingLabelPanel;

        //elements for progress
        [SerializeField] Text mappingProgressNum;
        [SerializeField] Image mappingProgressFillImage;

        private RectTransform thumbnailParentRectTransform;
        private RectTransform thumbnailRootRectTransform;
        public GameObject thumbnailParent;
        public RawImage thumbnailImage;
        public GameObject thumbnailCancelButton;
        public GameObject thumbnailProgress;
        [SerializeField] Toggle thumbnailZoomToggle;
        public bool canShowThumbnail = false;
        
        [SerializeField] private Button backButton;
        
        //indicator of user select indoor or outdoor
        [HideInInspector] public bool isSelectedTypeOfExperience;
        
        [SerializeField]
        [Tooltip("Panel for testing initizalizing, running, pausing, and destroying the ARDK AR Session.")]
        private GameObject testARSessionBottomPanel;
        
        [HideInInspector]
        private static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;

        private float thumbnailDefaultWidth;
        // private static float ThumbnailDefaultWidth = 278f;
        // private static float ThumbnailDefaultHeight = 398f;

        public static ActivityUIFacade Instance;
        
        public virtual void Awake()
        {
            Instance = this;
        }
        
        private void Start()
        {
            // print("ActivityUIFacade started");
            SetNextOrSaveButtonsVisible(false, false, false);
            nearbyVpsPanel.SetActive(false);
            thumbnailParent.SetActive(false);
            // Handles resizing the parent's content
            thumbnailRootRectTransform = thumbnailParent.transform.Find("root").GetComponent<RectTransform>();
            thumbnailParentRectTransform = thumbnailParent.GetComponent<RectTransform>();
            
            var thumbnailImageRectTransform = thumbnailImage.GetComponent<RectTransform>();
            thumbnailDefaultWidth = thumbnailImageRectTransform.rect.width;
            print($"#debugzoom thumbnailDefaultWidth={thumbnailDefaultWidth}");
            thumbnailZoomToggle.onValueChanged.AddListener(delegate
            {
                ZoomToggleValueChanged(thumbnailZoomToggle);
            });
        }
 
        private void ShowNearbyVpsMapPanel(Action postAction = null)
        {
            // print($"ShowNearbyVpsMapPanel > nearbyVpsPanel is null {nearbyVpsPanel==null}");
            // Do the necessary initialization of configuration here for the gameobject.
            UIManager.Instance.DeactivateScreens();
            UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);

            // just for testing Purpose..
            // mapBoxEssentials.SetActive(true);

            //if (GameObject.FindObjectOfType<PointsBarManager>().gameObject != null)
            //    GameObject.FindObjectOfType<PointsBarManager>().gameObject.SetActive(false);


        }
        
        private void ShowVpsRequestPanel(Action postAction = null)
        {
            BottomPanelManager.Instance.ShowPanel(nearbyVpsPanel, false, postAction);
        }
        
        public void FireUIEvent(UIEvent uiEvent, Action postAction = null, string altText = "")
        {
            print($"FireUIEvent > {uiEvent}");
            // print($"FireUIEvent > activitiesCompleteNextTo=null? {activitiesCompleteNextTo==null} | allActivitiesComplete=null? {allActivitiesComplete==null} | nearbyVpsPanel={nearbyVpsPanel==null}");
            // print($"FireUIEvent > SB context null? {SBContextManager.Instance.context==null} | deleteAnchorAlertBottomPanel=null? {deleteAnchorAlertBottomPanel==null}");
           
            if(uiEvent != UIEvent.BackToNative) {
                activitiesCompleteNextTo.SetActive(false);
                allActivitiesComplete.SetActive(false);
            }
            
            switch (uiEvent)
            {
                case UIEvent.ShowNearbyVpsMapPanel:
                    Debug.Log("FireUIEvent ShowNearbyVpsMapPanel");
                    ShowNearbyVpsMapPanel(postAction);
                    break;
                    
                case UIEvent.ShowVpsRequestPanel:
                    ShowVpsRequestPanel(postAction);
                    break;
                
                case UIEvent.EnableVps:
                    // print("#debugvps UIEvent.EnableVps");
                    askIndoorOrOutdoorBottomPanel.GetComponent<BottomPanel>().ShowButton(true);
                    var button1 = askIndoorOrOutdoorBottomPanel.gameObject.transform.Find("DetectingVpsButton");
                    if (button1 != null) 
                        button1.gameObject.SetActive(false);
                   
                    break;
                
                case UIEvent.AskIndoorOrOutdoor:
                    // print("FireUIEvent 3 #debugvps");
                    BottomPanelManager.Instance.ShowPanel(askIndoorOrOutdoorBottomPanel, false, postAction);
                    //askIndoorOrOutdoorBottomPanel.GetComponent<BottomPanel>().EnableButton(!SBContextManager.Instance.context.isOffline);
                    askIndoorOrOutdoorBottomPanel.GetComponent<BottomPanel>().ShowButton(false);
                    var button = askIndoorOrOutdoorBottomPanel.gameObject.transform.Find("DetectingVpsButton");
                    if (button != null) 
                        button.gameObject.SetActive(true);
                 
                    break;
                
                case UIEvent.MovingReticle:
                    // print("FireUIEvent 4");
                    thumbnailParent.SetActive(false);
                    SetNextOrSaveButtonsVisible(false, false, false);
                    EnableTapToPlaceAnimation(true);
                    break;
                
                case UIEvent.ReticleTapped:
                    // print("FireUIEvent 5");
                    EnableTapToPlaceAnimation(false);
                    break;
                
                case UIEvent.AnchorLockTappedGPSOnly:
                    // print("FireUIEvent 6");
                    LockAnchor(confirmLockBottomPanel, postAction);
                    break;

                case UIEvent.AnchorLockTapped:
                    // print("FireUIEvent 7");
                    //PN implementation --> LockAnchor(confirmScanBottomPanel, postAction);
                    LockAnchor(confirmLockBottomPanel, postAction);
                    break;
                
                case UIEvent.CancelStartScan:
                    // print("FireUIEvent 8");
                    if(ActiveAnchorManager.GetCurrentAnchorObject() != null) {
                        ActiveAnchorManager.GetCurrentAnchorObject().GetComponent<AnchorController>()
                            .LockAnchorWithAnimation(false);
                    }
                    break;
                
                case UIEvent.StartScan:
                    // print("FireUIEvent 9");
                    //debugThumbnailListParent.SetActive(true);
                    EnableMoveDeviceAnimation(true);
                    progressInfo.SetActive(true);
                    BottomPanelManager.Instance.ShowPanel(scanningBottomPanel, false, postAction);
                    scanningBottomPanel.GetComponent<BottomPanel>().ShowButton(true);
                    // if(AnchorManager.Instance.Anchors > 1) {
                    //     scanningBottomPanel.GetComponent<BottomPanel>().ShowButton(true);
                    // }
                    // else {
                    //     scanningBottomPanel.GetComponent<BottomPanel>().ShowButton(false);
                    // }
                    
                    break;
                
                case UIEvent.AbortScan:
                    // print("FireUIEvent 10");
                    EnableMoveDeviceAnimation(false);
                    progressInfo.SetActive(false);
                    //tooBottomPanelManager.Instance.HideCurrentPanel();
                    break;
                
                case UIEvent.ScanCompleted:
                    // print("FireUIEvent 11");
                    EnableMoveDeviceAnimation(false);
                    progressInfo.SetActive(false);
                    //BottomPanelManager.Instance.ShowPanel(postScanBottomPanel, false, postAction);
                    BottomPanelManager.Instance.ShowMessagePanel("Scanning completed! You may start editing the post panel.", false);
                    wasActivitiesCompleted = false;
                    break;

                case UIEvent.StartBlobUpload:                        
                    progressInfo.SetActive(true);
                    BottomPanelManager.Instance.ShowPanel(scanningBottomPanel, false, postAction);
                    scanningBottomPanel.GetComponent<BottomPanel>().ShowButton(false);
                    
                    BottomPanelManager.Instance.UpdateMessage("Uploading your content...");
                    break;

                case UIEvent.DeletingAnchor:
                    if (SBContextManager.Instance.context.isPlanning) {
                        finishButton.SetActive(true);
                    }
                    else {
                        SetNextOrSaveButtonsVisible(false, false, false);
                    }
                    BottomPanelManager.Instance.ShowPanel(deleteAnchorAlertBottomPanel, true, postAction);
                    break;
                
                case UIEvent.DeletingActivity:
                    BottomPanelManager.Instance.ShowPanel(deleteActivityAlertBottomPanel, true, postAction);
                    break;
                
                case UIEvent.BackToNative:
                    BottomPanelManager.Instance.ShowPanel(backToNativeAlertBottomPanel, true, postAction);
                    break;
                
                case UIEvent.UserTooFarAway:
                    BottomPanelManager.Instance.ShowPanel(tooFarAlertBottomPanel, false, postAction);
                    break;
                    
                case UIEvent.NextAnchorOrSaveMap:
                    BottomPanelManager.Instance.HideCurrentPanel(postAction);
                    if (SBContextManager.Instance.context.isPlanning) {
                        finishButton.SetActive(true);
                    }
                    else {
                        SetNextOrSaveButtonsVisible(true, false, false);
                    }
                    break;
                
                case UIEvent.NextOrCompleteGPSOnlyCreation:
                    BottomPanelManager.Instance.HideCurrentPanel(postAction);
                    if (SBContextManager.Instance.context.isPlanning) {
                        finishButton.SetActive(true);
                    }
                    else {
                        SetNextOrSaveButtonsVisible(false, false, true);
                    }
                    break;
                
                case UIEvent.CompletePlanning:
                    BottomPanelManager.Instance.HideCurrentPanel(postAction);
                    SetNextOrSaveButtonsVisible(false, false, false, true);
                    break;
                
                case UIEvent.NextAnchor:
                    BottomPanelManager.Instance.HideCurrentPanel(postAction);
                    SetNextOrSaveButtonsVisible(false, true, false);
                    break;
                
                case UIEvent.SavingMap:
                    BottomPanelManager.Instance.ShowMessagePanel("Saving map...", true, false, postAction);
                    break;

                case UIEvent.NavigateBeforeLocalization:
                    PrepareLocalizationObjects();

                    break;
                
                // This should be called only after UIEvent.NavigateBeforeLocalization.
                case UIEvent.PreLocalizationAfterNavigation:
                    BottomPanelManager.Instance.ShowPanel(preLocalizationButtonPanel, false, postAction);
                    BottomPanelManager.Instance.UpdateMessage("Go and point your phone at the area shown in the thumbnail.");
                    
                    break;
                
                case UIEvent.PreLocalization:
                    //thumbnailParent.SetActive(false);
                    //BottomPanelManager.Instance.ShowMessagePanel("Loading map...", true, false, postAction);
                    
                    //prepare the thumbnail loading UI
                    PrepareLocalizationObjects();
                    
                    //show bottom panel
                    BottomPanelManager.Instance.ShowPanel(preLocalizationButtonPanel, false, postAction);
                    
                    break;
                
                case UIEvent.RetryLocalizing:
                    BottomPanelManager.Instance.ShowPanel(retryLocalizationBottomPanel, false, postAction);
                    break;
                
                case UIEvent.Localizing:
                    // print("FireUIEvent 12");
                    if (!altText.IsNullOrWhiteSpace())
                    {
                        localizingBottomPanel.GetComponentInChildren<Text>().text = altText;
                    }
                    BottomPanelManager.Instance.ShowPanel(localizingBottomPanel, false, postAction);
                    break;

                case UIEvent.Localized:
                    // print("FireUIEvent 14");
                    canShowThumbnail = false;
                    StartThumbnailProgressAnimation(false, false);
                    thumbnailZoomToggle.gameObject.SetActive(false);
                    thumbnailImage.gameObject.SetActive(false);
                    thumbnailParent.SetActive(false);
                    localizationButton.interactable = false;
                    wasActivitiesCompleted = false;
                    MapBoxManager.Instance.ToggleNavigationMap(false);
                    
                    MessageManager.Instance.ShowMessage($"FireUIEvent > Localized: mode={SBContextManager.Instance.context.Mode.ToString()} ");
                    if(SBContextManager.Instance.IsCreating()) {
                        if (SBContextManager.Instance.IsEditCreating())
                        {
                            BottomPanelManager.Instance.ShowPanel(localized4CreatorBottomPanel, false, postAction);    
                        }
                        else
                        {
                            BottomPanelManager.Instance.ShowMessagePanel($"You are now localized. Tap on the 'X' icon to create an activity.", autoClose: true);
                            if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors && !SBContextManager.Instance.context.isPlanning) {
                                SetNextOrSaveButtonsVisible(false, false, true);
                            }
                            else {
                                finishButton.SetActive(true);
                            }
                        }
                    }
                    else {
                        BottomPanelManager.Instance.ShowPanel(localized4ConsumerBottomPanel, false, postAction);
                    }
                    break;
                
                case UIEvent.ConfirmToLoadNewMap:
                    BottomPanelManager.Instance.ShowPanel(loadNewMapConfirmBottomPanel, true, postAction);
                    break;

                case UIEvent.Undefined:
                    break;
                
                case UIEvent.WrongChallengeKeywords:
                    BottomPanelManager.Instance.ShowPanel(wrongChallengeKeywordsPanel, true, postAction);
                    break;
                
                case UIEvent.AllActivitiesComplete:
                    wasActivitiesCompleted = true;
                    if (ActiveAnchorManager.IsAllAnchorsComplete()) {
                        activitiesCompleteNextTo.SetActive(false);
                        allActivitiesComplete.SetActive(true);
                        OffScreenIndicatorManager.Instance.HideArrow();
                        MiniMapManager.Instance.RemoveRedPoint();
                    }
                    else
                    {
                        var anchorController = ActiveAnchorManager.GetNeededConsumeAnchorController();
                        if (anchorController != null)
                        {
                            print($"ActiveAnchorManager.GetNeededConsumeAnchorController() = {anchorController.GetAnchorInfo().postInfo.Title}");
                            OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform);
                            OffScreenIndicatorManager.Instance.ShowArrow();
                            MiniMapManager.Instance.SetRedPoint(anchorController.GetAnchorInfo().id);
                            activitiesCompleteNextTo.SetActive(true);    
                        }
                        else
                        {
                            print("UIEvent.AllActivitiesComplete > AnchorController is null!");
                        }
                    }
                    break;
                
                case UIEvent.ShareMemories:
                    BottomPanelManager.Instance.ShowPanel(shareMemoriesPanel, true, postAction);
                    break;
                
                case UIEvent.FinishEdit:
                    finishButton.SetActive(true);
                    break;
                
                case UIEvent.AskDoConnectToInternet:
                    BottomPanelManager.Instance.ShowPanel(askDoConnectToInternet, false, postAction);
                    break;
                
                case UIEvent.TestARSession:
                    BottomPanelManager.Instance.ShowPanel(testARSessionBottomPanel, false, postAction);
                    break;
                
                case UIEvent.InputLabelOn:
                    ToggleEditingLabel(altText, true);
                    break;
                    
                case UIEvent.InputLabelOff:
                    ToggleEditingLabel("", false);
                    break;
            }

        }

        void PrepareLocalizationObjects()
        {
            canShowThumbnail = true;
            thumbnailParent.SetActive(true);
            StartThumbnailProgressAnimation(true);
            thumbnailZoomToggle.gameObject.SetActive(false);
            thumbnailImage.gameObject.SetActive(false);
            localizationButton.interactable = false;
            PointsBarManager.Instance.HidePointsBar();
        }
        
        void LockAnchor(GameObject panel,  Action postAction = null)
        {
            thumbnailParent.SetActive(false);
            SetNextOrSaveButtonsVisible(false, false, false);
            BottomPanelManager.Instance.ShowPanel(panel, true, postAction);
        }
        
        void ToggleEditingLabel(string label, bool show)
        {
            var textComp = editingLabelPanel.GetComponentInChildren<Text>();
            print($"editing label is null? {textComp==null} | show={show}");
            if (textComp != null) textComp.text = label;
            editingLabelPanel.SetActive(show);
        }

        public void ShowAnchorMoveAndLockGuidance()
        {
            if (SBContextManager.Instance.context.isPlanning)
            {
                BottomPanelManager.Instance.ShowMessagePanel(
                    "You may move the object by dragging its bottom, or tap 'Edit' it to continue.",
                    false, false, null);   
            }
            else
            {
                BottomPanelManager.Instance.ShowMessagePanel(
                    "You may move the object by dragging its bottom, then lock it to continue.",
                    false, false, null);    
            }
        }
        

        public void EnableTapToPlaceAnimation(bool enable)
        {
            tapToPlaceAnimation.SetTrigger(enable ? Const.ANIMATION_FADE_ON : Const.ANIMATION_FADE_OFF);
        }
        
        
        public void EnableMoveDeviceAnimation(bool enable)
        {
            moveDeviceAnimation.SetTrigger(enable ? Const.ANIMATION_FADE_ON : Const.ANIMATION_FADE_OFF);
        }
        
        
        public void ResetMappingProgress()
        {
            mappingProgressFillImage.gameObject.GetComponent<Image>().fillAmount = 0;
            mappingProgressNum.text = "";
        }


        public void SetMappingProgress(float percentageUnderOne)
        {
            float percentage = Mathf.Max(percentageUnderOne, 0f);
            mappingProgressFillImage.gameObject.GetComponent<Image>().fillAmount = percentage;
            mappingProgressNum.text = ((int)(percentage * 100)).ToString();
        }


        public void UpdateThumbnail(Texture2D thumbnailTexture)
        {
            if (!canShowThumbnail)
                return;

            //making sure the thumbnail component is active
            thumbnailParent.SetActive(true);
            StartThumbnailProgressAnimation(false);
            thumbnailZoomToggle.gameObject.SetActive(true);
            thumbnailImage.gameObject.SetActive(true);

            //updating thumbnail image
            RectTransform thumbnailImageRect = thumbnailImage.rectTransform;
            float scaleFactor = 1; //2.5f; //prev: 2

            // MessageManager.Instance.DebugMessage(string.Format("original: width={0}, height={1}, after scale: width={2}, height={3}",
            //     thumbnailTexture.width, thumbnailTexture.height, thumbnailTexture.width * scaleFactor, thumbnailTexture.height * scaleFactor));
            print(
                $"original: width={thumbnailTexture.width}, height={thumbnailTexture.height}, after scale: width={thumbnailTexture.width * scaleFactor}, height={thumbnailTexture.height * scaleFactor}");

            // We now want the thumbnail set to a fixed size
            // thumbnailImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, thumbnailTexture.width * scaleFactor);
            // thumbnailImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, thumbnailTexture.height * scaleFactor);
            thumbnailImageRect.ForceUpdateRectTransforms();
            thumbnailImage.texture = thumbnailTexture;
                
            //updating button as well
            // thumbnailCancelButton.transform.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, thumbnailTexture.width * scaleFactor);
            // thumbnailCancelButton.transform.GetComponent<RectTransform>().ForceUpdateRectTransforms();
            // thumbnailCancelButton.transform.localPosition = new Vector3(
            //     thumbnailTexture.width * scaleFactor, 
            //         thumbnailCancelButton.transform.localPosition.y, 
            //             thumbnailCancelButton.transform.localPosition.z);
            
            //enable localization button
            localizationButton.interactable = true;
        }

        void UpdateThumbnailSize(float width, float height)
        {
            RectTransform thumbnailImageRect = thumbnailImage.rectTransform;
            thumbnailImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            thumbnailImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            thumbnailImageRect.ForceUpdateRectTransforms();
        }
 
        private void StartThumbnailProgressAnimation(bool start, bool showMessage = true)
        {
            if (thumbnailProgress == null)
                return;
            
            UnityGif progressGif = thumbnailProgress.GetComponentInChildren<UnityGif>();
            if (start)
            {
                BottomPanelManager.Instance.UpdateMessage("Retrieving thumbnail...");
                thumbnailProgress.SetActive(true);
                if (!progressGif.IsPlaying)
                    progressGif.Play();
            }
            else
            {
                if (showMessage)
                    BottomPanelManager.Instance.UpdateMessage("Go and point your phone at the area shown in the thumbnail.");
                if (progressGif.IsPlaying)
                    progressGif.Pause();
                thumbnailProgress.SetActive(false);
            }
        }

        private bool wasActivitiesCompleted;
        public void BackToNextOrSaveButton() {
            if (wasActivitiesCompleted) {
                FireUIEvent(UIEvent.AllActivitiesComplete);
            }

            if (!isSelectedTypeOfExperience && !SBContextManager.Instance.context.IsConsuming() && !SBContextManager.Instance.context.isPlanning) {
                Invoke("ShowAskIndoorOrOutdoor", 0.5f);
            }
        }

        void ShowAskIndoorOrOutdoor() {
            InteractionManager.Instance.AskIndoorOrOutdoor();
        }
        
        
        public void SetNextOrSaveButtonsVisible(bool nextOrSaveButtonVisible, bool nextButtonVisible, bool nextOrCompleteGPSOnlyCreation, bool planningCompleteVisible = false)
        {
            if (nextOrSaveButtons == null) return;
            
            nextOrSaveButtons.SetActive(nextOrSaveButtonVisible);
            nextButton.SetActive(nextButtonVisible);
            nextOrCompleteGPSOnlyCreationButtons.SetActive(nextOrCompleteGPSOnlyCreation);
            planningCompleteButton.SetActive(planningCompleteVisible);
            finishButton.SetActive(false);
        }

        public void EnableBackButton(bool enable) {
            backButton.interactable = enable;
        }

        public void ShowActivityCompleteUI(bool show) {
            allActivitiesComplete.transform.parent.gameObject.SetActive(show);
        }
     
        
        #region Zooming
        
        public void ZoomToggleValueChanged(Toggle toggle) {
            print($"#debugzoom Toggle = {toggle.isOn}");
            if (!toggle.isOn) {
                ZoomIn();
            }
            else {
                ZoomOut();
            }
        }
        
        void ZoomIn() {
            Debug.Log($"ZoomIn()");
            StartCoroutine(Zoom(true));
        }
        
        void ZoomOut() {
            Debug.Log($"ZoomOut()");
            StartCoroutine(Zoom(false));
        
        }
        
        IEnumerator Zoom(bool zoomIn)
        {
            var zoomInScale = 3f;
            float speed = 0.25f;
            for (float f = 0; f < speed; f += Time.deltaTime) {
                float newScale;
                newScale = zoomIn ? Mathf.Lerp(1, zoomInScale, f / speed) : Mathf.Lerp(zoomInScale, 1, f / speed);
                print($"#debugzoom >>> width={thumbnailDefaultWidth * newScale}");
                thumbnailParentRectTransform.sizeDelta = new Vector2(thumbnailDefaultWidth * newScale, thumbnailDefaultWidth * newScale);
                thumbnailRootRectTransform.localScale = new Vector3(newScale, newScale, newScale);
                yield return null;
            }
            if (zoomIn) {
                thumbnailParentRectTransform.sizeDelta = new Vector2(thumbnailDefaultWidth * zoomInScale, thumbnailDefaultWidth * zoomInScale);
                thumbnailRootRectTransform.localScale = new Vector3(zoomInScale, zoomInScale, zoomInScale);
            }
            else {
                thumbnailParentRectTransform.sizeDelta = new Vector2(thumbnailDefaultWidth, thumbnailDefaultWidth);
                thumbnailRootRectTransform.localScale = Vector3.one;
            }
            yield return null;
        }
    
        #endregion

    }
 
}