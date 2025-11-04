using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GifPlayer;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;


public class PhotoVideoActivity : SBActivity
{

    [SerializeField] private GameObject photoPlayPanel;
    [SerializeField] private GameObject videoPlayPanel;

    // [SerializeField] private GameObject photoBoard;
    // [SerializeField] private GameObject videoBoard;
    public GameObject contentBoard;

    [Header("Title UI")]
    [SerializeField] private GameObject keywordsPanel;
    [SerializeField] private GameObject captionInputObject;
    [SerializeField] private Text titleText;
    [SerializeField] private Text keywordsText;
    [SerializeField] private Text keywordsOnEditText;
    [SerializeField] private Text keywordsTextOnKeywordsPanel;
    [SerializeField] private GameObject keywordField;
    [SerializeField] private GameObject keywordPrefab;
    [SerializeField] private Toggle challengeToggle;
    [SerializeField] private GameObject challengeMarker;
    [SerializeField] private Button nextChallengeButton;
    [SerializeField] private Button[] cancelButtons;
    [SerializeField] private GameObject keywordsButtonPanel;
    [SerializeField] private GameObject editVideoButton;
    [SerializeField] private GameObject keywordCover;
    [SerializeField] private GameObject captionCover;
    [SerializeField] private Transform buttonsRoot;

    [Header("Video Player")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private Button fullScreenButton;
    [SerializeField] private Slider allClipLoader;
    private IEnumerator isAllClipLoaderTimer;
    private bool isAllClipLoaderTimerWork;
    [Header("Preview")]
    [SerializeField] private GameObject previewForARPhotoEffect;
    // [SerializeField] private GameObject previewContentForARPhotoEffect;
    public Slider waitLoader;
    [SerializeField] private GameObject challengeLoader;
    [SerializeField] private GameObject videoLoader;

    //[Header("Debug")]
    //[SerializeField] private string[] testingTags;// must be replace to tags from native app

    List<string> imageTags = new List<string>();// --- the tags that were pulled from the photo
    List<string> selectedTags = new List<string>();// --- selected tags
    private string captionInput;
    bool isGettinKeywords = false;
    private bool isVideoPanel;
    string contentBeforeEditLocalPath;
    string captionBeforeEdit;
    private bool isVideoBeforeEdit;
    [HideInInspector] public bool isChallengeBeforeEdit;
    // Texture previewForARPlane;
    bool pathFromURL = false;
    Texture2D thumbnailTex;
    private bool isContentChanged;
    
    [HideInInspector] public List<string> videoPaths;
    private int filterIndex;
    private int currClip;
    private VideoPlayer vp;
    private Material photoMat;

    bool isApplyData;

    private void OnEnable() {
        vp = contentBoard.GetComponent<VideoPlayer>();
        vp.loopPointReached += ShowPlayButton;
        vp.loopPointReached += ChangeClip;
        photoMat = contentBoard.GetComponent<Renderer>().material;
    }

    void OnDisable() {
        vp = contentBoard.GetComponent<VideoPlayer>();
        vp.loopPointReached -= ShowPlayButton;
        vp.loopPointReached -= ChangeClip;
    }

    
    public override void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "")
    {
        currClip = -1;
        if (parentId.IsNullOrWhiteSpace())
            throw new ArgumentNullException("A photo activity cannot be created without a parent. ParentId is required.");

        base.Born(id, type, experienceId, anchorPose, parentId, mapId, anchorPayload);

        print($"[IsQuickPhotoVideo] PhotoVideoActivity.Born > startWithPhotoVideo={RecordManager.Instance.startWithPhotoVideo}");
        var info = (PhotoVideoActivityInfo)activityInfo;
        info.IsQuickPhotoVideo = RecordManager.Instance.startWithPhotoVideo;
        ((PhotoVideoActivityInfo)uiValues).IsQuickPhotoVideo = info.IsQuickPhotoVideo;

        SetMakeChallengeToggle(false);

        if (RecordManager.Instance.startWithPhotoVideo) {
            if (RecordManager.Instance.isPhotoTaking) {
                InteractionManager.Instance.OnPhotoTaken(RecordManager.Instance.filteredFilePath);
                challengeToggle.gameObject.SetActive(!SBContextManager.Instance.context.IsCreatingInConsume());
                EnableSaveButton();
            }
            else {
                thumbnailTex = RecordManager.Instance.filteredThumbnail;
                // contentBoard.GetComponent<MeshRenderer>().material.SetTexture("_Albedo", thumbnailTex);
                // InteractionManager.Instance.ShowVideoThumbnail(thumbnailTex);
                ConfigureSegments(RecordManager.Instance.segmentPaths, RecordManager.Instance.selectedMatInList);
                BeforeOnVideoTaken();
                OnVideoTaken();
                if (RecordManager.Instance.filteredFilePath == null) {
                    DisableSaveButton(true);
                }
                else {
                    EnableSaveButton();
                }
                ShowThumbnail(thumbnailTex);
                ShowPlayButtons();
            }

        }
        else {
            //Commented off by cliff, this action is moved to be after the animation of sliding up photo/video activity panel >>>>>
            //RecordManager.Instance.ClearPaths();
            //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            challengeToggle.gameObject.SetActive(!SBContextManager.Instance.context.IsCreatingInConsume());
        }

        RecordManager.Instance.currPhotoVideoActivity = this;
        RecordManager.Instance.isCreateMode = true;

        anchorController.PhotoVideoExist(true);
        // previewForARPlane = contentBoard.GetComponent<MeshRenderer>().material.GetTexture("_Albedo");
    }



    

    public override void Reborn(IActivityInfo activityInfo)
    {
        print($"PhotoVideoActivity.Reborn - activityInfo = {(activityInfo == null ? "NULL" : activityInfo.ToString())}");
        if (activityInfo.ParentId.IsNullOrWhiteSpace())
            throw new ArgumentNullException("A photo/video activity cannot be reborn without a parent. ParentId is required.");

        activityInfo.IsEditing = true;
        base.Reborn(activityInfo);

        print("-=- Reborn() Clear RecordManager paths");
        if (RecordManager.Instance.startWithPhotoVideo) {
            if (RecordManager.Instance.isPhotoTaking) {
                InteractionManager.Instance.OnPhotoTaken(RecordManager.Instance.filteredFilePath);
            }
            else {
                RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
                RecordManager.Instance.ShowVideoOnPanel();
            }
        }
        // else {
        //     RecordManager.Instance.ClearPaths();
        // }
        //parse photo/video URL, retrieve and display it.
        ApplyDataToUI();
        // if (activityInfo.IsChallenge)
        //     MakeAsChallenge();

        var info = (PhotoVideoActivityInfo)this.activityInfo;
        SetMakeChallengeToggle(false, info.IsVideo);

        RecordManager.Instance.currPhotoVideoActivity = this;
        RecordManager.Instance.isCreateMode = false;

        anchorController.PhotoVideoExist(true);
        isChallengeBeforeEdit = info.IsChallenge;
        // previewForARPlane = contentBoard.GetComponent<MeshRenderer>().material.GetTexture("_Albedo");
    }


    /// <summary>
    /// Use the activityInfo here.
    /// </summary>
    protected override void ApplyDataToUI()
    {
        ApplyDataToEditPanel();

        ApplyDataToPreviewPlayPanel();
    }

    /// <summary>
    /// Set the values of the UI elements from the activityInfo.
    /// </summary>
    private void ApplyDataToEditPanel() {
        isApplyData = true;
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        // MessageManager.Instance.DebugMessage($"ApplyDataToEditPanel: activityInfo = {activityInfo}");
        titleText.text = activityInfo.Title;
        HideCaptionSpace(String.IsNullOrWhiteSpace(titleText.text));
        
        FontSizeControl(titleText.gameObject, titleText, titleText.text);

        if (activityInfo.IsChallenge || activityInfo.Keywords.Count() > 0)
        {
            print($"ApplyDataToEditPanel: activityInfo.Keywords.Count() = {activityInfo.Keywords.Count()}");
            // At this point the selectedTags is the same list as the imageTags
            imageTags = activityInfo.KeywordsForSelection.ToList();
            selectedTags = activityInfo.Keywords.ToList();
            activityInfo.MarkAsAChallenge(true);
            
            captionInputObject.GetComponent<InputField>().text = captionInput;

            if (!isChallengeBeforeEdit) {
                MakeAsChallenge();
            }
            
            HideKeywordSpace(false);
            // challengeMarker.SetActive(true);
            // challengeToggle.isOn = true;
            // challengeToggle.targetGraphic.gameObject.SetActive(false);
            // challengeToggle.graphic.gameObject.SetActive(true);
            
            // keywordsText.text = $"Keyword(s): {string.Join(", ", selectedTags.ToList())}";
            // keywordsOnEditText.text = keywordsText.text;
            // keywordsText.gameObject.SetActive(true);
            // keywordsOnEditText.gameObject.SetActive(true);
        }
        else {
            HideKeywordSpace(true);
        }

        print($"ApplyDataToEditPanel: activityInfo = {activityInfo}");
        captionInput = activityInfo.Title;
        captionInputObject.GetComponent<InputField>().placeholder.GetComponent<Text>().text = activityInfo.Title;
        captionInputObject.GetComponent<InputField>().text = activityInfo.Title;
        titleText.text = activityInfo.Title;
        if(!isChallengeBeforeEdit) {
            challengeToggle.isOn = activityInfo.IsChallenge;
            challengeToggle.targetGraphic.gameObject.SetActive(!activityInfo.IsChallenge);
            challengeToggle.graphic.gameObject.SetActive(activityInfo.IsChallenge);
        }
        challengeMarker.SetActive(activityInfo.IsChallenge);

        //print($"isVideo={isVideo}");
        if (activityInfo.IsVideo) {
            string path;
            if(!RecordManager.Instance.oldContents.ContainsKey(activityInfo.Id)) {
                print($"ApplyDataToEditPanel: setting up panel for video, content from activityInfo.ContentPath");
                path = activityInfo.ContentURL;
                pathFromURL = true;
            }
            else {
                print($"ApplyDataToEditPanel: setting up panel for video, content from local path");
                path = RecordManager.Instance.PathWithNewGUID(RecordManager.Instance.oldContents[activityInfo.Id]);
            }
            RecordManager.Instance.videoPath = path;
            RecordManager.Instance.filteredFilePath = path;
            RecordManager.Instance.contentPublicPath = path;

            // Show elements used for video activities.
            playButton.SetActive(true);

            if (pathFromURL) {
                StartCoroutine(LoadThumbnail());
            }
            else {
                RecordManager.Instance.ShowLocalVideo(contentBoard);
            }

            BeforeOnVideoTaken();
            OnVideoTaken();
            RecordManager.Instance.isPhotoTaking = false;
        }
        else
        {
            string path;
            if(!RecordManager.Instance.oldContents.ContainsKey(activityInfo.Id)) {
                print($"ApplyDataToEditPanel: setting up panel for photo, content from activityInfo.ContentPath");
                path = activityInfo.ContentURL;
            }
            else {
                print($"ApplyDataToEditPanel: setting up panel for photo, content from local path");
                path = RecordManager.Instance.PhotoPathWithNewGUID(RecordManager.Instance.oldContents[activityInfo.Id]);
            }
            RecordManager.Instance.photoPath = path;
            RecordManager.Instance.filteredFilePath = path;
            RecordManager.Instance.contentPublicPath = path;
            // It doesn't matter what we set here as we set the values in the RecordManager already.
            OnPhotoTaken("");
            RecordManager.Instance.isPhotoTaking = true;
        }
        editVideoButton.SetActive(false);
    }

    protected override void ApplyDataToPreviewPlayPanel()
    {

    }

    public override void OnSave()
    {
        if (!saveEditButton.interactable) {
            return;
        }
        
        if (!challengeToggle.isOn) {
            ClearChallengeData();
        }
        else {
            MakeAsChallenge();
        }

        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        //print("PhotoVideoActivity - OnSave");
        if (challengeToggle.isOn && selectedTags.Count < 1 && !uiValues.IsVideo)
        {
            string message = "You need to select at least one keyword.";
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction(message));
            BottomPanelManager.Instance.ShowMessagePanel(message);
            return;
        }

        if (String.IsNullOrWhiteSpace(RecordManager.Instance.filteredFilePath)) {
            // StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("You need to wait for the filtered file to be created."));
            BottomPanelManager.Instance.ShowMessagePanel("You need to wait for the filtered file to be created.");
            return;
        }

        contentBeforeEditLocalPath = RecordManager.Instance.filteredFilePath;
        if (!String.IsNullOrWhiteSpace(titleText.text)) {
            captionBeforeEdit = titleText.text;
        }

        if (challengeToggle.isOn) {
            challengeMarker.SetActive(false);
            keywordsOnEditText.gameObject.SetActive(true);
        }
        isChallengeBeforeEdit = challengeToggle.isOn;

        var info = (PhotoVideoActivityInfo) this.activityInfo;
        if (uiValues.IsVideo || info.IsVideo) {
            print("isVideoBeforeEdit = true");
            isVideoBeforeEdit = true;
            RecordManager.Instance.DeleteAllSegmentsData();
            vp.url = RecordManager.Instance.videoPath;
        }
        challengeToggle.gameObject.SetActive(false);
        editVideoButton.SetActive(false);
        // keywordsOnEditText.gameObject.SetActive(true);

        //save
        base.OnSave();//this has to be called before the action for submitting data
        SubmitPhotoVideo();

        // captionInputObject.SetActive(false);
        // titleText.gameObject.SetActive(true);
    }

    void DisableControls()
    {

    }

    public override void OnCancelCreate()
    {
        if(!SBContextManager.Instance.context.startWithPhotoVideo) {
            base.OnCancelCreate();

            challengeToggle.gameObject.SetActive(keywordsText.text.IsNullOrWhiteSpace());
            keywordsText.gameObject.SetActive(!keywordsText.text.IsNullOrWhiteSpace());
            keywordsOnEditText.gameObject.SetActive(!keywordsText.text.IsNullOrWhiteSpace());

            RecordManager.Instance.CancelRecording();
            RecordManager.Instance.DeletePhotoOrVideo(false);
            RecordManager.Instance.DeleteAllSegmentsData();

            var val = (PhotoVideoActivityInfo) this.uiValues;
            if (val.IsQuickPhotoVideo) {
                OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform
                    .GetComponentInChildren<PostActivity>().transform);
            }
            // captionInputObject.SetActive(false);
            // titleText.gameObject.SetActive(true);
        }
        else {
            RecordManager.Instance.StartEnableMicrophone();
            //StartCoroutine(GetComponentInParent<ActivityManager>().StartCameraUI());
            //UIManager.Instance.StartCameraUI();
            UIManager.Instance.FadeInCameraUI();
            RecordManager.Instance.DeleteAllSegmentsData();
        }
    }

    public override void OnEdit()
    {
        print($"OnEdit: isEverSaved={isEverSaved}, activityInfo.Id={activityInfo.Id}");
        uiValues = ((PhotoVideoActivityInfo)activityInfo).Clone();
        print($"OnEdit: id={uiValues.Id}");
        keywordsText.GetComponent<Button>().interactable = true;
        base.OnEdit();
        playPanel.SetActive(true);
        editVideoButton.SetActive(true);
        playButton.SetActive(false);

        // captionInputObject.SetActive(true);
        // titleText.gameObject.SetActive(false);
        if (!isVideoPanel) { 
            challengeToggle.gameObject.SetActive(!SBContextManager.Instance.context.IsCreatingInConsume());
            keywordsOnEditText.gameObject.SetActive(false);
        }
        // keywordsText.gameObject.SetActive(false);

        DisableSaveButton(true);
        isContentChanged = false;
        
        HideKeywordSpace(false);
        HideCaptionSpace(false);
    }


    public override void OnCancelEdit()
    {
        keywordsText.GetComponent<Button>().interactable = false;
        string previousPath = "";
        print($"PhotoVideoActivity OnCancelEdit contentBeforeEditLocalPath = {contentBeforeEditLocalPath}");
        if(!String.IsNullOrWhiteSpace(contentBeforeEditLocalPath)) {
            previousPath = RecordManager.Instance.filteredFilePath;
            RecordManager.Instance.filteredFilePath = contentBeforeEditLocalPath;
        }

        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        var info = (PhotoVideoActivityInfo) this.activityInfo;
        if (info.IsVideo || uiValues.IsVideo || isVideoPanel || isVideoBeforeEdit) {
            isVideoPanel = true;
            info.IsVideo = true;
            uiValues.IsVideo = true;
            RecordManager.Instance.ConfigureVideoElements(contentBoard, this.gameObject, true);
            RecordManager.Instance.ShowVideoOnPanel();
        }
        else if(previousPath != contentBeforeEditLocalPath){
            RecordManager.Instance.ShowPhoto(contentBoard);
            ApplyDataToUI();
        }

        // There's no need to restore from any instance
        // as we are only updating the "activityInfo"
        // when we are saving the data.
        if (isEverSaved) {
            challengeToggle.gameObject.SetActive(false);
            // keywordsOnEditText.gameObject.SetActive(true);
            if (!String.IsNullOrWhiteSpace(keywordsText.text) && keywordsText.text != "Keyword(s): ") {
                keywordsText.gameObject.SetActive(true);
                
                print($"PhotoVideoActivity OnCancelEdit keywordsText.text = Keyword(s):");
                keywordsText.text = $"Keyword(s): {string.Join(", ", selectedTags)}";
            }
        }

        if (!isChallengeBeforeEdit) {
            keywordsText.text = "";
            keywordsOnEditText.text = "";
            challengeMarker.SetActive(false);
        }

        base.OnCancelEdit();
        //print("OnCancelEdit");
        RecordManager.Instance.CancelRecording();

        // titleText.text = captionBeforeEdit;
        // captionInputObject.SetActive(false);
        // titleText.gameObject.SetActive(true);
    }

    public void OnEditVideo() {
        InteractionManager.Instance.OnPhotoVideoCameraOpen();
        RecordManager.Instance.currPhotoVideoActivity = this;
        RecordManager.Instance.isCreateMode = false;
        RecordManager.Instance.ClearPaths();
        pathFromURL = false;
        RecordManager.Instance.ConfigureVideoElements(contentBoard, this.gameObject, true);
    }
    
    public void OnPlayButton() {
        playButton.SetActive(false);
        pauseButton.SetActive(true);
        print($"-=- ActivityForConsume OnPlayButton()");
        if(pathFromURL) {
            var activityInfo = (PhotoVideoActivityInfo) this.activityInfo;
            print($"-=- ActivityForConsume activityInfo = {activityInfo.ContentURL}");
            // RecordManager.Instance.ShowVideoByMediaPlayer(testPlane.GetComponent<Renderer>(), activityInfo.ContentURL);
            RecordManager.Instance.ShowVideoByMediaPlayer(contentBoard.GetComponent<Renderer>(),
                activityInfo.ContentURL, null,this,true,
                // contentBoard.GetComponent<Renderer>().material.GetTexture("_Albedo"));
            contentBoard.GetComponent<Renderer>().material.GetTexture("_MainTex"));
        }
        else {
            if (currClip == -1) {
                currClip = 0;
            }
            print($"-=- ActivityForConsume OnPlayButton() pathFromURL = false, vp.url = {vp.url}");
            vp.Play();
            // contentBoard.GetComponent<VideoPlayer>().Play();
            // SetAllClipLoader((float) contentBoard.GetComponent<VideoPlayer>().length);
            // PlayAllClipLoader();
        }
    }

    public void OnPauseButton() {
        pauseButton.SetActive(false);
        playButton.SetActive(true);
        print($"-=- ActivityForConsume OnPauseButton()");
        if (pathFromURL) {
            RecordManager.Instance.Pause();
        }
        else {
            contentBoard.GetComponent<VideoPlayer>().Pause();
        }

        StopAllClipLoader();
    }

    public void ShowVideoLoader() {
        videoLoader.SetActive(true);
    }

    public void HideVideoLoader() {
        videoLoader.SetActive(false);
    }

    public void ShowPlayButton() {
        pauseButton.SetActive(false);
        playButton.SetActive(true);
    }

    public void SetAllClipLoader(float clipLenght) {
        allClipLoader.maxValue = clipLenght;
    }

    public void PlayAllClipLoader() {
        if (isAllClipLoaderTimer == null) {
            isAllClipLoaderTimer = AllClipLoaderTimer();
            allClipLoader.value = 0;
            isAllClipLoaderTimerWork = true;
            allClipLoader.gameObject.SetActive(true);
            StartCoroutine(isAllClipLoaderTimer);
        }

        isAllClipLoaderTimerWork = true;
    }

    public void StopAllClipLoader(bool end = false) {
        if (end) {
            isAllClipLoaderTimerWork = false;
            allClipLoader.gameObject.SetActive(false);
            if(isAllClipLoaderTimer != null) {
                StopCoroutine(isAllClipLoaderTimer);
                isAllClipLoaderTimer = null;
            }
        }
        isAllClipLoaderTimerWork = false;
    }
    
    IEnumerator AllClipLoaderTimer() {
        for (float f = 0; f < allClipLoader.maxValue;) {
            if (isAllClipLoaderTimerWork) {
                allClipLoader.value = f;
                f += Time.deltaTime;
                yield return null;
            }
            else {
                yield return null;
            }
        }
        yield return null;
    }
    
    public override void OnSuccessfulSave()
    {
        SBContextManager.Instance.context.UploadedMedia += 1;
        keywordsText.GetComponent<Button>().interactable = false;
        activityInfo = uiValues.Clone();
        RecordManager.Instance.isNewPhotoVideo = false;
        base.OnSuccessfulSave();

        if (!RecordManager.Instance.oldContents.ContainsKey(activityInfo.Id)) {
            print($"AudioActivity: oldContents does NOT Contain activity id({activityInfo.Id})");
            RecordManager.Instance.oldContents.Add(activityInfo.Id, RecordManager.Instance.filteredFilePath);
        }
        else {
            print($"AudioActivity: oldContents Contain activity id({activityInfo.Id})");
            RecordManager.Instance.oldContents.Remove(activityInfo.Id);
            RecordManager.Instance.oldContents.Add(activityInfo.Id, RecordManager.Instance.filteredFilePath);
        }
        RecordManager.Instance.SaveContentPath();        

        if (challengeToggle.isOn) {
            challengeMarker.SetActive(true);
        }
        HideKeywordSpace(!challengeToggle.isOn);
        HideCaptionSpace(String.IsNullOrWhiteSpace(titleText.text));

        if(isVideoPanel) {
            RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
            RecordManager.Instance.ShowVideoOnPanel();
        }

        if (SBContextManager.Instance.context.startWithPhotoVideo)
        {
            GetComponentInParent<ActivityManager>().SetInteractable(0, true);
        }
        
        var val = (PhotoVideoActivityInfo)this.uiValues;
        if (val.IsQuickPhotoVideo) {
            OffScreenIndicatorManager.Instance.SetTarget(anchorController.transform.GetComponentInChildren<PostActivity>().transform);
        }
    }

    public override void OnFailedSave(ErrorInfo error)
    {
        print("ErrorHandler > PhotoVideoActivity.OnFailedSave");
        print($"errorCode: {error.ErrorCode}");        
        base.OnFailedSave(error);
    }

    public void OnOpenCameraButtonClicked()
    {
        //print("camera button on PhotoVideo activity panel clicked");
        InteractionManager.Instance.OnPhotoVideoCameraOpen();
    }


    public void OnPhotoTaken(string contentPath) {
        contentBoard.GetComponent<Renderer>().material = photoMat;
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        isVideoPanel = false;
        //print($"photo taken from native camera, showing photo: contentPath={contentPath} | photoPath={RecordManager.Instance.photoPath} | photoPublicPath={RecordManager.Instance.photoPublicPath}.");
        uiValues.IsVideo = false;
        //SetUIMode(UIMode.Preview);
        playPanel.SetActive(true);
        photoPlayPanel.SetActive(true);
        videoPlayPanel.SetActive(false);
        challengeToggle.gameObject.SetActive(!SBContextManager.Instance.context.IsCreatingInConsume());
        keywordsOnEditText.gameObject.SetActive(false);
        //AnchorManager.Instance.GetCurrentAnchorObject().GetComponent<AnchorController>().SetUIMode(AnchorController.UIMode.Busy);
        // Do we need to create a corresponding "Busy" behavior? --> AnchorManager.Instance.GetCurrentAnchorObject().GetComponent<AnchorController>().SetBehaviourMode(AnchorController.BehaviourMode.Creating_Scanning);

        if (!contentPath.IsNullOrWhiteSpace())
        {
            uiValues.ContentPath = contentPath;
        }

        SetPreviewPlane();
        //start 'fly to the panel' transition animation
        if (!RecordManager.Instance.startWithPhotoVideo && !isApplyData) {
            RecordManager.Instance.FlyToPanelTransitionAnimation(previewForARPhotoEffect);
        }
        // else if(RecordManager.Instance.startWithPhotoVideo){
        //     EnableSaveButton();
        // }
        isApplyData = false;
        playButton.SetActive(false);
        isContentChanged = true;
        
        if (activityInfo.IsChallenge && !contentPath.IsNullOrWhiteSpace()) {
            ClearChallengeData();
        }

        StopAllClipLoader(true);
    }

    void ClearChallengeData() {
        var activityInfo = (PhotoVideoActivityInfo)uiValues;
        selectedTags.Clear();
        MakeAsChallenge();
        activityInfo.MarkAsAChallenge(false);
        activityInfo.Keywords = new List<string>();
        activityInfo.AlternateKeywords = new List<string>();
    }

    void SetPreviewPlane() {
        var photoWasShown = false;
        if (!RecordManager.Instance.filteredFilePath.IsNullOrWhiteSpace())
        {
            try
            {
                VideoPlayer vp = contentBoard.GetComponent<VideoPlayer>();
                vp.enabled = false;
                var bytes = System.IO.File.ReadAllBytes(RecordManager.Instance.filteredFilePath);
                Texture2D tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                // Texture tex = contentBoard.GetComponent<Renderer>().material.GetTexture("_Albedo");
                // contentBoard.GetComponent<Renderer>().material.SetTexture("_Albedo", tex);
                contentBoard.GetComponent<Renderer>().material.SetTexture("_MainTex", tex);
                photoWasShown = true;
                RecordManager.Instance.SetARPanelByContentSize(contentBoard, tex.width, tex.height);
                print($"-=- SetPreviewPlane() Done RecordManager.Instance.filteredFilePath = {RecordManager.Instance.filteredFilePath}");
                return;
            }
            catch
            {
                print($"PhotoVideoActivity.SetPreviewPlane() failed ");
            }
        }
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        if (!activityInfo.ContentURL.IsNullOrWhiteSpace() && !photoWasShown)
        {
            print($"PhotoVideoActivity.SetPreviewPlane - loading from public URL: {activityInfo.ContentURL}");
            this.StartThrowingCoroutine(DownloadPhoto(activityInfo.ContentURL, contentBoard, (texture, panel) =>
            {
                print("FROM AR: photo downloaded, showing on the panel...");
                // panel.GetComponent<Renderer>().material.SetTexture("_Albedo", texture);
                panel.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);
            }), e =>
            {
                print("FROM AR: photo CANNOT be downloaded!");
            });
        }
    }

    IEnumerator DownloadPhoto(string url, GameObject panel, Action<Texture2D, GameObject> callback)
    {
        print("FROM AR: DownloadPhoto");
        // Then let's load the photo from a URL.
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                print("Photo download failed."); ;

                // ToDo: log this error and notify ourselves
                callback(null, panel);
            }
            else
            {
                callback(((DownloadHandlerTexture)www.downloadHandler).texture, panel);
            }
        }
    }

    public void OnVideoTaken()
    {
        // var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        isVideoPanel = true;
        // print("PhotoVideoActivity OnVideoTaken() video taken from native camera, showing video.");
        // uiValues.IsVideo = true;
        // //SetUIMode(UIMode.Preview);
        // playPanel.SetActive(true);
        // photoPlayPanel.SetActive(false);
        // videoPlayPanel.SetActive(true);
        //
        // //Todo: show video here:
        // challengeMarker.SetActive(false);
        // challengeToggle.gameObject.SetActive(false);
        print("PhotoVideoActivity OnVideoTaken() video taken from native camera, showing video.");

        
        VideoPlayer vp = contentBoard.GetComponent<VideoPlayer>();
        vp.enabled = true;
        
        RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
        RecordManager.Instance.ShowVideoOnPanel();

        if (!RecordManager.Instance.startWithPhotoVideo && !isApplyData) {
            RecordManager.Instance.FlyToPanelTransitionAnimation(previewForARPhotoEffect);
        }
        isApplyData = false;
        isContentChanged = true;
        //start 'fly to the panel' transition animation
        // previewForARPhotoEffect.SetActive(true);
        // RecordManager.Instance.FlyToPanelTransitionAnimation(previewForARPhotoEffect);
    }

    public void BeforeOnVideoTaken() {
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        isVideoPanel = true;
        print("PhotoVideoActivity BeforeOnVideoTaken() video taken from native camera, showing video.");
        uiValues.IsVideo = true;
        //SetUIMode(UIMode.Preview);
        playPanel.SetActive(true);
        // photoPlayPanel.SetActive(false);
        // videoPlayPanel.SetActive(true);
        // if (previewForARPlane != null) {
        //     contentBoard.GetComponent<MeshRenderer >().material.SetTexture("_Albedo", previewForARPlane);
        // }

        //Todo: show video here:
        challengeMarker.SetActive(false);
        challengeToggle.gameObject.SetActive(false);
        if (!String.IsNullOrWhiteSpace(keywordsText.text) && keywordsText.text != "Keyword(s): ") {
            keywordsText.gameObject.SetActive(true);
        }
        // keywordsOnEditText.gameObject.SetActive(true);
    }

    public void ShowThumbnail(Texture2D tex) {
        print($"-=- PhotoVideoActivity ShowThumbnail tex.width = {tex.width}");
        // contentBoard.GetComponent<MeshRenderer >().material.SetTexture("_Albedo", tex);
        contentBoard.GetComponent<MeshRenderer >().material.SetTexture("_MainTex", tex);
        RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
        RecordManager.Instance.SetARPanelByContentSize(contentBoard, tex.width, tex.height);
    }

    IEnumerator GetImageKeywords(bool isChallenge)
    {
        // ToDo: Make sure the button (or whatever UI element) that will call this method is disabled
        // if there is no photo that was taken or selected from the gallery.

        var uiValues = (PhotoVideoActivityInfo)this.uiValues;

        uiValues.MarkAsAChallenge(isChallenge);

        if (isChallenge)
        {
            // But we will be explicit here and throw an exception just in case.
            if (uiValues.UrlToUseForValidation.IsNullOrWhiteSpace())
            {
                throw new ApplicationException("This photo activity cannot be marked as a challenge because the photoURL is empty!");
            }

            print($"-=- uiValues.UrlToUseForValidation = {uiValues.UrlToUseForValidation}");
            InteractionManager.Instance.OnWillGetImageKeywords(uiValues.UrlToUseForValidation, false);
        }
        else
        {
            ClearKeywords();
        }
        yield return null;
    }

    /// <summary>
    /// The method that prepares the panel and the keyword buttons.
    /// </summary>
    /// <param name="keywords">The keywords to show on the panel.</param>
    /// <param name="showTags">If true, the panel is shown. Otherwise, the buttons will be generated but the panel will be hidden.</param>
    public void ShowKeywords(string keywords)
    {
        ShowChallengeLoader(false);
        EnableCancelButtons(true);
        challengeMarker.SetActive(true);
        //isGettinKeywords = false;
        challengeToggle.enabled = true;
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;

        // We are only interested on the top 10 keywords.
        // Keywords are passed from that native sorted by relevance.
        imageTags = keywords.Split(',').Take(10).ToList();
        // Set this in the activity info already.
        uiValues.AlternateKeywords = keywords.Split(',').Take(10).ToList();

        if (imageTags.Count < 1)
        {
            // ToDo: Here, set the UI appropriately like show a message to the user that there are no keywords.
            // If we can disabled the Save button then that is good.
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("Keywords cannot be generated from your selected photo. Please choose another photo."));
            BottomPanelManager.Instance.ShowMessagePanel("Keywords cannot be generated from your selected photo. Please choose another photo.");
            return;
        }

        if (imageTags.Contains("NOT_SAFE"))
        {
            // The image was flagged as not safe.

            // ToDo: Here, set the UI appropriately and inform the user that the image is not safe for uploading.
            //  Show a message in the bottom panel that the photo is not safe for uploading, something like:
            //      Your photo is not safe for uploading.
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("Your photo is not safe for uploading. Please choose another photo."));
            BottomPanelManager.Instance.ShowMessagePanel("Your photo is not safe for uploading. Please choose another photo.");
            return;
        }

        // At this point we are guaranteed that the image is safe and keywords are generated from it.
        // ToDo: Use the "tags" variable to show the list of keywords on the UI.
        //

        PreparePanelForKeywords(false, true);

        print($"PhotoVideoActivity.ShowKeywords: keywords found = {keywords}");
    }

    void ClearKeywords()
    {
        // ToDo: clear the UI here
        if(pagesCurrObjects.Count >= 0) {
            foreach (var currObjects in pagesCurrObjects) {
                foreach (var currObject in currObjects) {
                    Destroy(currObject);
                }
            }
        }
        selectedTags.Clear();
        pagesCurrObjects.Clear();
        lineObjects.Clear();
        lines.Clear();
        pageWithTags = 0;
        currObjects.Clear();

        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        uiValues.Keywords.ToList().Clear();
        uiValues.AlternateKeywords.ToList().Clear();
    }

    /// <summary>
    /// Submit the photo or video data to the API.
    /// </summary>
    /// <param name="caption">The optional caption of the photo or video.</param>
    void SubmitPhotoVideo()
    {
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;

        print($"PhotoVideoActivity.SubmitPhotoVideo: submitting a {(uiValues.IsVideo ? "Video" : "Photo")} with ID = {uiValues.Id} | IsQuickPhotoVideo={uiValues.IsQuickPhotoVideo}");
        
        var refreshPolicy = SBContextManager.Instance.context.UploadedMedia < 1;
        var experienceId = SBContextManager.Instance.context.experienceId;

        var info = (PhotoVideoActivityInfo)this.activityInfo;

        if (!RecordManager.Instance.isNewPhotoVideo && !SBContextManager.Instance.context.isOffline)
        {            
            var assetType = uiValues.IsVideo ? AssetType.Video : AssetType.Photo;
            if (uiValues.IsVideo || info.IsVideo)
            {
                SubmitVideo(uiValues.Title, uiValues.ContentURL);
                RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
            }
            else
            {
                SubmitPhoto(uiValues.Title, uiValues.ContentURL);
            }
            return;
        }
  
        if (SBContextManager.Instance.context.isOffline) {
            print("PhotoVideoActivity - SBContextManager.Instance.context.isOffline");
            if (uiValues.IsVideo || info.IsVideo) {
                SubmitVideo(uiValues.Title, "");
                RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
            }
            else {
                SubmitPhoto(uiValues.Title, "");
            }
            return;
        }
        
        print("PhotoVideoActivity - starting upload...");
        // Upload the photo or video first to our blob server.
        //var assetType = uiValues.IsVideo ? AssetType.Video : AssetType.Photo;        
        this.StartThrowingCoroutine(SBRestClient.Instance.GetExperienceContainerUrlIEnumerator(experienceId, refreshPolicy, OnSasUrlReceived, OnSasUrlError)
            , e =>
            {                                
                OnSasUrlError(ErrorInfo.CreateNetworkError());
            });
    }

    void OnSasUrlReceived(string sasURL)
    {
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        var info = (PhotoVideoActivityInfo) this.activityInfo;
        var assetType = uiValues.IsVideo ? AssetType.Video : AssetType.Photo;
        if(anchorController.isReborn && !isContentChanged) {
            if (uiValues.IsVideo || info.IsVideo) {
                SubmitVideo(uiValues.Title, uiValues.ContentURL);
                RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
            }
            else {
                SubmitPhoto(uiValues.Title, uiValues.ContentURL);
            }
        }
        else {
            this.StartThrowingCoroutine(SBRestClient.Instance.UploadBlobIEnumerator(sasURL, Guid.NewGuid().ToString(),
                uiValues.Title, assetType, ContinueSubmitPhotoVideo, ContinueOnError), e =>
                {
                    print("ErrorHandler > callback - UploadBlobIEnumerator");
                    print($"Message={e.Message}");
                    print($"StackTrace={e.StackTrace}");
                    ContinueOnError(ErrorInfo.CreateNetworkError());
                });
        }
    }

    void OnSasUrlError(ErrorInfo error)
    {
        print($"Retrieving SASURL failed!");
        // "*" at then end of the message is intentional.
        // We are using the same message when getting the sasURL fails and uploading the media.
        BottomPanelManager.Instance.ShowMessagePanel("Your content cannot be uploaded at this time. Please try again later. *", true, false, () =>
        {
            OnFailedSave(error);
        });        
    }

    void ContinueSubmitPhotoVideo(string caption, string blobURL)
    {
        print($"PhotoVideoActivity.ContinueSubmitPhotoVideo: blobURL={blobURL}");

        if (blobURL.IsNullOrWhiteSpace())
        {
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("Your content cannot be uploaded at this time. Please try again later."));
            BottomPanelManager.Instance.ShowMessagePanel("Your content cannot be uploaded at this time. Please try again later.");
            return;
        }

        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        var info = (PhotoVideoActivityInfo) this.activityInfo;
        if (String.IsNullOrWhiteSpace(caption)) {
            caption = " ";
        }
        
        if (uiValues.IsVideo || info.IsVideo)
        {            
            SubmitVideo(caption, blobURL);
            // --- need to do after submit success
            RecordManager.Instance.ConfigureVideoElements(contentBoard, gameObject, true);
            // RecordManager.Instance.ShowVideoOnPanel();
        }
        else
        {
            SubmitPhoto(caption, blobURL);
            // RecordManager.Instance.ShowPhoto(contentBoard);
        }
    }

    void SubmitPhoto(string caption, string blobURL)
    {
        //print("PhotoVideoActivity.SubmitPhoto");

        var activityInfo = (PhotoVideoActivityInfo)uiValues;

        activityInfo.Title = caption;
        activityInfo.ContentURL = blobURL;
        activityInfo.ContentPath = RecordManager.Instance.photoPath;

        // The "samplePhotos" will not be assigned a value here
        // but instead in the callback.
        Location anchorLocation = GetComponentInParent<AnchorController>().GetSBLocationInfo();
        var input = PhotoActivityInput.CreateFrom(activityInfo,
               SBContextManager.Instance.context.experienceId,
               SBContextManager.Instance.context.collectionId,
               anchorLocation,
               SBContextManager.Instance.context.isPlanning);
        input.ARAnchorId = GetComponentInParent<AnchorController>().GetAnchorInfo().id;
        
        print($"PhotoVideoActivity.SubmitPhoto: data={input}.");
        
        if (activityInfo.IsEditing)
            SBRestClient.Instance.UpdatePhoto(activityInfo.Id, input);
        else
            SBRestClient.Instance.CreatePhoto(activityInfo.Id, input);
    }

    void SubmitVideo(string caption, string blobURL)
    {
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;

        uiValues.Title = caption;
        uiValues.ContentURL = blobURL;
        uiValues.ContentPath = RecordManager.Instance.videoPath;

        Location anchorLocation = GetComponentInParent<AnchorController>().GetSBLocationInfo();
        var input = VideoActivityInput.CreateFrom(uiValues,
               SBContextManager.Instance.context.experienceId,
               SBContextManager.Instance.context.collectionId,
               anchorLocation,
               SBContextManager.Instance.context.isPlanning);
        input.ARAnchorId = GetComponentInParent<AnchorController>().GetAnchorInfo().id;

        print($"PhotoVideoActivity.SubmitVideo: data={input}, blobURL={blobURL}.");

        if (activityInfo.IsEditing)
            SBRestClient.Instance.UpdateVideo(activityInfo.Id, input);
        else
            SBRestClient.Instance.CreateVideo(activityInfo.Id, input);
    }

    // --- Caption & Keywords UI communication
    // ToDo: this method doesn't seem to be used?
    public void KeepCaptionText() {
        string captionText = captionInputObject.GetComponent<InputField>().text;
        print("KeepCaptionText = " + captionText);

        captionInput = captionText;
        uiValues.Title = captionText;
        titleText.text = captionText;
        if (!RecordManager.Instance.isPhotoTaking && RecordManager.Instance.status == "") {
            EnableSaveButton();
        }
        else if(RecordManager.Instance.isPhotoTaking){
            EnableSaveButton();
        }

        // We don't need to set these here.
        // The user needs to be able to type as long as the activity has not been saved yet.
        // captionInputObject.SetActive(false);
        // titleText.gameObject.SetActive(true);
    }

    void SetMakeChallengeToggle(bool showTags = true, bool isVideo = false)
    {
        print("SetMakeChallengeToggle");
        //challengeMarker.SetActive(true);
        challengeToggle.gameObject.SetActive(!isVideo);
        challengeToggle.onValueChanged.RemoveAllListeners();
        challengeToggle.onValueChanged.AddListener(delegate { MakeChallengeToggle(challengeToggle); });

        if (isEverSaved && !isVideo)
        {
            SetKeywordsInField(imageTags.ToArray(), true, showTags);
        }
    }

    void PreparePanelForKeywords(bool showAsSelected, bool showTags = true)
    {
        //captionInputObject.SetActive(false);
        //challengeToggle.gameObject.SetActive(false);
        keywordsPanel.SetActive(true);
        keywordsButtonPanel.SetActive(true);
        playPanel.SetActive(false);
        SetKeywordsInField(imageTags.ToArray(), showAsSelected, showTags);
    }

    void MakeChallengeToggle(Toggle change)
    {
        print("MakeChallengeToggle");
        challengeMarker.SetActive(challengeToggle.isOn);
        if (challengeToggle.isOn) {
            print("challengeToggle.is On");
            challengeToggle.targetGraphic.gameObject.SetActive(false);
            challengeToggle.graphic.gameObject.SetActive(true);
            DisableSaveButton();
        }
        else {
            print("challengeToggle.is Not On");
            challengeToggle.targetGraphic.gameObject.SetActive(true);
            challengeToggle.graphic.gameObject.SetActive(false);
            return;
        }

        // NativeCall.Instance.OpenGallery(activityInfo.Id);
        // return;
        //print("MakeChallengeToggle");


        //NativeCall.Instance.OpenGallery(activityInfo.Id);
        //return;


        //if (isGettinKeywords)
        //{
        //    print("Still getting the keywords, returning now.");
        //    return;
        //}
        //isGettinKeywords = true;
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;

        if (uiValues.ContentPath.IsNullOrWhiteSpace()) {
            challengeToggle.isOn = false;
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("Please take a photo first."));
            BottomPanelManager.Instance.ShowMessagePanel("Please take a photo first.");
            return;
        }
        challengeToggle.enabled = false;

        //print("PhotoVideoActivity.MakeChallengeToggle");
        if (change.isOn)
        {

            // Clean up previous keywords
            ClearKeywords();

            if (!uiValues.ContentPath.IsNullOrWhiteSpace())
            {
                //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("Retrieving keywords for your photo, please wait..."));
                BottomPanelManager.Instance.ShowMessagePanel("Retrieving keywords for your photo, please wait...");
                StartCoroutine(GetImageKeywords(true));
            }
        }
        uiValues.MarkAsAChallenge(change.isOn);
        challengeToggle.gameObject.SetActive(false);
        ShowChallengeLoader(true);
        EnableCancelButtons(false);
    }

    public void OnCancelChallenge(bool enableCancelButton = false) {
        pageWithTags = 0;
        challengeToggle.isOn = false;
        challengeToggle.gameObject.SetActive(true);
        challengeToggle.enabled = true;
        keywordsText.text = "";
        keywordsOnEditText.text = "";

        keywordsPanel.SetActive(false);
        keywordsButtonPanel.SetActive(false);
        playPanel.SetActive(true);
        challengeMarker.SetActive(false);
        EnableSaveButton();

        if (enableCancelButton)
        {
            EnableCancelButtons(true);
        }
    }

    public void MakeAsChallenge()
    {
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;

        print($"PhotoVideoActivity.MakeAsChallenge: selectedTags={string.Join(",", selectedTags)}");

        // Let's make sure that each time the keywords panel shows up, we are showing the first page.
        pageWithTags = 0;
        if (selectedTags.Count > 0)
        {
            challengeToggle.gameObject.SetActive(false);
            // keywordsText.gameObject.SetActive(true);
            // titlePanel.SetActive(true);

            string keywords = "Keyword(s): ";
            for (int i = 0; i < selectedTags.Count; i++)
            {
                if (i == selectedTags.Count - 1)
                {
                    keywords += selectedTags[i] + ".";
                }
                else
                {
                    keywords += selectedTags[i] + ", ";
                }
            }

            keywordsText.text = keywords;
            keywordsOnEditText.text = keywords;
            keywordsText.gameObject.SetActive(true);
            keywordsOnEditText.gameObject.SetActive(true);
            titleText.text = captionInput;

            uiValues.Keywords = selectedTags;
        }
        else
        {
            // Unselecting all the keywords will:
            //  1. Mark the activity as not a challenge, the challengeToggle will be unchecked
            challengeToggle.isOn = false;
            challengeToggle.gameObject.SetActive(true);
            challengeToggle.enabled = true;
            //  2. Clear the keywords label
            keywordsText.text = "";
            keywordsOnEditText.text = "";
            challengeMarker.SetActive(false);
            //  3. Show the caption input
            // captionInputObject.SetActive(true);
            //  4. Hide the title panel
            // titleText.gameObject.SetActive(false);
        }
        keywordsPanel.SetActive(false);
        keywordsButtonPanel.SetActive(false);
        playPanel.SetActive(true);
        EnableSaveButton(true);
    }

    public void ShowPlayButtons() {
        print($"-=- PhotoVideoActivity  ShowPlayButtons");
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        var info = (PhotoVideoActivityInfo) this.activityInfo;
        if (info.IsVideo || uiValues.IsVideo) {
            playButton.SetActive(true);
            pauseButton.SetActive(false);
            // previewForARPhotoEffect.SetActive(false);
        }
    }

    void ShowPlayButton(VideoPlayer vp) {
        print($"-=- PhotoVideoActivity  ShowPlayButton");
        if (currClip != -1) {
            if (currClip < videoPaths.Count - 1) {
                return;
            }
        }
        print($"-=- PhotoVideoActivity  ShowPlayButton1");
        playButton.SetActive(true);
        pauseButton.SetActive(false);
        StopAllClipLoader(true);
    }

    public void Fullscreen() {
        VideoPlayer vp = contentBoard.GetComponent<VideoPlayer>();
        RecordManager.Instance.Fullscreen(vp, isVideoPanel, pathFromURL, filterIndex);
    }

    public void ShowChallengeLoader(bool visible) {
        if (challengeLoader == null) {
            print("PhotoVideoActivity > ShowChallengeLoader: challengeLoader is null");
            return;
        }

        UnityGif uGif = challengeLoader.GetComponentInChildren<UnityGif>();
        if (visible) {
            challengeLoader.SetActive(true);
            uGif?.Play();
        }
        else {
            uGif?.Pause();
            challengeLoader.SetActive(false);
        }
    }

    public void StartTrackCharacters(InputField input)
    {
        FontSizeControl(input.gameObject, input.textComponent, input.text);//update the font size according to the number of characters
        UIManager.Instance.StartTrack(input);
    }

    public void FinishTrackCharacters() {
        UIManager.Instance.FinishTrack();
    }
    
    private void FontSizeControl(GameObject go, Text text, string textValue)
    {
        if (go.name == "Caption")
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
    }

    
    public void EnableSaveButton() {
        EnableSaveButton(true);
        nextChallengeButton.interactable = true;
        fullScreenButton.interactable = true;
    }

    public void DisableSaveButton(bool fullScreen = false) {
        EnableSaveButton(false);
        nextChallengeButton.interactable = false;
        if (fullScreen) {
            fullScreenButton.interactable = false;
        }
    }

    public void ShowPreviewImage() {
        var uiValues = (PhotoVideoActivityInfo)this.uiValues;
        var info = (PhotoVideoActivityInfo) this.activityInfo;
        if (info.IsVideo || uiValues.IsVideo || isVideoPanel) {
            RecordManager.Instance.ShowThumbnail(contentBoard);
        }
    }

    public void ConfigureSegments(List<string> paths, int filterI) {
        vp.enabled = true;
        videoPaths = paths;
        filterIndex = filterI;
        vp.url = paths[0];
        currClip = 0;
        pathFromURL = false;

        Material mat = new Material(RecordManager.Instance.filterMats[filterI].shader);
        mat.CopyPropertiesFromMaterial(RecordManager.Instance.filterMats[filterI]);
        mat.SetInt("_CompareFunction", 3);
        contentBoard.GetComponent<Renderer>().material = mat;
        fullScreenButton.interactable = true;
        print($"ConfigureSegments contentBoard.GetComponent<Renderer>().material.shader = {mat.shader}\n paths.Count = {paths.Count}");
    }
    
    void ChangeClip(VideoPlayer vp) {
        print($"PhotoVideoActivity ChangeClip1, currClip = {currClip}");
        if (currClip != -1) {
            currClip++;
            vp.url = videoPaths[currClip];
            if (currClip == videoPaths.Count - 1) {
                currClip = -1;
            }
            vp.Play();
        }
    }
    

    public IEnumerator LoadThumbnail(bool wait = false) {
        if (wait) {
            yield return new WaitForSeconds(0.1f);
        }

        if (thumbnailTex != null) {
            // contentBoard.GetComponent<Renderer>().material.SetTexture("_Albedo", thumbnailTex);
            contentBoard.GetComponent<Renderer>().material.SetTexture("_MainTex", thumbnailTex);
        }
        print($"PhotoVideoActivityForConsume IEnumerator LoadThumbnail");

        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        thumbnailTex = new Texture2D(1, 1);

        WWW www = new WWW(activityInfo.Thumbnail);
        yield return www;

        www.LoadImageIntoTexture(thumbnailTex);
        // contentBoard.GetComponent<Renderer>().material.SetTexture("_Albedo", thumbnailTex);
        contentBoard.GetComponent<Renderer>().material.SetTexture("_MainTex", thumbnailTex);
        www.Dispose();
        www = null;
    }

    private bool captionHide;
    void HideCaptionSpace(bool hide) {
        if(captionHide == hide)
            return;
        
        captionCover.gameObject.SetActive(!hide);
        float newHeight = captionCover.GetComponent<RectTransform>().rect.height / 2;
        print($"PhotoVideoActivity HideCaptionSpace, hide = {hide}, newHeight = {newHeight}");
        
        if (hide) {
            buttonsRoot.localPosition = new Vector3(0, buttonsRoot.localPosition.y + newHeight, 0);
            playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x,playPanel.transform.localPosition.y - newHeight, playPanel.transform.localPosition.z);
            challengeMarker.transform.localPosition = new Vector3(challengeMarker.transform.localPosition.x, challengeMarker.transform.localPosition.y - newHeight, challengeMarker.transform.localPosition.z);
        }
        else {
            buttonsRoot.localPosition = new Vector3(0, buttonsRoot.localPosition.y - newHeight, 0);
            playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x, playPanel.transform.localPosition.y + newHeight, playPanel.transform.localPosition.z);
            challengeMarker.transform.localPosition = new Vector3(challengeMarker.transform.localPosition.x, challengeMarker.transform.localPosition.y + newHeight, challengeMarker.transform.localPosition.z);
        }
        captionHide = hide;
    }
    
    private bool keywordHide;
    void HideKeywordSpace(bool hide) {
        if(keywordHide == hide)
            return;
        
        keywordCover.gameObject.SetActive(!hide);
        float newHeight = keywordCover.GetComponent<RectTransform>().rect.height / 2;
        print($"PhotoVideoActivity HideKeyWordSpace, hide = {hide}, newHeight = {newHeight}");
        
        if (hide) {
            buttonsRoot.localPosition = new Vector3(0, buttonsRoot.localPosition.y + newHeight, 0);
            playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x,playPanel.transform.localPosition.y - newHeight, playPanel.transform.localPosition.z);
            challengeMarker.transform.localPosition = new Vector3(challengeMarker.transform.localPosition.x,challengeMarker.transform.localPosition.y - newHeight, challengeMarker.transform.localPosition.z);
        }
        else {
            buttonsRoot.localPosition = new Vector3(0, buttonsRoot.localPosition.y - newHeight, 0);
            playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x,playPanel.transform.localPosition.y + newHeight, playPanel.transform.localPosition.z);
            challengeMarker.transform.localPosition = new Vector3(challengeMarker.transform.localPosition.x,challengeMarker.transform.localPosition.y + newHeight, challengeMarker.transform.localPosition.z);
        }
        keywordHide = hide;
    }

    void EnableCancelButtons(bool enable) {
        foreach (var cancelButton in cancelButtons) {
            cancelButton.interactable = enable;
        }
    }
    

    #region Keywords UI

    List<List<string>> lines = new List<List<string>>();
    private int pageWithTags = 0;
    List<GameObject> lineObjects = new List<GameObject>();
    List<GameObject> currObjects = new List<GameObject>();
    private List<List<GameObject>> pagesCurrObjects = new List<List<GameObject>>();
    //bool end = false;

    /// <summary>
    /// Run when we got tags or want to show tags in panel.
    /// </summary>
    /// <param name="tags"></param>
    /// <param name="showAsSelected">If true, the keywords will be shows as pre-selected.</param>
    /// <param name="showTags">If false, the tags will be generated but will not be shown.</param>
    void SetKeywordsInField(string[] tags, bool showAsSelected = false, bool showTags = true)
    {
        print($"PhotoVideoActivity.SetKeywordsInField: tags={tags.Length}, showAsSelected={showAsSelected}, showTags={showTags}");
        float wDist = 32.0f;
        float wFrame = keywordField.GetComponent<RectTransform>().rect.width;
        List<string> line = new List<string>();

        // --- at first we creat split string[] tags on lines. Which will be showed
        float wline = 0;
        for (int i = 0; i < tags.Length; i++)
        {
            float tagWidth = 60 + (tags[i].Length * 17.7f);

            if (wline + tagWidth < wFrame)
            {
                line.Add(tags[i]);
                wline = wline + tagWidth + wDist;
                if (i == tags.Length - 1)
                {
                    lines.Add(line);
                }
            }
            else
            {
                lines.Add(line);
                line = new List<string>();
                line.Add(tags[i]);
                wline = tagWidth + wDist;
            }
        }

        GenerateKeywordButtons(showAsSelected);
        // Always start at page 0.
        pageWithTags = 0;
        if (showTags)
            ShowTags();
    }

    // --- set size each button
    GameObject KeywordButton(string tag)
    {
        GameObject newTagButton = keywordPrefab;
        newTagButton.GetComponent<RectTransform>().sizeDelta = new Vector2(60 + (tag.Length * 17.7f), 88);
        return keywordPrefab;
    }

    void GenerateKeywordButtons(bool showAsSelected = false) {
        int pagesCont = lines.Count / 3;
        if (lines.Count % 3 != 0) {
            pagesCont++;
        }

        for(int iPage = 0; iPage < pagesCont; iPage++) {
            print(
                $"PhotoVideoActivity.GenerateKeywordButtons: showAsSelected={showAsSelected}, selectedTags={string.Join(",", selectedTags)}, currObjects.Count = {currObjects.Count}");
            if (lineObjects.Count == 0) {
                HorizontalLayoutGroup[] hlg = keywordField.GetComponentsInChildren<HorizontalLayoutGroup>();
                foreach (var line in hlg) {
                    lineObjects.Add(line.gameObject);
                }
            }

            foreach (var tagObj in currObjects) {
                tagObj.SetActive(false);
            }

            currObjects = new List<GameObject>();

            for (int i = 0; i < 3; i++) {
                if (lines.Count > i + (pageWithTags * 3)) {
                    foreach (var tag in lines[i + (pageWithTags * 3)]) {
                        GameObject newTag = Instantiate(KeywordButton(tag), lineObjects[i].transform);
                        Toggle m_Toggle = newTag.GetComponent<Toggle>();
                        if (selectedTags.Contains(tag) && showAsSelected) {
                            m_Toggle.isOn = true;
                        }

                        m_Toggle.onValueChanged.AddListener(delegate { ToggleValueChanged(m_Toggle, tag); });
                        currObjects.Add(newTag);
                        newTag.GetComponentInChildren<Text>().text = tag;
                        // -- if it is the last line
                        if (lines.Count == i + (pageWithTags * 3) + 1) {
                            //end = true;
                            break;
                        }
                    }
                }
            }

            pagesCurrObjects.Add(currObjects);
            pageWithTags++;
        }
    }

    // --- more button uses this method
    public void ShowTags()
    {
        foreach (var currentObjects in pagesCurrObjects)
        {
            foreach (var tagObj in currentObjects)
            {
                tagObj.SetActive(false);
            }
        }

        print($"ShowTags - pageWithTags={pageWithTags}, pagesCurrObjects={pagesCurrObjects.Count}");
        if (pageWithTags >= 0 && pageWithTags < pagesCurrObjects.Count)
        {
            foreach (var tagObj in pagesCurrObjects[pageWithTags])
                tagObj.SetActive(true);
        }

        if (pageWithTags == pagesCurrObjects.Count - 1)
        {
            pageWithTags = 0;
        }
        else
        {
            pageWithTags++;
        }
        print($"ShowTags - pageWithTags={pageWithTags}.");
    }

    // --- getting changes from created buttons
    void ToggleValueChanged(Toggle change, string tag)
    {
        if (change.isOn)
        {
            if (selectedTags.Count <= 4)
            {
                selectedTags.Add(tag);
            }
            else
            {
                change.isOn = false;
            }
        }
        else
        {
            selectedTags.Remove(tag);
        }

        string keywords = "Keyword(s): ";
        for (int i = 0; i < selectedTags.Count; i++)
        {
            if (i == selectedTags.Count - 1)
            {
                keywords += selectedTags[i] + ".";
            }
            else
            {
                keywords += selectedTags[i] + ", ";
            }
        }
        keywordsTextOnKeywordsPanel.text = keywords;
        keywordsOnEditText.text = keywords;

        //keywordsText should only be disabled during preview
        //keywordsText.GetComponent<Button>().interactable = false;

        if (selectedTags.Count > 0) {
            EnableSaveButton();
        }
        else {
            DisableSaveButton();
        }
    }

    #endregion

}
