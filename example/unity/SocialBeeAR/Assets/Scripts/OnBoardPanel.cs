using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using GifPlayer;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class OnBoardPanel : MonoBehaviour
{
    public ActivityType activityType;
    
    [SerializeField] private GameObject outline;
    [SerializeField] private GameObject rootForRotateEffect;
    public Transform beePos;
    public GameObject fade;
    public Transform arrowTrigger;
    
    [Header(" ------ For Video ------")][Space(25)]
    [SerializeField] private GameObject challengeLoader;
    [SerializeField] private GameObject videoLoader;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject board;
    [SerializeField] private Text text;
    
    [Header(" ------ For Likes ------")][Space(25)]
    [SerializeField] Button submitButton;
    [SerializeField] private Color rightColor;
    [Header("(Titles on the panel will be sent to the native) (size = 12) !")]
    public OptionContent[] optionContents;

    [Header(" ------ For Challenge ------")][Space(25)]
    [SerializeField] private GameObject acceptButton;
    [SerializeField] private Color waitColor;
    [SerializeField] private Text keyWordText;
    [SerializeField] private Material photoPlaneMat;
    [SerializeField] private GameObject beeBotProfilePicture;
    [SerializeField] private Color correctColor;
    [SerializeField] private Color wrongColor;
    private bool isCorrectPhoto = false;
    
    private bool isActivityComplete;
    Texture2D thumbnailTex;
    private CanvasGroup canvasGroup;
    
    public enum ActivityType {
        Video,
        Challenge,
        Likes
    }
    

    private void Start() {
        canvasGroup = GetComponent<CanvasGroup>();
        if(activityType == ActivityType.Likes) {
            OnBoardManager.Instance.likesPanel = this;
            SetItems();
            EnableButtons(false);
        }
        if(activityType == ActivityType.Video) {
            RecordManager.Instance.onBoard = this;
            RecordManager.Instance.videoPath = OnBoardManager.Instance.videoPath;   
            
            // this.StartThrowingCoroutine(LoadThumbnail(), e =>
            // {
            //     print($"LoadThumbnail Error: {e.StackTrace}");
            //     BottomPanelManager.Instance.ShowMessagePanel("The OnBoarding cannot proceed due to the missing thumbnail.  challenge cannot be downloaded at this time. Please try again later.");
            // });
            // RecordManager.Instance.SetCroppingPositionOnARPanel(board);
            OnBoardManager.Instance.videoPanel = this;
            OnBoardManager.Instance.arrowTrigger = arrowTrigger;
            OnBoardManager.Instance.TextOnVideoPanel(text);
            EnableButtons(true);
            Invoke("OnPlayButton", 0.4f);
        }
        if(activityType == ActivityType.Challenge) {
            RecordManager.Instance.onBoardChallenge = this;
            RecordManager.Instance.SetCroppingPositionOnARPanel(board);
            OnBoardManager.Instance.challengePanel = this;
            this.StartThrowingCoroutine(DownloadPhoto(), e =>
            {
                BottomPanelManager.Instance.ShowMessagePanel("The photo challenge cannot be downloaded at this time. Please try again later.");
            });
            EnableButtons(false);
        }
    }
    
    
    #region Video
    
    
    public void Fullscreen() {
        Debug.Log($"-=- OnBoardPanel Fullscreen");
        // if (playButton.activeSelf) {
        //     OnPlayButton();
        // }
        // VideoPlayer vp = board.GetComponent<VideoPlayer>();
        RecordManager.Instance.Fullscreen(null, true, true, 0);
    }

    public void OnPlayButton() {
        playButton.SetActive(false);
        pauseButton.SetActive(true);
        videoLoader.SetActive(true);
        Debug.Log($"-=- OnBoardPanel OnPlayButton()");

        RecordManager.Instance.PlayIntro(board.GetComponent<Renderer>());
        
            // RecordManager.Instance.ShowVideoByMediaPlayer(board.GetComponent<Renderer>(),
            //     OnBoardManager.Instance.videoPath, null, null,false,
            //     board.GetComponent<Renderer>().material.GetTexture("_Albedo"));
    }

    public void OnPauseButton() {
        pauseButton.SetActive(false);
        playButton.SetActive(true);
        Debug.Log($"-=- OnBoardPanel OnPauseButton()");
        RecordManager.Instance.Pause();
    }

    
    public void ShowPlayButton() {
        pauseButton.SetActive(false);
        playButton.SetActive(true);
    }

    public void EnablePlayButton(bool enable) {
        pauseButton.GetComponent<Button>().interactable = enable;
        playButton.GetComponent<Button>().interactable = enable;
    }

    public void ShowVideoLoader() {
        Debug.Log($"-=- OnBoardPanel ShowVideoLoader()");
        videoLoader.SetActive(true);
    }   
    
    public void HideVideoLoader() {
        Debug.Log($"-=- OnBoardPanel HideVideoLoader()");
        videoLoader.SetActive(false);
    }

    IEnumerator LoadThumbnail() {
        if(!String.IsNullOrWhiteSpace(OnBoardManager.Instance.thumbnailPath)) {
            if (thumbnailTex != null) {
                board.GetComponent<Renderer>().material.SetTexture("_Albedo", thumbnailTex);
            }

            Debug.Log($"PhotoVideoActivityForConsume IEnumerator LoadThumbnail");

            thumbnailTex = new Texture2D(1, 1);

            WWW www = new WWW(OnBoardManager.Instance.thumbnailPath);
            yield return www;

            www.LoadImageIntoTexture(thumbnailTex);
            board.GetComponent<Renderer>().material.SetTexture("_Albedo", thumbnailTex);
            www.Dispose();
            www = null;
        }
    }


    #endregion

    #region Options

    private int selectedOptions;
    [HideInInspector] public string[] selectedNames = new string[12];
    
    public void OnPressOption(Toggle toggle, string name) {
        if(!isActivityComplete) {
            bool isOn = toggle.isOn;
            Debug.Log($"-=- OnPressOption isOn = {isOn}, name = {name}");
            if (isOn) {
                selectedNames[selectedOptions] = name;
                selectedOptions++;
            }
            else {
                selectedOptions--;
                for (int i = 0; i < 12; i++) {
                    if (selectedNames[i] == name) {
                        selectedNames[i] = "";
                    }
                }
            }

            if (selectedOptions >= 1) {
                submitButton.interactable = true;
            }
            else {
                submitButton.interactable = false;
            }
        }
    }
    
    void SetItems() {
        foreach (var option in optionContents) {
            Image icon = option.objectInScene.GetComponentInChildren<Image>();
            Text title = option.objectInScene.GetComponentInChildren<Text>();
            icon.sprite = option.sprite;
            title.text = option.title;

            Toggle toggle = option.objectInScene.GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(delegate { OnPressOption(toggle, option.title); });
        }
    }


    #endregion

    #region Challenge

    private string blobURL;
    List<string> imageTags = new List<string>();
    
    // --- Start Photo Taken
    public void OnAcceptChallenge() {
        Debug.Log($"-=- OnBoardPanel AcceptChallenge()");
        OffScreenIndicatorManager.Instance.HideArrow();
        UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
        RecordManager.Instance.PhotoTakeOnly();
        RecordManager.Instance.ChangeCameraButton();
        // RecordManager.Instance.ConfigureVideoElements(board, gameObject, false);
        RecordManager.Instance.onBoard = null;
        beeBotProfilePicture.SetActive(false);
        board.SetActive(false);
        RecordManager.Instance.StartCheckingMicrophonPermissions();
        // GetPhoto(); // --- test in editor
    }

    // [HideInInspector] public string caption;
    // public void OnGetCaption(string cap) {
    //     caption = cap;
    // }

    public void ShowPhoto() {
        board.SetActive(true);
        RecordManager.Instance.ShowPhoto(board);
        GetPhoto();
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        OffScreenIndicatorManager.Instance.ShowArrow();
    }
    
    void GetPhoto() {
        Debug.Log($"-=- OnBoardPanel GetPhoto()");
        
        var path = RecordManager.Instance.photoPath;
        
        StartCoroutine(SentImageKeywords(path));        
        ShowChallengeLoader(true);
        
        acceptButton.GetComponent<Button>().enabled = false;
        acceptButton.GetComponent<Image>().color = waitColor;
        acceptButton.GetComponentInChildren<Text>().text = "Accepted";
    }

    public void ShowKeywords(string keywords) {   
        OffScreenIndicatorManager.Instance.HideArrow();
        imageTags = new List<string>();
        imageTags = keywords.Split(',').Take(10).ToList();
        string keyWords = "Keyword(s): ";
        for (int i = 0; i < imageTags.Count; i++) {
            if(i != 0) {
                keyWords += ", ";
            }
            keyWords += imageTags[i];
        }
        print($"-=- ShowKeywords imageTags.Count = {imageTags.Count}");
        this.StartThrowingCoroutine(WaitSubmitAndKeywords(), e =>
        {
            print($"WaitSubmitAndKeywords Error: {e.StackTrace}");
        });
        
        keyWordText.text = keyWords;
    }

    IEnumerator SentImageKeywords(string path) {
        print($"-=- GetImageKeywords path = {path}");
        InteractionManager.Instance.OnWillGetImageKeywords(path, true);
        
        //CompleteActivity(); // --- for test
        yield return null;
    }
    
    public void SubmitPhoto()
    {
        print($"OnBoardPanel SubmitPhoto");

        var refreshPolicy = SBContextManager.Instance.context.UploadedMedia < 1;
        var experienceId = SBContextManager.Instance.context.experienceId;
        //var assetType =  AssetType.Photo;

        this.StartThrowingCoroutine(SBRestClient.Instance.GetExperienceContainerUrlIEnumerator(experienceId, refreshPolicy, OnSasUrlReceived, OnSasUrlError),
            e =>
            {
                ContinueOnError(ErrorInfo.CreateNetworkError());
            });
        RecordManager.Instance.onBoardChallenge = null;
        OnBoardManager.Instance.ToAskingToShare();
    }

    void OnSasUrlReceived(string sasURL)
    {
        this.StartThrowingCoroutine(SBRestClient.Instance.UploadBlobIEnumerator(sasURL, Guid.NewGuid().ToString(), "My Profile Picture", AssetType.Photo, ContinueSubmitPhotoVideo, ContinueOnError),
            e =>
            {
                ContinueOnError(ErrorInfo.CreateNetworkError());
            });
    }

    void OnSasUrlError(ErrorInfo error)
    {
        print($"Retrieving SASURL failed!");
        // "*" at then end of the message is intentional.
        // We are using the same message when getting the sasURL fails and uploading the media.
        BottomPanelManager.Instance.ShowMessagePanel("Your content cannot be uploaded at this time. Please try again later.");
        
        // --- do something...
        OnBoardManager.Instance.ChallengeCompleted("");
    }

    void ContinueSubmitPhotoVideo(string caption, string url) {
        print($"OnBoardPanel: url = {url} | caption={caption}");

        if (url.IsNullOrWhiteSpace()) {            
            BottomPanelManager.Instance.ShowMessagePanel(
                "Your content cannot be uploaded at this time. Please try again later.", false, true, postAction: () => {
                    ShowChallengeLoader(false);
                });
            return;
        }

        ShowChallengeLoader(false);
        blobURL = url;
        CompletePhoto(url);
    }

    void ContinueOnError(ErrorInfo error) {
        BottomPanelManager.Instance.UpdateMessage(
            "Your content cannot be uploaded at this time. Please try again later.");
        // --- do something...
        OnBoardManager.Instance.ChallengeCompleted("");
    }

    IEnumerator WaitSubmitAndKeywords() {
        while (imageTags.Count == 0) {
            yield return null;
        }

        var faceParts = 0;
        isCorrectPhoto = false;
        BottomPanelManager.Instance.HideCurrentPanel(() => { });
        foreach (var t in imageTags) {
            var tag = t.ToLower();
            if (tag == "face" || tag == "person" || tag == "mask") {                
                isCorrectPhoto = true;                
                break;
            }
            else if (tag == "forehead" || tag == "nose" || tag == "chin" || tag == "eyebrow" || tag == "jaw" || tag == "ear" || tag == "cheek")
            {
                ++faceParts;
            }
        }
        // ToDo: for now, as long as we detect two parts of a face we will allow the photo.
        if (isCorrectPhoto || faceParts > 2)
        {
            yield return new WaitForSeconds(Const.PANEL_ANIMATION_TIME + 0.05f);
            isCorrectPhoto = true;
            SubmitPhoto();
            yield break;
        }

        yield return new WaitForSeconds(Const.PANEL_ANIMATION_TIME + 0.05f);
        isCorrectPhoto = false;
        
        outline.SetActive(true);
        outline.GetComponent<Image>().color = wrongColor;
        
        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.WrongChallengeKeywords);
        BottomPanelManager.Instance.UpdateMessage($"Keyword(s): {string.Join(", ", imageTags)}");
    }

    private void ShowChallengeLoader(bool visible) {
        if (challengeLoader == null) {
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
    
    IEnumerator DownloadPhoto()
    {
        using (var www = UnityWebRequestTexture.GetTexture(OnBoardManager.Instance.photoPath))
        {
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                print("Photo download failed.");
            }
            else
            {
                print("Photo download success.");
                Texture2D texture = ((DownloadHandlerTexture) www.downloadHandler).texture;
                Material newMat = new Material(Shader.Find("Mask/MaskedForARPlane"));
                newMat.CopyPropertiesFromMaterial(photoPlaneMat);
                newMat.SetTexture("_Albedo", texture);
                if (texture != null) {
                    board.GetComponent<Renderer>().material = newMat;
                    RecordManager.Instance.SetCroppingPositionOnARPanel(board);
                }
            }
        }
    }


    #endregion

    public void CompleteActivity()
    {
        CompleteActivityHelper(ActivityType.Likes);
    }

    public void CompleteVideo() {
        RecordManager.Instance.ExitFullscreen();
        CompleteActivityHelper(ActivityType.Video);
    }

    public void CompletePhoto(string photoURL)
    {
        CompleteActivityHelper(ActivityType.Challenge, photoURL);
        print($"OnBoardPanel CompletePhoto DONE: {photoURL}.");
    }
    
    void CompleteActivityHelper(ActivityType activityType, string photoURL = "") {
        if(!isActivityComplete) {
            isActivityComplete = true;
            outline.SetActive(true);
            outline.GetComponent<Image>().color = correctColor;
            if(activityType == ActivityType.Video || activityType == ActivityType.Likes || isCorrectPhoto) {
                OnBoardManager.Instance.AddPoints();
                rootForRotateEffect.transform.DOLocalRotate(new Vector3(0, 360, 0),
                    0.75f, RotateMode.FastBeyond360).OnComplete(() => { });
            }
            
            if (activityType == ActivityType.Video) {
                Debug.Log($"-=- Complete Video Activity");
                return;
            }
            
            if (activityType == ActivityType.Likes) {
                string allNames = "";
                foreach (var selectedName in selectedNames) {
                    allNames += ", " + selectedName;
                }
                Debug.Log($"-=- Complete Likes Activity, selected names = {allNames}");
                submitButton.gameObject.GetComponent<Image>().color = rightColor;
                submitButton.enabled = false;
                OnBoardManager.Instance.LikesCompleted();
                
                ActivityFeed.Instance.MakeLikes(selectedNames, this);
                return;
            }
            
            if (activityType == ActivityType.Challenge) {
                Debug.Log("-=- Complete Challenge Activity");
                OnBoardManager.Instance.ChallengeCompleted(photoURL);
                if (!isCorrectPhoto) {
                    outline.GetComponent<Image>().color = wrongColor;
                }
                return;
            }
        }
    }

    public void EnableButtons(bool interactable) {
        canvasGroup.alpha = (interactable ? 1 : 0.65f);
        canvasGroup.interactable = interactable;
    }

}

[Serializable]
public class OptionContent {
    public Sprite sprite;
    public string title;
    public GameObject objectInScene;
}
