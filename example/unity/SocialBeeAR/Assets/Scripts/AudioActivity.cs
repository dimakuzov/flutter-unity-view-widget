using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using SocialBeeAR;
using UnityEngine.UI;
using UnityEngine.Video;


public class AudioActivity : SBActivity {

    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject playAudioPanel;
    
    public Button saveTitleButton;
    // public GameObject playButton;
    // public GameObject pauseAndMoveButtons;
    [SerializeField] private Text titleText;
    [SerializeField] private Text titleInputPlaceholder;
    [SerializeField] private GameObject captionInputObject;
    [SerializeField] private GameObject captionCover;

    [SerializeField] GameObject outline;
    [SerializeField] GameObject rootForRotateEffect;
    
    [SerializeField] private GameObject[] playButtons;
    [SerializeField] private GameObject[] pauseButtons;
    [SerializeField] GameObject editSoundButton;
    [SerializeField] GameObject waitAudioClipAlert;

    Color activeColor = new Color(1, 0.57f, 0.343f, 1);
    Color notActiveColor = new Color(0.765f, 0.765f, 0.765f, 1);
    
    VideoPlayer audioVP;
    private string captionInput;
    private string descriptionInput;
    [HideInInspector] public bool isBorn;
    private int currClip;
    private AudioSource audioSource;
    bool pathFromURL = false;

    private void OnEnable() {
        audioSource = GetComponent<AudioSource>();
        audioVP = GetComponent<VideoPlayer>();
        RecordManager.Instance.OnAudioMergeCompleted += OnAudioMergeCompleted;
        // audioVP.prepareCompleted += ChangeClip;
        if (SBContextManager.Instance.context.IsConsuming()) {
            audioVP.loopPointReached += AudioComplete;
        }
        audioVP.loopPointReached += ClipOver;
        // audioVP.loopPointReached += ChangeClip;
    }

    void OnDisable() {
        RecordManager.Instance.OnAudioMergeCompleted -= OnAudioMergeCompleted;
        if (SBContextManager.Instance.context.IsConsuming()) {
            audioVP.loopPointReached -= AudioComplete;
        }
        audioVP.loopPointReached -= ClipOver;
    }

    public override void Born(string id, ActivityType type, string experienceId, Pose anchorPose, string parentId = "", string mapId = "", string anchorPayload = "")
    {
        if (parentId.IsNullOrWhiteSpace())
            throw new ArgumentNullException("An AudioActivity cannot be created without a parent. ParentId is required.");

        base.Born(id, type, experienceId, anchorPose, parentId, mapId, anchorPayload);

        isBorn = true;
        
        //commented off by cliff, these action should be executed AFTER tween animation for sliding up activity panel.
        // UIManager.Instance.SetUIMode(UIManager.UIMode.Audio);
        // RecordManager.Instance.SetPanel(this, true);
        //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        
        anchorController.AudioExist(true);
    }

    public override void Reborn(IActivityInfo activityInfo)
    {
        print(
            $"AudioActivity.Reborn - activityInfo = {(activityInfo == null ? "NULL" : activityInfo.ToString())}");
        if (activityInfo.ParentId.IsNullOrWhiteSpace())
            throw new ArgumentNullException(
                "An AudioActivity cannot be reborn without a parent. ParentId is required.");

        activityInfo.IsEditing = true;
        base.Reborn(activityInfo);

        ApplyDataToEditPanel();
        ApplyDataToUI();

        if (activityInfo.Status == ActivityStatus.Verified)
        {
            ActivityCompleteSuccess();
        }
        else if (activityInfo.Status != ActivityStatus.New)
        {            
        }

        isBorn = false;

        // if (SBContextManager.Instance.IsConsuming()) {
        //     Transform tr = GetComponentInChildren<CanvasScaler>().transform;
        //     tr.localPosition = new Vector3(tr.localPosition.x, 255, tr.localPosition.z);
        // }
        
        anchorController.AudioExist(true);
    }


    /// <summary>
    /// Set the values of the UI elements from the activityInfo.
    /// </summary>
    private void ApplyDataToEditPanel() {
        var activityInfo = (AudioActivityInfo) this.activityInfo;
        
        captionInput = activityInfo.Title;
        titleInputPlaceholder.text = activityInfo.Title;
        titleText.text = activityInfo.Title;
        
        FontSizeControl(titleText.gameObject, titleText, titleText.text);
        
        HideCaptionSpace(String.IsNullOrWhiteSpace(titleText.text));

        if(!RecordManager.Instance.oldContents.ContainsKey(activityInfo.Id)) {
            // pathFromURL = true; // <-- enable if we use url from stream server
            audioVP.url = activityInfo.ContentURL;
            print($"ApplyDataToEditPanel: path = URL, activityInfo = {activityInfo}");
        }
        else {
            pathFromURL = false;
            audioVP.url = RecordManager.Instance.PathWithNewGUID(RecordManager.Instance.oldContents[activityInfo.Id]);
            print($"ApplyDataToEditPanel: path = local, activityInfo = {activityInfo}");
        }

        captionInputObject.GetComponent<InputField>().text = captionInput;
        captionInputObject.GetComponent<InputField>().placeholder.GetComponent<Text>().text = captionInput;
    }

    public override void OnSave() {
        
        if (!saveEditButton.interactable) {
            return;
        }
        print($"AudioActivity OnSave");
        
        audioVP.loopPointReached -= RecordManager.Instance.ChangeClip;
        audioVP.Stop();

        // if (RecordManager.Instance.audioPath != null) {
            print($"OnSave 1 - Memory used={System.GC.GetTotalMemory(true)}");
            base.OnSave();
            print($"OnSave 2 - Memory used={System.GC.GetTotalMemory(true)}");
            StartSubmitAudio();
            print($"OnSave 3 - Memory used={System.GC.GetTotalMemory(true)}");
        // }
        
        // if(RecordManager.Instance.audioSegmentPaths.Count != 0) {
        //     print($"1 - Memory used={System.GC.GetTotalMemory(true)}");
        //     // RecordManager.Instance.StartMergeAudioFile();
        //     print($"2 - Memory used={System.GC.GetTotalMemory(true)}");
        //     base.OnSave(); //this has to be called before the action for submitting data  
        // }
        // else {
        //     base.OnSave();
        //     print($"3 - Memory used={System.GC.GetTotalMemory(true)}");
        //     StartSubmitAudio();
        //     print($"4 - Memory used={System.GC.GetTotalMemory(true)}");
        // }
        // SetUIMode(UIMode.Saving);
        
        //save
        // base.OnSave(); //this has to be called before the action for submitting data       
    }

    protected override void ApplyDataToPreviewPlayPanel()
    {
        ApplyDataToEditPanel();
        ApplyDataToUI();
    }

    public override void OnSuccessfulSave()
    {        
        print("AudioActivity: OnSuccessfulSave");
        SBContextManager.Instance.context.UploadedMedia += 1;
        // Todo Show play panel
        playAudioPanel.SetActive(true);
        RecordManager.Instance.isNewAudio = false;
        base.OnSuccessfulSave();
        var activityInfo = (AudioActivityInfo) this.activityInfo;

        print($"OnSuccessfulSave 1 - Memory used={System.GC.GetTotalMemory(true)}");
        
        // audioVP.url = RecordManager.Instance.audioPath;
        audioVP.url = activityInfo.ContentURL;
        audioVP.isLooping = false;

        if (!RecordManager.Instance.oldContents.ContainsKey(activityInfo.Id)) {
            print($"AudioActivity: oldContents does NOT Contain activity id({activityInfo.Id})");
            RecordManager.Instance.oldContents.Add(activityInfo.Id, RecordManager.Instance.audioPath);
        }
        else {
            print($"AudioActivity: oldContents Contain activity id({activityInfo.Id})");
            RecordManager.Instance.oldContents.Remove(activityInfo.Id);
            RecordManager.Instance.oldContents.Add(activityInfo.Id, RecordManager.Instance.audioPath);
        }
        RecordManager.Instance.SaveContentPath();
        print($"-=- AudioActivity OnSuccessfulSave() = {audioVP.url}");
        
        RecordManager.Instance.SetPanel(this, false);
        // EnableSaveButton(false);
        
        HideCaptionSpace(String.IsNullOrWhiteSpace(titleText.text));
        print($"OnSuccessfulSave 2 - Memory used={System.GC.GetTotalMemory(true)}");
    }

    /// <summary>
    /// Post-specific processing when the API call fails.
    /// </summary>
    public override void OnFailedSave(ErrorInfo error)
    {
        print($"AudioActivity.OnFailedSave");
        base.OnFailedSave(error);
    }

    public override void OnSuccessfulConsume()
    {
        if (SBContextManager.Instance.context.IsConsuming())
        {
            ActivityCompleteSuccess();            
        }
    }

    public override void OnCancelCreate()
    {
        base.OnCancelCreate();
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        OnPauseButton();
        audioVP.loopPointReached -= RecordManager.Instance.ChangeClip;
        ShowWaitAudioClipAlert(false);
    }

    public override void OnEdit()
    {        
        print($"OnEdit: isEverSaved={isEverSaved}, activityInfo.Id={activityInfo.Id}");
        uiValues = ((AudioActivityInfo)activityInfo).Clone();
        print($"OnEdit: id={uiValues.Id}");
        
        base.OnEdit();  
        
        HideCaptionSpace(false);
        EnableEditSoundButton(true);
        EnableSaveButton(false);
    }

    public override void OnCancelEdit()
    {
        print($"AudioActivity OnCancelEdit");
        RecordManager.Instance.SetPanel(this, false);
        // There's no need to restore from any instance
        // as we are only updating the "activityInfo"
        // when we are saving the data.
        ApplyDataToUI();
        ApplyDataToEditPanel();
        base.OnCancelEdit();
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        OnPauseButton();
        audioVP.loopPointReached -= RecordManager.Instance.ChangeClip;
        EnableEditSoundButton(false);
        ShowWaitAudioClipAlert(false);
        RecordManager.Instance.audioPath = null;
    }

    public void OnSoundEdit() {
        UIManager.Instance.SetUIMode(UIManager.UIMode.Audio);
        RecordManager.Instance.SetPanel(this, true);
        OffScreenIndicatorManager.Instance.HideArrow();
    }
    
    void StartSubmitAudio()
    {
        var uiValues = (AudioActivityInfo)this.uiValues;      
        var experienceId = SBContextManager.Instance.context.experienceId;
        
        var refreshPolicy = SBContextManager.Instance.context.UploadedMedia < 1;

        if (!RecordManager.Instance.isNewAudio && !SBContextManager.Instance.context.isOffline)
        {
            print("StartSubmitAudio() !RecordManager.Instance.isNewAudio");
            SubmitAudio(uiValues.Title, uiValues.ContentURL);
            return;
        }

        if (SBContextManager.Instance.context.isOffline) {
            print("StartSubmitAudio() SBContextManager.Instance.context.isOffline");
            SubmitAudio(uiValues.Title, "");
            return;
        }

        // Upload the audio to our blob server.                
        this.StartThrowingCoroutine(SBRestClient.Instance.GetExperienceContainerUrlIEnumerator(experienceId, refreshPolicy, OnSasUrlReceived, OnSasUrlError)
            , e =>
            {
                OnSasUrlError(ErrorInfo.CreateNetworkError());
            });
    }

    void OnSasUrlReceived(string sasURL)
    {
        var uiValues = (AudioActivityInfo)this.uiValues;                
        this.StartThrowingCoroutine(SBRestClient.Instance.UploadBlobIEnumerator(sasURL, Guid.NewGuid().ToString(), uiValues.Title, AssetType.Audio, ContinueSubmitAudio, ContinueOnError), e =>
                {
                    print("ErrorHandler > callback - UploadBlobIEnumerator");
                    ContinueOnError(ErrorInfo.CreateNetworkError());
                });
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

    void ContinueSubmitAudio(string caption, string blobURL)
    {        
        if (blobURL.IsNullOrWhiteSpace())
        {
            string message = "Your content cannot be uploaded at this time. Please try again later.";
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction(message));
            BottomPanelManager.Instance.ShowMessagePanel(message);
            return;
        }
        
        if (String.IsNullOrWhiteSpace(caption)) {
            caption = " ";
        }
        //var uiValues = (AudioActivityInfo)this.uiValues;
        SubmitAudio(caption, blobURL);
    }

    void SubmitAudio(string caption, string blobURL)
    {
        //print($"SubmitAudio 1 - Memory used={System.GC.GetTotalMemory(true)}");
        print($"audioPath={RecordManager.Instance.audioPath}");
        print($"blobURL={blobURL}");
        var activityInfo = (AudioActivityInfo)uiValues;        

        activityInfo.Title = caption;
        activityInfo.ContentURL = blobURL;
        activityInfo.ContentPath = RecordManager.Instance.audioPath;

        Location anchorLocation = GetComponentInParent<AnchorController>().GetSBLocationInfo();
        var input = AudioActivityInput.CreateFrom(activityInfo,
               SBContextManager.Instance.context.experienceId,
               SBContextManager.Instance.context.collectionId,
               anchorLocation,
               SBContextManager.Instance.context.isPlanning);

        print($"AudioActivity.SubmitAudio: data={input}.");

        if (activityInfo.IsEditing)
            SBRestClient.Instance.UpdateAudio(activityInfo.Id, input);
        else
            SBRestClient.Instance.CreateAudio(activityInfo.Id, input);

        print($"SubmitAudio 2 - Memory used={System.GC.GetTotalMemory(true)}");
        // EnableSaveButton(true);
    }

    void ConsumeAudio()
    {
        if (isSaving)
            return;

        if (activityInfo.Status != ActivityStatus.New)
        {
            BottomPanelManager.Instance.UpdateMessage("You have already completed this activity.");
            return;
        }

        isSaving = true;
        print($"ConsumeAudio: completing the audio activity...");
        // We are using the model "PhotoPoiConsumeInput" for consuming an audio activity. 
        SBRestClient.Instance.ConsumeAudio(activityInfo.Id, new PhotoPoiConsumeInput
        {
            ExperienceId = SBContextManager.Instance.context.experienceId,
            ActivityId = activityInfo.Id,
        });
    }

    public void KeepCaptionText()
    {
        captionInput = captionInputObject.GetComponent<InputField>().text;
        print($"KeepCaptionText, captionInput = {captionInput}");
        titleText.text = captionInput;
        // ShowSaveButtonAsEnable();
        uiValues.Title = captionInput;
        if (!RecordManager.Instance.isRecordingAudio && activityInfo.IsEditing) {
            EnableSaveButton(true);
        }
    }
    
    public void KeepDescriptionText()
    {
        print("KeepDescriptionText");
        // --- For enable need to add descriptionInputObject
        // descriptionInput = descriptionInputObject.GetComponent<InputField>().text;
    }

    public void OnPlayButton() {
        print($"OnPlayButton 1 - Memory used={System.GC.GetTotalMemory(true)}");
        if (pathFromURL) {
            var activityInfo = (AudioActivityInfo) this.activityInfo;
            print($"-=- AudioActivity url path = {activityInfo.ContentURL}");
            RecordManager.Instance.SoundAudioByMediaPlayer(activityInfo.ContentURL, this);
        }
        else {
            print($"-=- AudioActivity OnPlayButton audioVP.url = {audioVP.url}");
            if (RecordManager.Instance.audioSegmentPaths.Count > 0) {
                print(
                    $"-=- AudioActivity RecordManager.Instance.audioSegmentPaths.Count > 0");
                audioVP.loopPointReached -= RecordManager.Instance.ChangeClip;
                audioVP.loopPointReached += RecordManager.Instance.ChangeClip;
                RecordManager.Instance.ChangeClip(audioVP);
            }
            else {
                EnableAudioVisualization();
            }

            audioVP.Play();
        }
        print($"OnPlayButton 2 - Memory used={System.GC.GetTotalMemory(true)}");
    }
    
    public void OnPauseButton() {
        if (pathFromURL) {
            RecordManager.Instance.Pause();
        }
        else {
            AudioVisualization.Instance.audioSource = null;
            audioVP.Pause();
            if (RecordManager.Instance.audioSegmentPaths.Count == 0) {
                ShowPlayButton();
            }
        }
    }
    
    public void EnableAudioVisualization() {
        audioVP.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
        audioVP.SetTargetAudioSource(0, audioSource);
        AudioVisualization.Instance.audioSource = audioSource;
    }

    void ClipOver(VideoPlayer vp) {
        OnPauseButton();
    }

    public void OnForwardButton(float moveTime) {
        audioVP.time = audioVP.time += moveTime;
    }

    public void OnReplayButton(float moveTime) {
        audioVP.time = audioVP.time -= moveTime;
    }

    // void ChangeClip(VideoPlayer calledVP) {
    //     print($"-=- AudioActivity ChangeClip, currClip = {currClip}");
    //     if (RecordManager.Instance.audioSegmentPaths.Count != 0) {
    //         calledVP.url = RecordManager.Instance.audioSegmentPaths[currClip];
    //         calledVP.Play();
    //         currClip++;
    //         if (currClip == RecordManager.Instance.audioSegmentPaths.Count) {
    //             currClip = 0;
    //         }
    //     }
    // }

    // public void ShowSaveButtonAsEnable() {
    //     print($"-=- AudioActivity ShowSaveButtonAsEnable()");
    //     EnableSaveButton();
    //     // if (RecordManager.Instance.audioSegmentPaths.Count != 0
    //     //     && !String.IsNullOrWhiteSpace(captionInput)) {
    //     // saveTitleButton.enabled = true;
    //     // saveTitleButton.gameObject.GetComponent<Image>().color = activeColor;
    //     // }
    // }

    void OnAudioMergeCompleted(string audioPath, string activityId)
    {
        // Let's make sure that the callback is for this activity.
        if (activityId != GetActivityId())
        {
            // ToDo: log this instance and notify ourselves.
            print($"AudioActivity, activityId != GetActivityId()");
            return;
        }

        audioVP.url = audioPath;

        print($"Audio merging is completed.");
        // StartSubmitAudio();
        
        // --- Temporary solution
        // base.OnSave();
        EnableSaveButton(true);
    }
    
    // ---------------- Consume -----------------

    public void CompleteActivity() {
        if (SBContextManager.Instance.context.IsConsuming()) {
            CompleteAudioActivity();
        }
    }
    
    void AudioComplete(VideoPlayer vp) {
        ShowPlayButton();
        CompleteAudioActivity();
    }

    void CompleteAudioActivity()
    {
        print($"CompleteAudioActivity");
        if (SBContextManager.Instance.context.IsConsuming())
        {
            ConsumeAudio();
            return;
        }
    }

    public void ActivityCompleteSuccess() {
        print($"-=- ActivityCompleteSuccess");
        rootForRotateEffect.transform.DOLocalRotate(new Vector3(0, 360, 0), 0.75f, RotateMode.FastBeyond360).OnComplete(
            () => {
                outline.SetActive(true);
                if (GetComponentInParent<ActivityManager>().ActivitiesCompleted) {
                    print("-=- AudioActivity, ActivitiesCompleted");
                    MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
                    ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);
                }
                else {
                    OffScreenIndicatorManager.Instance.ShowArrow();
                }
            });
    }

    public void EnableEditSoundButton(bool enable) {
        editSoundButton.SetActive(enable);
    }

    public void ShowPlayButton() {
            print($"-=- AudioActivity ShowPlayButton()");
            for (int i = 0; i < playButtons.Length; i++) {
                playButtons[i].SetActive(true);
                pauseButtons[i].SetActive(false);
            }
    }

    public void HidePlayButton() {
        for (int i = 0; i < playButtons.Length; i++) {
            playButtons[i].SetActive(false);
            pauseButtons[i].SetActive(true);
        }
    }

    public void StartTrackCharacters(InputField input)
    {
        FontSizeControl(input.gameObject, input.textComponent, input.text); //update the font size according to the number of characters
        UIManager.Instance.StartTrack(input);
    }
    
    public void FinishTrackCharacters() {
        UIManager.Instance.FinishTrack();
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
    }

    public void ShowWaitAudioClipAlert(bool show) {
        waitAudioClipAlert.SetActive(show);
    }
    
    private bool captionHide;
    void HideCaptionSpace(bool hide) {
        if(captionHide == hide)
            return;
        
        captionCover.gameObject.SetActive(!hide);
        float newHeight = (captionCover.GetComponent<RectTransform>().rect.height - 22) / 2;
        RectTransform rTrOutline = outline.GetComponent<RectTransform>();
        print($"AudioActivity HideCaptionSpace, hide = {hide}, height = {newHeight}");
        
        if (hide) {
            rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height - newHeight * 2);
            rootForRotateEffect.transform.localPosition = new Vector3(rootForRotateEffect.transform.localPosition.x,
                rootForRotateEffect.transform.localPosition.y + newHeight, rootForRotateEffect.transform.localPosition.z);
        }
        else {
            rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height + newHeight * 2);
            rootForRotateEffect.transform.localPosition = new Vector3(rootForRotateEffect.transform.localPosition.x,
                rootForRotateEffect.transform.localPosition.y - newHeight, rootForRotateEffect.transform.localPosition.z);
        }
        captionHide = hide;
    }
    
}

