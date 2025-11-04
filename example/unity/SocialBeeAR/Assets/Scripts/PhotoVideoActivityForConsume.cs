using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using DG.Tweening;
using GifPlayer;
using RenderHeads.Media.AVProVideo;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PhotoVideoActivityForConsume : SBActivityForConsume
{
    bool takeLoss = false;
    bool isConsumingVideo = false;
    bool isGettingKeywords = false;
    List<string> tagsForValidation = new List<string>();

    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject waitText;
    [SerializeField] private GameObject board;
    [SerializeField] private MeshFilter boardMask;

    [SerializeField] private GameObject doneActivityButton;
    [SerializeField] private GameObject challengeCover;
    [SerializeField] private Button acceptButton;
    [SerializeField] private GameObject correctOrWrong;
    [SerializeField] private GameObject waitResult;
    [SerializeField] private GameObject lowConnectionAlert;
    [SerializeField] private Text keywordsText;
    [SerializeField] private Text title;
    [SerializeField] private Text titleInChallenge;
    // [SerializeField] private Image challengeResultInfo;
    [SerializeField] private Color correctColor;
    [SerializeField] private Color wrongColor;
    [SerializeField] private GameObject outline;
    [SerializeField] private GameObject rootForRotateEffect;
    [SerializeField] private GameObject challengeLoader;
    [SerializeField] private GameObject videoLoader;
    [SerializeField] private Slider allClipLoader;
    [SerializeField] private GameObject playPanel;
    [SerializeField] private GameObject consumeCover;
    [SerializeField] private Mesh allRoundMask;
    
    // [HideInInspector] public bool isActivePanel = false;

    // Text challengeResultInfoText;
    // private float outlineNormalHightSize = 684;
    private float outlineChallengeHightSize = 872;
    private List<string>  keywordsList;
    bool pathFromURL = false;
    private Texture2D thumbnailTex;
    private IEnumerator isAllClipLoaderTimer;
    [HideInInspector] public bool isAllClipLoaderTimerWork;
    Mesh twoRoundMask;
    private IEnumerator lowConnectionAlertCorountine;
    
    private void OnEnable() {
        VideoPlayer vp = board.GetComponent<VideoPlayer>();
        vp.loopPointReached += VideoComplete;
    }

    void OnDisable() {
        VideoPlayer vp = board.GetComponent<VideoPlayer>();
        vp.loopPointReached -= VideoComplete;
    }

    public override void Reborn(IActivityInfo activityInfo)
    {
        print($"PhotoVideoActivityForConsume.Reborn - activityInfo = {(activityInfo == null ? "NULL" : activityInfo.ToString())}");

        activityInfo.IsEditing = false;
        base.Reborn(activityInfo);
       
        //parse photo/video URL, retrieve and display it.
        ApplyDataToUI();
    }

    /// <summary>
    /// Use the activityInfo here.
    /// </summary>
    protected override void ApplyDataToUI()
    {
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;

        string path;
        print($"activityInfo.Id = {activityInfo.Id}");
        if (!RecordManager.Instance.oldContents.ContainsKey(activityInfo.Id)) {
            path = activityInfo.ContentURL;
            pathFromURL = true;
        }
        else if(activityInfo.IsVideo){
            // path = RecordManager.Instance.PathWithNewGUID(RecordManager.Instance.oldContents[activityInfo.Id]);
            path = RecordManager.Instance.PathWithNewGUID(activityInfo.ContentPath);
        }
        else {
            // path = RecordManager.Instance.PhotoPathWithNewGUID(RecordManager.Instance.oldContents[activityInfo.Id]);
            path = RecordManager.Instance.PhotoPathWithNewGUID(activityInfo.ContentPath);
        }

        RecordManager.Instance.filteredFilePath = path;
        RecordManager.Instance.contentPublicPath = activityInfo.ContentURL;
        if(activityInfo.IsVideo) {
            RecordManager.Instance.videoPath = path;
            ShowVideo();
        }

        else {
            // RecordManager.Instance.photoPath = activityInfo.ContentPath;
            RecordManager.Instance.photoPath = path;
            print($"activityInfo.IsChallenge = {activityInfo.IsChallenge}");
            if (activityInfo.IsChallenge) {
                print($"1 setting keywords...");

                tagsForValidation = activityInfo.Keywords.Count() > 0
                    ? activityInfo.Keywords.ToList()
                    : activityInfo.AlternateKeywords.ToList();
                print($"tagsForValidation={tagsForValidation.Count}");                
                //if (tagsForValidation.Count < 0) // <-- this does NOT translate into a false when count is zero??? :O
                //{
                //    print($"setting AlternateKeywords...");
                //    tagsForValidation = activityInfo.AlternateKeywords.ToList();
                //}                
                ShowChallenge();
            }
            else {
                doneActivityButton.SetActive(true);
            }
            ShowPhoto("");
            RecordManager.Instance.isPhotoTaking = true;
            waitText.SetActive(false);
        }
        print($"activityInfo.Title = {activityInfo.Title}");

        title.text = activityInfo.Title;
        titleInChallenge.text = activityInfo.Title;
        HideCaptionSpace(String.IsNullOrWhiteSpace(title.text));

        if (activityInfo.Status == ActivityStatus.Verified)
        {
            SetPanelToCorrectStatus();
        }
        else if (activityInfo.Status != ActivityStatus.New)
        {
            SetPanelToFailedStatus();
        }
        else {
            acceptButton.gameObject.SetActive(true);
        }
    }

    void ShowChallenge() {
        print($"-=- PhotoVideoActivityForConsume ShowChallenge()");
        challengeCover.SetActive(true);
        outline.GetComponent<RectTransform>().sizeDelta = new Vector2(518, outlineChallengeHightSize);
        keywordsText.text = $"Keyword(s): {string.Join(", ", tagsForValidation)}";
    }

    void ShowVideo() {

        print($"-=- PhotoVideoActivityForConsume ShowVideo()");
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;

        //print("video taken from native camera, showing video.");
        activityInfo.IsVideo = true;

        //Todo:
        // 1. Hide the keywords label here.
        // 2. Hide the photo panel here.
        // 3. Show the video panel here.

        if (pathFromURL) {
            print($"-=- PhotoVideoActivityForConsume pathFromURL");
            StartCoroutine(LoadThumbnail());
        }
        
        RecordManager.Instance.ConfigureVideoElements(board, gameObject, false);
        RecordManager.Instance.ShowVideoOnPanel();
        // waitText.SetActive(false);
        playButton.SetActive(true);
    }

    void ShowPhoto(string contentPath)
    {
        print($"-=- PhotoVideoActivityForConsume ShowPhoto()");
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;

        //print($"photo taken from native camera, showing photo: contentPath={contentPath} | photoPath={RecordManager.Instance.photoPath} | photoPublicPath={RecordManager.Instance.photoPublicPath}.");
        activityInfo.IsVideo = false;

        //Todo:
        // 1. Show the keywords label here.
        // 2. Show the photo panel here.
        // 3. Hideo the video panel here.
        // keywordsText.gameObject.SetActive(true);

        RecordManager.Instance.ShowPhoto(board);
        // SetPreviewPlane();
        // RecordManager.Instance.SetCroppingPositionOnARPanel(board);
    }

    public void OnAcceptChallenge() {
        print($"-=- PhotoVideoActivityForConsume OnAcceptChallenge()");
        // isActivePanel = true;
        InteractionManager.Instance.currentChallengePhotoVideoActivity = this;
        UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
        RecordManager.Instance.PhotoTakeOnly();
        RecordManager.Instance.ConfigureVideoElements(board, gameObject, false);
    }

    public void OnCancelChallenge() {
        acceptButton.gameObject.SetActive(true);
        acceptButton.interactable = true;
        // isActivePanel = false;
    }

    // public void GetPhotoKeywords(string path) {
    //     print($"-=- PhotoVideoActivityForConsume GetPhotoKeywords() path = {path}");
    //     BottomPanelManager.Instance.ShowMessagePanel("Retrieving keywords for your photo, please wait...");
    //     StartCoroutine(GetImageKeywords(path));
    //     // challengeResultInfo.gameObject.SetActive(true);
    // }

    /// <summary>
    /// The method to call when consuming a photo POI or a video.
    /// </summary>
    public override void OnConsumed()
    {
        MarkCompleted();
    }

    /// <summary>
    /// The method to call when completing a photo challenge.
    /// </summary>
    public override void OnSubmit()
    {
        print($"-=- PhotoVideoActivityForConsume OnSubmit() RecordManager.Instance.filteredFilePath = {RecordManager.Instance.filteredFilePath}");
        print($"-=- isGettingKeywords = {isGettingKeywords}, isSaving = {isSaving}");

        if (isGettingKeywords || isSaving)
            return;

        if ((PhotoVideoActivityInfo) this.completedActivity == null) {
            // The empty values doesn't matter at this point.
            completedActivity = (PhotoVideoActivityInfo)activityInfo;
        }

        var completedActivity1 = (PhotoVideoActivityInfo)this.completedActivity;
        completedActivity1.ContentPath = RecordManager.Instance.filteredFilePath;

        // Ensure we have a photo to be submitted.
        if (completedActivity1.ContentPath.IsNullOrWhiteSpace())
        {            
            BottomPanelManager.Instance.ShowMessagePanel("You need to take a photo.");
            return;
        }

        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        // Get the keywords        
        // BottomPanelManager.Instance.ShowMessagePanel("Retrieving keywords for your photo, please wait...");
        StartCoroutine(GetImageKeywords());

        // This method should only be called for photo or video challenges and not for photo/video POIs.

        var activityInfo1 = (PhotoVideoActivityInfo)this.activityInfo;

        // But let's make sure the activity is not a video.
        if (activityInfo1.IsVideo)
            base.OnSubmit();
        // else we don't need to do anything else for now.
        ShowChallengeLoader(true);
    }

    /// <summary>
    /// Sending Post data to SB cloud
    /// </summary>
    bool MarkCompleted()
    {
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;

        print($"PhotoVideoForConsume.MarkCompleted: ID = {activityInfo.Id}");
        var input = PhotoPoiConsumeInput.CreateFrom(SBContextManager.Instance.context.experienceId);

        // ToDo: call API here
        //SBRestClient.Instance.ConsumePhoto(activityInfo.Id, null);

        print("Exiting MarkCompleted...");
        return true;
    }

    /// <summary>
    /// The method that shows the keywords of the taken photo
    /// that will be matched to the activity photo.    
    /// </summary>
    /// <param name="keywords">The keywords to show in a label.</param>
    public void ShowKeywords(string keywords) {
        if (!isGettingKeywords) {
            return;
        }
        isSaving = false;
        isGettingKeywords = false;
        print($"PhotoVideo > ShowKeywords = {keywords}");
        print($"accepted keywords: {string.Join(",", tagsForValidation)}");
        keywordsList = keywords.Split(',').ToList();

        var matchingKeywords = tagsForValidation.Intersect(keywordsList);
        ShowChallengeLoader(false);
        ((PhotoVideoActivityInfo)completedActivity).Keywords = keywordsList;
        if (matchingKeywords.Count() > 0)
        {
            //
            // ToDo: show the keywords on the UI.

            //
            // For now we will go easy on the validation.
            // If there is at least one keyword from the submitted photo
            // that matches the photo being consumed,
            // then the activity will be treated as successful.
            SubmitPhotoChallenge(false, keywordsList);
        }
        else
        {
            StartCoroutine(ShowInvalidPhoto());
        }
    }
     
    IEnumerator GetImageKeywords()
    {
        isGettingKeywords = true;
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        activityInfo.MarkAsAChallenge(true);
        print($"-=- activityInfo.UrlToUseForValidation = {activityInfo.UrlToUseForValidation}");

        // InteractionManager.Instance.OnWillGetImageKeywords(activityInfo.UrlToUseForValidation);
        InteractionManager.Instance.OnWillGetImageKeywords(RecordManager.Instance.filteredFilePath, true);
        yield return null;
    }

    /// <summary>
    /// The method used to show that the consumer's selected/taken photo
    /// does not match the creator's photo.
    /// </summary>
    IEnumerator  ShowInvalidPhoto()
    {
        print("FROM AR: PhotoVideo > ShowInvalidPhoto");
        // ToDo: this is a temporary code to show intent of what needs to happen next.
        // BottomPanelManager.Instance.ShowMessagePanel("Allow the user to retake the challenge.", true, true);
        // END ToDo.
        
        BottomPanelManager.Instance.HideCurrentPanel(() => { });
        yield return new WaitForSeconds(Const.PANEL_ANIMATION_TIME + 0.05f);
        
        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.WrongChallengeKeywords);
        BottomPanelManager.Instance.UpdateMessage($"Keyword(s): {string.Join(", ", keywordsList)}");
            
        
        // If there are no matching keywords then we don't need to submit to the API.
        // Show this screen: https://www.figma.com/file/Csgsqwy443R4aoEGGjJHlb/Social-Bee-Retouch?node-id=718%3A1182.
        // That allows the user to submit another photo.

        // If the user choose the "I take my loss" then call "SubmitPhotoChallenge" with param=true
        //SubmitPhotoChallenge(true);

        // If the user chooses "Retake" then do nothing here
        // and follow the design to allow user to retake a photo.
    }

    void UploadPhoto()
    {
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        var completedActivity = (PhotoVideoActivityInfo)this.completedActivity;

        // ToDo: This is the caption of the photo being submitted. Get this from the UI element.
        completedActivity.Title = title.text;
        completedActivity.ContentPath = RenamedFile(RecordManager.Instance.photoPath);
        RecordManager.Instance.photoPath = completedActivity.ContentPath;
        print(
            $"PhotoVideoActivityForConsume.UploadPhoto: ID = {activityInfo.Id}");

        var refreshPolicy = SBContextManager.Instance.context.UploadedMedia < 1;
        var experienceId = SBContextManager.Instance.context.experienceId;

        //print("PhotoVideoActivity - starting upload...");
        //
        // Upload the photo or video first to our blob server.
        // The Id is the combination of the acti
        var blobId = activityInfo.GetBlobIdForUpload();
        
        StartCoroutine(SBRestClient.Instance.GetExperienceContainerUrlIEnumerator(experienceId, refreshPolicy, OnSasUrlReceived, OnSasUrlError));
    }

    void OnSasUrlReceived(string sasURL)
    {
        StartCoroutine(SBRestClient.Instance.UploadBlobIEnumerator(sasURL, Guid.NewGuid().ToString(), title.text, AssetType.Photo, ContinueSubmitPhoto, ContinueOnError));
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
        ShowChallengeLoader(false);
    }

    void ContinueSubmitPhoto(string caption, string blobURL)
    {
        //print($"PhotoVideoActivityForConsume.ContinueSubmitPhoto: blobURL={blobURL}");

        if (blobURL.IsNullOrWhiteSpace())
        {
            //StartCoroutine(BottomPanelManager.Instance.ShowAlertWithoutAction("Your content cannot be uploaded at this time. Please try again later."));
            BottomPanelManager.Instance.ShowMessagePanel("Your content cannot be uploaded at this time. Please try again later.");
            ShowChallengeLoader(false);
            return;
        }
        
        SubmitPhotoChallenge(takeLoss, ((PhotoVideoActivityInfo)activityInfo).Keywords);
        RecordManager.Instance.ShowPhoto(board);
    }
     
    public void OnWrongPress() {

    }

    void VideoComplete(VideoPlayer vp) {
        CompleteActivity();
        playButton.SetActive(true);
        pauseButton.SetActive(false);
        StopAllClipLoader(true);
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

    public void CompleteActivity() {
        print($"-=- CompleteActivity");
        if (isConsumingVideo)
        {
            SubmitVideoPOI();
            return;
        }

        // We are completing a photo activity
        if (activityInfo.IsChallenge)
        {
            print("not yet supported");
            return;
        }

        SubmitPhotoPOI();
    }

    public void SubmitPhotoChallenge(bool takeLoss) {
        print($"SubmitPhotoChallenge, takeLoss button pressed");
        //SubmitPhotoChallenge(takeLoss, keywordsList);
        this.takeLoss = takeLoss;
        UploadPhoto();
        ShowChallengeLoader(true);
    }
    
    void SubmitPhotoChallenge(bool takeLoss, IEnumerable<string> keywords)
    {
        print("SubmitPhotoChallenge");
        if (isSaving)
            return;

        // isActivePanel = false;
        //
        // ToDo: comment this block to test retries of submitting images. #tempcode #debugging
        if (activityInfo.Status != ActivityStatus.New)
        {
            BottomPanelManager.Instance.UpdateMessage("You have already completed this activity.");
            return;
        }
        // End - comment this block.
        
        isSaving = true;
        print($"SubmitPhotoChallenge: completing the photo challenge...");
        SBRestClient.Instance.CompletePhotoChallenge(activityInfo.Id, new PhotoChallengeConsumeInput
        {
            ExperienceId = SBContextManager.Instance.context.experienceId,
            ActivityId = activityInfo.Id,
            // Show the optional caption here.
            Caption = "", 
            TakeLoss = takeLoss,
            // The photo should have already been uploaded to our blob storage.
            // The filename uploaded should be in this format: {activityId}{userId}.{extension}
            //Extension = ".jpeg",
            Keywords = keywords,            
        }, takeLoss ? ActivityStatus.Undefined : ActivityStatus.Verified);
        if (takeLoss) {
            SetPanelToFailedStatus();
        }
        else {
            SetPanelToCorrectStatus();
        }     
        
        ShowChallengeLoader(false);
    }

    void SubmitPhotoPOI()
    {
        if (isSaving)
            return;

        //
        // ToDo: comment this block to test retries of submitting images. #tempcode #debugging
        if (activityInfo.Status != ActivityStatus.New)
        {
            BottomPanelManager.Instance.UpdateMessage("You have already completed this activity.");
            return;
        }
        // End - comment this block.

        isSaving = true;
        print($"SubmitPhotoPOI: completing the photo POI...");
        SBRestClient.Instance.ConsumePhoto(activityInfo.Id, new PhotoPoiConsumeInput
        {
            ExperienceId = SBContextManager.Instance.context.experienceId
        });
    }

    void SubmitVideoPOI()
    {
        if (isSaving)
            return;
        
        if (activityInfo.Status != ActivityStatus.New)
        {
            BottomPanelManager.Instance.UpdateMessage("You have already completed this activity.");
            return;
        }

        isSaving = true;
        print($"SubmitVideoPOI: completing the video POI...");
        // We are using the model "PhotoPoiConsumeInput" for consuming a video POI. 
        SBRestClient.Instance.ConsumeVideo(activityInfo.Id, new PhotoPoiConsumeInput
        {
            ExperienceId = SBContextManager.Instance.context.experienceId
        });
    }

    void OnActivitySuccess()
    {
        //print($"-=- OnActivitySuccess: activity status = {activityInfo.Status}");
        isSaving = false;        
        //print($"-=-1 OnActivitySuccess");
        outline.SetActive(true);
        //print($"-=-2 OnActivitySuccess");
        //
        // ToDo: @Dmitry, "rootForRotateEffect" is null at this point after the API call is done.
        rootForRotateEffect.transform.DOLocalRotate(new Vector3(0, 360, 0), 0.75f, RotateMode.FastBeyond360).OnComplete(() => {
            SetPanelToCorrectStatus();
            
            if(GetComponentInParent<ActivityManager>().ActivitiesCompleted) {
                print("-=- PhotoVideoForConsume, ActivitiesCompleted");
                MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);                
            }
            else {
                OffScreenIndicatorManager.Instance.ShowArrow();
            }
        });

    }

    void OnActivityFailed() {        
        print($"-=- OnActivityFailed");
        SetPanelToFailedStatus();
        
        if(GetComponentInParent<ActivityManager>().ActivitiesCompleted) {
            print("-=- PhotoVideoForConsume, ActivitiesCompleted");
            MiniMapManager.Instance.SetGreenPoint(GetComponentInParent<AnchorController>().GetAnchorInfo().id);
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.AllActivitiesComplete);
        }
    }

    void SetPanelToFailedStatus()
    {
        print($"SetPanelToFailedStatus");
        isSaving = false;
        outline.SetActive(true);
        outline.GetComponent<Image>().color = wrongColor;
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        if (activityInfo.IsChallenge)
        {
            waitResult.SetActive(false);
            float a = wrongColor.a;
            wrongColor.a = 1;
            correctOrWrong.GetComponent<Button>().enabled = false;
            correctOrWrong.GetComponent<Image>().color = wrongColor;
            wrongColor.a = a;
            correctOrWrong.GetComponentInChildren<Text>().text = "Wrong";
        }
    }

    void SetPanelToCorrectStatus()
    {
        //print($"-=-1 SetPanelCorrectStatus");
        outline.SetActive(true);
        //print($"-=-2 SetPanelCorrectStatus");
        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        //print($"-=-3 SetPanelCorrectStatus");
        if (activityInfo.IsChallenge)
        {
            //print($"-=-4 SetPanelCorrectStatus");
            waitResult.SetActive(false);
            float a = correctColor.a;
            correctColor.a = 1;
            correctOrWrong.GetComponent<Button>().enabled = false;
            correctOrWrong.GetComponent<Image>().color = correctColor;
            correctColor.a = a;
            correctOrWrong.GetComponentInChildren<Text>().text = "Correct";
            
            // --- Show new photo on panel
            RecordManager.Instance.ShowPhoto(board);
            
            //print($"-=-10 SetPanelCorrectStatus");
        }
    }

    public void Fullscreen() {
        print($"-=- ActivityForConsume Fullscreen");
        var activityInfo = (PhotoVideoActivityInfo) this.activityInfo;
        VideoPlayer vp = board.GetComponent<VideoPlayer>();
        RecordManager.Instance.Fullscreen(vp, activityInfo.IsVideo, pathFromURL);
    }

    public void OnPlayButton() {
        playButton.SetActive(false);
        pauseButton.SetActive(true);
        print($"-=- ActivityForConsume OnPlayButton()");
        if(pathFromURL) {
            var activityInfo = (PhotoVideoActivityInfo) this.activityInfo;
            print($"-=- ActivityForConsume activityInfo = {activityInfo.ContentURL}");
            // RecordManager.Instance.ShowVideoByMediaPlayer(testPlane.GetComponent<Renderer>(), activityInfo.ContentURL);
            RecordManager.Instance.ShowVideoByMediaPlayer(board.GetComponent<Renderer>(),
                activityInfo.ContentURL, this, null,false,
                board.GetComponent<Renderer>().material.GetTexture("_Albedo"));
        }
        else {
            board.GetComponent<VideoPlayer>().Play();
            SetAllClipLoader((float)board.GetComponent<VideoPlayer>().length);
            PlayAllClipLoader();
        }
    }

    public void CorrectBoardScaleByVideoSize(MediaPlayer mp) {
        float width = (float)mp.Info.GetVideoWidth();
        float height = (float)mp.Info.GetVideoHeight();
        
        //Landscape
        if (height / width < 1) {
            board.transform.localScale = new Vector3(width / height * board.transform.localScale.x,
                board.transform.localScale.y, board.transform.localScale.z);
        }
        else {
            board.transform.localScale = new Vector3(board.transform.localScale.x,
                height / width * board.transform.localScale.y, board.transform.localScale.z);
        }
        print($"-=- ActivityForConsume CorrectBoardScaleByVideoSize, board.transform.localScale = {board.transform.localScale}");
    }

    public void OnPauseButton() {
        pauseButton.SetActive(false);
        playButton.SetActive(true);
        print($"-=- ActivityForConsume OnPauseButton()");
        if (pathFromURL) {
            RecordManager.Instance.Pause();
        }
        else {
            board.GetComponent<VideoPlayer>().Pause();
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
    
    public override void OnSuccessfulSave()
    {
        print("PhotoVideo was successfully completed.");
        if (completedActivity.Status == ActivityStatus.Verified)
            OnActivitySuccess();
        else 
            OnActivityFailed();
    }

    public override void OnFailedSave(ErrorInfo error)
    {
        print("PhotoVideo was NOT successfully completed.");
        // Do not call OnActivityFailed() here because this method is called when the API call failed.
        base.OnFailedSave(error);
    }
    
    public void ShowChallengeLoader(bool visible) {
        if (challengeLoader == null) {
            return;
        }

        UnityGif uGif = challengeLoader.GetComponentInChildren<UnityGif>();
        if (visible) {
            challengeLoader.SetActive(true);
            uGif?.Play();
            waitResult.SetActive(true);
            acceptButton.gameObject.SetActive(false);
            if(lowConnectionAlertCorountine != null) {
                StopCoroutine(lowConnectionAlertCorountine);
            }
            lowConnectionAlertCorountine = LowConnectionAlert();
            StartCoroutine(lowConnectionAlertCorountine);
        }
        else {
            uGif?.Pause();
            challengeLoader.SetActive(false);
            waitResult.SetActive(false);
            StopCoroutine(lowConnectionAlertCorountine);
            lowConnectionAlert.SetActive(false);
        }
        print($"ShowChallengeLoader visible = {visible}");
    }

    IEnumerator LowConnectionAlert() {
        print($"LowConnectionAlert() start");
        yield return new WaitForSeconds(13f);
        if(isGettingKeywords) {
            lowConnectionAlert.SetActive(true);
        }
    }

    public void BreakChallengeOnGettingKeywords() {
        isGettingKeywords = false;
        ShowChallengeLoader(false);
        OnCancelChallenge();
    }
    
    public void ShowThumbnail(Texture2D tex) {
        print($"-=- PhotoVideoActivityForConsume ShowThumbnail");
        board.GetComponent<MeshRenderer >().material.SetTexture("_Albedo", tex);
        RecordManager.Instance.ConfigureVideoElements(board, gameObject, false);
        RecordManager.Instance.SetARPanelByContentSize(board, tex.width, tex.height);
    }
    
    string RenamedFile(string path) {
        // The filename uploaded should be in this format: {activityId}{userId}.{extension}
        // --- move file
        try {
            string activityId = this.GetComponent<SBActivityForConsume>().GetActivityId();
            string newPath = Application.persistentDataPath + "/" + activityId +
                             SBContextManager.Instance.context.userId + ".jpg";
            print($"-=- newPath = {newPath}");
            if (File.Exists(newPath)) {
                File.Delete(newPath);
            }
            File.Move(path, newPath);
            return newPath;
        }
        catch(Exception ex)
        {
            // ToDo: log this error and notify ourselves.
            print($"PhotoVideoActivityForConsume - Renamed File failed: {ex.Message}");
            return path;
        }
    }

    
    public IEnumerator LoadThumbnail(bool wait = false) {
        if (wait) {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (thumbnailTex != null) {
            board.GetComponent<Renderer>().material.SetTexture("_Albedo", thumbnailTex);
        }
        print($"PhotoVideoActivityForConsume IEnumerator LoadThumbnail");

        var activityInfo = (PhotoVideoActivityInfo)this.activityInfo;
        thumbnailTex = new Texture2D(1, 1);

        WWW www = new WWW(activityInfo.Thumbnail);
        yield return www;
        
        www.LoadImageIntoTexture(thumbnailTex);
        board.GetComponent<Renderer>().material.SetTexture("_Albedo", thumbnailTex);
        www.Dispose();
        www = null;
    }

    private bool captionHide = false;
    void HideCaptionSpace(bool hide) {
        if(captionHide == hide)
            return;
        
        challengeCover.SetActive(activityInfo.IsChallenge);
        RectTransform rTrOutline = outline.GetComponent<RectTransform>();
        
        float newHeight = consumeCover.GetComponent<RectTransform>().rect.height / 2;
        if(activityInfo.IsChallenge) { 
            newHeight = (256 - 168) / 2;
        }
        
        print($"PhotoVideoActivityForConsume HideCaptionSpace, hide = {hide}, newHeight = {newHeight}");
        
        if (hide) {
            twoRoundMask = boardMask.mesh;
            //hide challenge panel
            challengeCover.GetComponent<RectTransform>().sizeDelta = new Vector2(486, 110);
            
            //hide title cover
            consumeCover.SetActive(false);
            if(!activityInfo.IsChallenge) {
                boardMask.mesh = allRoundMask;
                rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height - newHeight * 2);
            }
            else {
                rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height - 146);
            }
            
            // playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x,playPanel.transform.localPosition.y - newHeight, playPanel.transform.localPosition.z);
            rootForRotateEffect.transform.localPosition = new Vector3(rootForRotateEffect.transform.localPosition.x,rootForRotateEffect.transform.localPosition.y - newHeight, rootForRotateEffect.transform.localPosition.z);
        }
        else {
            challengeCover.GetComponent<RectTransform>().sizeDelta = new Vector2(486, 256);
            consumeCover.SetActive(true);
            boardMask.mesh = twoRoundMask;
            
            if(!activityInfo.IsChallenge) {
                rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height + newHeight * 2);
            }
            else {
                rTrOutline.sizeDelta = new Vector2(518, rTrOutline.rect.height + 146);
            }
            
            // playPanel.transform.localPosition = new Vector3(playPanel.transform.localPosition.x, playPanel.transform.localPosition.y + newHeight, playPanel.transform.localPosition.z);
            rootForRotateEffect.transform.localPosition = new Vector3(rootForRotateEffect.transform.localPosition.x,rootForRotateEffect.transform.localPosition.y + newHeight, rootForRotateEffect.transform.localPosition.z);
        }
        captionHide = hide;
    }
    
}
