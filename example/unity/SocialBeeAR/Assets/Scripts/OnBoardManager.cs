using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using RenderHeads.Media.AVProVideo;
using SocialBeeAR;
using SocialBeeARDK;
using UnityEngine;
using UnityEngine.UI;

public class OnBoardManager : MonoBehaviour
{
    [Header("--- Prefabs:")]//Anchor prefab
    public GameObject anchorObjPrefab;
    public GameObject bot;

    
    [Header("--- Speed (0.5 is twice faster)")]//Anchor prefab
    public float beeBotSpeed;
    public float waitingSpeed;
    
    // --- GUI Panels
    [Header("--- UI in scene:")]
    [SerializeField] private GameObject root;
    [SerializeField] private InputField nameInputIF;
    [SerializeField] private GameObject whatIsYourNamePanel;
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject plusBtnHighlightPanel;
    [SerializeField] private GameObject plusBtn;
    [SerializeField] private GameObject letsStartActivityPanel;

    // [SerializeField] private GameObject tapToPlaceActivityTopPanel;
    [SerializeField] private GameObject tapToPlaceActivityPanel;
    
    [SerializeField] private GameObject pointsPanel;
    [SerializeField] private Text pointsText;
    [SerializeField] private GameObject pointsPanelHighlight;

    [SerializeField] private GameObject youWillGetPointsToCompleteActivityPanel;
    [SerializeField] private GameObject askingToShare;
    [SerializeField] private GameObject askingToShareHighlight;
    [SerializeField] private Ease botFlyPath = Ease.InOutBack;
    [SerializeField] GameObject backButton;
    
    [Header("--- Since activity feed:")] 
    [SerializeField] private GameObject myFeedButton;
    [SerializeField] private GameObject hideActivityButton;
    [SerializeField] private GameObject weTrackYourActivities;
    [SerializeField] private GameObject tapToClose;
    [SerializeField] private GameObject nowLetsTryToShare;
    [SerializeField] private GameObject createMemoriesHighlight;
    [SerializeField] private GameObject createMemoriesButton;
    [SerializeField] private GameObject creatingMemories;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject finishButton;
    [SerializeField] private GameObject lastNextButton;
    [SerializeField] private GameObject excellentPanel;

    [Header("--- Contents:")]
    public string videoPath;
    public string thumbnailPath;
    public string photoPath;
    // --- Texts
    string str01 = "Hi, my name is Bee Bot";
    string str02 = "What is your name?";
    string str11 = "Welcome to Social Bee!";
    string str13 = "The ultimate experience platform.";
    string str21 = "In Social Bee, experiences consist of activities. Let's start by placing an activity.";
    string str22 = "Tap here";
    string str31 = "Point the \"X\" icon on any surface and tap it to place an activity.";
    string str41 = "You will get points everytime you complete an activity.";
    string askingToShareText = "Great work! In Social Bee experiences, you can see your content and create memories, let’s have a look.";
    string videoPanelText = "Play the video to get points.";
    string weTrackYourActivitiesText = "We track your activities on Social Bee.";
    string nowLetsTryToShareText = "Now, lets try to share your experience to get more points";
    string excellentText = "Excellent! Now let's get you signed up and make it official.";
    string userName = "";

    OnBoardingInputModel onboardInput; 

    int tempTextNumber = 1;
    Camera camera;
    private Vector3 initPos = new Vector3(-6.14f, -5.27f,5);
    private Vector3[] wayPoints = new Vector3[] {
        new Vector3(0.67f, -0.14f, 6),
        new Vector3(2.08f, 3.05f, 8.82f),
        new Vector3(0.07f, 4.48f, 10.1f),
        new Vector3(-1.8f, 3.8f, 9.5f),
        new Vector3(-1.53f, 2.85f, 9f)
    };
    
    private bool isBeeEnable;
    Tween flyTween;
    Vector3 botPos;
    private float botDistance = 9;
    [HideInInspector] public GameObject anchorObj;
    [HideInInspector] public OnBoardPanel likesPanel;
    [HideInInspector] public OnBoardPanel challengePanel;
    [HideInInspector] public OnBoardPanel videoPanel;
    // bool showArrow;
    private Vector3 neededPanel;
    [HideInInspector] public  Transform arrowTrigger;
    private string photoURL;
    // private bool mediaPlayerIsPlaing;
    // private MediaPlayer mediaPlayer;
    private List<string> categories = new List<string>();
    

    private static OnBoardManager _instance;
    public static OnBoardManager Instance
    {
        get
        {
            return _instance;
        }
    }

    private void Awake() {
        _instance = this;
    }

    private void OnEnable() {
        HideOnBoardContent();
    }

    // public Transform targetTest; // --- Test in editor
        
    void Start()
    {
        // backButton.SetActive(false);//added back by Cliff
        EraseTxtInChild_All();
        camera = Camera.main;
        onboardInput = new OnBoardingInputModel();
        // StartOnBoard(); // --- Test in editor
        // OnReticleTapped(targetTest); // --- Test in editor
    }

    private float timeForCheck = 0;
    private void Update() {
        if (isBeeEnable) {
            if(timeForCheck < Time.time) {
                timeForCheck = Time.time + 0.4f;
                if (!flyTween.active) {
                    Ray ray = camera.ScreenPointToRay(botPos);
                    Vector3 targetPos = ray.GetPoint(botDistance);
                    float dis = Vector3.Distance(bot.transform.position, targetPos);

                    // --- Send bot on right place on screen
                    if (dis / botDistance > 0.13f) {
                        flyTween = bot.transform.DOMove(targetPos, 0.9f * beeBotSpeed).SetEase(botFlyPath);
                        timeForCheck = Time.time + 0.9f;
                        // Debug.Log($"-=- dis = {dis / botDistance}");
                    }
                }
            }
        }
        
        // if (mediaPlayerIsPlaing && mediaPlayer.Control.IsPaused())
        // {
        //     print($"-=- OnBoardManager video memories complete");
        //     mediaPlayerIsPlaing = false;
        //     OnNextButtonAfterMemories();
        // }
        
        // if (Input.anyKeyDown) { // --- Test in editor
        //     OnReticleTapped(targetTest);
        // }
    }

    public void StartOnBoard() {
        // --- Start fly
        backButton.GetComponent<Button>().interactable = false;
        backButton.GetComponent<Image>().enabled = false;
        StartCoroutine(FirstFly());
        
        if(!String.IsNullOrWhiteSpace(SBContextManager.Instance.context.GetOnboardingVideoURL())) {
            videoPath = SBContextManager.Instance.context.GetOnboardingVideoURL();
        }
        
        RecordManager.Instance.PreLoadVideo(videoPath);
    }
    

    #region Action one (name input, plus button appears)
    
    IEnumerator FirstFly() {
        //update UI mode
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        
        yield return new WaitForSeconds(0.6f * waitingSpeed);
        ShowOnBoardContent();
        UIManager.Instance.SetTopBar("Introduction to Social Bee");
        
        // PlaneManager.Instance.SetARPlanesVisible(false);
        // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.None);
        // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);
        ARHelper.Instance.StopPlaneDetection();
        
        botPos = whatIsYourNamePanel.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        Ray ray = camera.ScreenPointToRay(botPos);
        botDistance = 9;
        Vector3 targetPos = ray.GetPoint(botDistance);
        Quaternion camRotation = camera.transform.rotation;
        Vector3 camPosition = camera.transform.position;
        
        initPos += camera.transform.position;
        initPos = camRotation * (initPos - camPosition) + camPosition;
        
        for (int i = 0; i < wayPoints.Length; i++) {
            // --- move by camera  
            wayPoints[i] += camera.transform.position;
            // --- rotate by camera  
            wayPoints[i] = camRotation * (wayPoints[i] - camPosition) + camPosition;
            if (i == wayPoints.Length - 1) {
                wayPoints[i] = targetPos;
            }
        }
        bot.SetActive(true);
        bot.transform.position = initPos;
        bot.GetComponentInChildren<LookAt>().lookAt = camera.transform;

        flyTween = bot.transform.DOPath(wayPoints, 3.5f * beeBotSpeed, PathType.CatmullRom);

        yield return new WaitForSeconds(3.4f * waitingSpeed);
        isBeeEnable = true;
        while (!RecordManager.Instance.isCameraPermission) {
            yield return null;
        }
        StartCoroutine(FirstAnimationDoneCO());
    }

    // --- When bot is here
    IEnumerator FirstAnimationDoneCO()
    {
        whatIsYourNamePanel.SetActive(true);
        yield return new WaitForSeconds(1f * waitingSpeed);

        StartCoroutine(TypeCharacters(whatIsYourNamePanel.transform.GetChild(0).GetComponent<Text>(), str01));
        yield return new WaitUntil(() => tempTextNumber == 2);
        StartCoroutine(TypeCharacters(whatIsYourNamePanel.transform.GetChild(1).GetComponent<Text>(), str02));

        yield return new WaitUntil(() => tempTextNumber == 3);
        nameInputIF.gameObject.SetActive(true);
    }
    
    // --- When the Name uf the user has been entered
    public void UpdateName()
    {
        if(!String.IsNullOrWhiteSpace(nameInputIF.text)) {
            userName = nameInputIF.text;
            StartCoroutine(UpdateNameCO());
            // print($"-=- OnBoardManager userName = {userName}");
            Debug.Log($"-=- OnBoardManager userName = {userName}");             
        }
        else {
            BottomPanelManager.Instance.ShowMessagePanel("Please tell us your name.", true);
        }
    }
    
    IEnumerator UpdateNameCO()
    {
        if (nameInputIF.text != "")
        {
            botPos = welcomePanel.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
            Ray ray = camera.ScreenPointToRay(botPos);
            botDistance = 10.5f;
            Vector3 targetPos = ray.GetPoint(botDistance);
            flyTween = bot.transform.DOMove(targetPos, 2.2f * beeBotSpeed).SetEase(botFlyPath);
            
            yield return new WaitForSeconds(0.2f * waitingSpeed);
            whatIsYourNamePanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
            nameInputIF.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        
            yield return new WaitForSeconds(1.7f * waitingSpeed);
            welcomePanel.SetActive(true);
            
            yield return new WaitForSeconds(1f * waitingSpeed);
            StartCoroutine(TypeCharacters(welcomePanel.transform.GetChild(0).GetComponent<Text>(), str11));
            yield return new WaitUntil(() => tempTextNumber == 4);
            StartCoroutine(TypeCharacters(welcomePanel.transform.GetChild(1).GetComponent<Text>(), userName));
            yield return new WaitUntil(() => tempTextNumber == 5);
            StartCoroutine(TypeCharacters(welcomePanel.transform.GetChild(2).GetComponent<Text>(), str13));
            yield return new WaitUntil(() => tempTextNumber == 6);
            
            yield return new WaitForSeconds(0.8f * waitingSpeed);
            StartCoroutine(SecondAnimationDoneCO());
        }
    }
    
    // --- When bot is here
    IEnumerator SecondAnimationDoneCO() {
        botPos = letsStartActivityPanel.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        Ray ray = camera.ScreenPointToRay(botPos);
        botDistance = 6;
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 2.9f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(0.2f * waitingSpeed);
        welcomePanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        
        yield return new WaitForSeconds(2.5f * waitingSpeed);
        letsStartActivityPanel.SetActive(true);
        
        StartCoroutine(TypeCharacters(letsStartActivityPanel.transform.GetChild(0).GetComponent<Text>(), str21));
        yield return new WaitUntil(() => tempTextNumber == 7);
        
        // yield return new WaitForSeconds(0.5f * waitingSpeed);
        plusBtn.SetActive(true);
        
        yield return new WaitForSeconds(0.1f * waitingSpeed);
        plusBtnHighlightPanel.SetActive(true);
        
        yield return new WaitForSeconds(0.3f * waitingSpeed);
        StartCoroutine(TypeCharacters(plusBtnHighlightPanel.transform.GetChild(0).GetComponent<Text>(), str22));
    }

    
    #endregion

    #region Action two (add anchor)

    
    public void OnPlusButton() {
        plusBtnHighlightPanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        plusBtn.SetActive(false);
        letsStartActivityPanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        
        // #placenote2lightship - we don't need this in ARDK?
        // PlaneManager.Instance.SetARPlanesVisible(true);
        // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.Hexagon);
        // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.All);
        //start placing reticle
        ReticleController.Instance.StartOnBoardingReticle();

        //update UI mode
        // UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.MovingReticle);
        StartCoroutine(ActionTwoSetAnchor());
        Debug.Log($"-=- OnBoardManager OnPlusButton");
    }

    IEnumerator ActionTwoSetAnchor() {
        yield return new WaitForSeconds(0.1f * waitingSpeed);
        botPos = tapToPlaceActivityPanel.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        Ray ray = camera.ScreenPointToRay(botPos);
        botDistance = 12;
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 1.7f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(0.5f * waitingSpeed);
        tapToPlaceActivityPanel.SetActive(true);
        StartCoroutine(TypeCharacters(tapToPlaceActivityPanel.transform.GetChild(0).GetComponent<Text>(), str31));
        yield return new WaitUntil(() => tempTextNumber == 8);
        
        yield return new WaitUntil(() => anchorObj != null);
        tapToPlaceActivityPanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
    }

    // --- Add Anchor with panels
    public void OnReticleTapped(Transform reticleTr) {
        Debug.Log($"-=- OnBoardManager OnReticleTapped, reticleTr.position = {reticleTr.position}");
        ReticleController.Instance.StopOnBoardReticle();
        
        Vector3 relativePos = camera.transform.position - reticleTr.position;
        relativePos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);

        anchorObj = Instantiate(anchorObjPrefab, reticleTr.position, rotation);
        
        pointsPanel.SetActive(true);
        
        // PlaneManager.Instance.SetARPlanesVisible(false);
        // PlaneManager.Instance.SetPlaneVisualizationType(PlaneManager.PlaneVisualizationType.Hexagon);
        // PlaneManager.Instance.SetPlaneDetectionMode(PlaneManager.PlaneDetectionMode.None);
        ARHelper.Instance.StopPlaneDetection();

        //update UI mode
        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ReticleTapped);
    }

    public void TextOnVideoPanel(Text text) {
        StartCoroutine(TypeCharacters(text, videoPanelText));
        OffScreenIndicatorManager.Instance.ShowArrow();
        OffScreenIndicatorManager.Instance.SetTarget(videoPanel.arrowTrigger);
    }

    IEnumerator FlyToPointsBar() {
        
        OffScreenIndicatorManager.Instance.HideArrow();
        botPos = pointsPanel.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        Ray ray = camera.ScreenPointToRay(botPos);
        botDistance = 8;
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 1.7f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(1.3f * waitingSpeed);
        pointsPanelHighlight.SetActive(true);
        youWillGetPointsToCompleteActivityPanel.SetActive(true);
        StartCoroutine(TypeCharacters(youWillGetPointsToCompleteActivityPanel.transform.GetChild(0).GetComponent<Text>(), str41));
        yield return new WaitUntil(() => tempTextNumber == 11);
        
        yield return new WaitForSeconds(1.1f * waitingSpeed);
        SetFade(OnBoardPanel.ActivityType.Likes);
        youWillGetPointsToCompleteActivityPanel.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        pointsPanelHighlight.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        OffScreenIndicatorManager.Instance.ShowArrow();
        OffScreenIndicatorManager.Instance.SetTarget(likesPanel.arrowTrigger);
        
        yield return new WaitForSeconds(0.2f * waitingSpeed);
        StartCoroutine(FlyToLikesPanel());
    }

    IEnumerator FlyToLikesPanel() {
        Debug.Log($"-=- FlyToLikesPanel()");
        isBeeEnable = false;
        Vector3 dir = (likesPanel.beePos.position - camera.transform.position).normalized;
        botDistance = 6;
        Ray ray = new Ray(camera.transform.position, dir);
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 2.7f * beeBotSpeed).SetEase(botFlyPath);
        yield return null;
    }

    public void LikesCompleted() {
        Debug.Log($"LikesCompleted()");
        StartCoroutine(FlyToChallengePanel());
        foreach (var selectedName in likesPanel.selectedNames) {
            if (!String.IsNullOrWhiteSpace(selectedName)) {
                categories.Add(selectedName);
            }
        }
    }
    
    IEnumerator FlyToChallengePanel() {
        OffScreenIndicatorManager.Instance.SetTarget(challengePanel.arrowTrigger);
        Vector3 dir = (challengePanel.beePos.position - camera.transform.position).normalized;
        botDistance = 10;
        Ray ray = new Ray(camera.transform.position, dir);
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 3.3f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(0.3f * waitingSpeed);
        SetFade(OnBoardPanel.ActivityType.Challenge);
    }
    
    public void GetKeywords(string keywords) {
        print($"-=- OnBoardManager GetKeywords");
        challengePanel.ShowKeywords(keywords);
    }
    
    public void ChallengeCompleted(string url) {
        print($"-=- OnBoardManager ChallengeCompleted()");
        // ArrowForMoveDeviceManager.Instance.HideArrow();
        // StartCoroutine(FlyToAskingToShare());
        photoURL = url;
        
        // ActivityFeed.Instance.ApplyPhotoOnBoard();        
    }

    public void ToAskingToShare() {
        print($"-=- OnBoardManager ToAskingToShare()");
        OffScreenIndicatorManager.Instance.HideArrow();
        StartCoroutine(FlyToAskingToShare());
        ActivityFeed.Instance.ApplyPhotoOnBoard();
    }

    public void WrongPhoto() {
        print($"-=- OnBoardManager WrongPhoto()");
        challengePanel.SubmitPhoto();
    }

    public void Retake() {
        print($"-=- OnBoardManager Retake()");
        challengePanel.OnAcceptChallenge();
    }

    #endregion

    #region Asking To Share

    IEnumerator FlyToAskingToShare() {
        
        yield return new WaitForSeconds(0.5f * waitingSpeed);
        botPos = askingToShare.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        Ray ray = camera.ScreenPointToRay(botPos);
        botDistance = 7;
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 1.9f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(0.3f * waitingSpeed);
        SetFade(OnBoardPanel.ActivityType.Challenge, true);
        
        yield return new WaitForSeconds(1.5f * waitingSpeed);
        isBeeEnable = true;
        askingToShare.SetActive(true);
        
        StartCoroutine(TypeCharacters(askingToShare.transform.GetChild(0).GetComponent<Text>(), askingToShareText));
        yield return new WaitUntil(() => tempTextNumber == 12);
        myFeedButton.SetActive(true);
        askingToShareHighlight.SetActive(true);
        
        StartCoroutine(TypeCharacters(askingToShareHighlight.transform.GetChild(0).GetComponent<Text>(), "Tap here"));
        yield return new WaitUntil(() => tempTextNumber == 13);
    }

    
    // --- this method is called by myFeedButton
    public void ShowActivityFeed() {
        // myFeedButton.SetActive(false);
        
        // FinalizeOnBoarding();

        // --- for test
    }

    public void FlyToWeTrack() {
        askingToShare.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        askingToShareHighlight.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        StartCoroutine(FlyToWeTrackYourActivities());
    }
    
    // --- Screen13
    IEnumerator FlyToWeTrackYourActivities() {
        yield return new WaitForSeconds(0.7f * waitingSpeed);
        print($"-=- OnBoardManager FlyToWeTrackYourActivities2");
        weTrackYourActivities.SetActive(true);
        StartCoroutine(TypeCharacters(weTrackYourActivities.transform.GetChild(0).GetComponent<Text>(), weTrackYourActivitiesText));
        yield return new WaitUntil(() => tempTextNumber == 14);
        yield return new WaitForSeconds(0.5f * waitingSpeed);
        // hideActivityButton.SetActive(true);
        tapToClose.SetActive(true);
        StartCoroutine(TypeCharacters(tapToClose.transform.GetChild(0).GetComponent<Text>(), "Tap to close"));
        yield return new WaitUntil(() => tempTextNumber == 15);
        
        yield return new WaitForSeconds(2.7f * waitingSpeed);
        ActivityFeed.Instance.MoveActivities();
        
        yield return new WaitForSeconds(3.4f * waitingSpeed);
        
        // botPos = weTrackYourActivities.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        // Ray ray = camera.ScreenPointToRay(botPos);
        // botDistance = 9;
        // Vector3 targetPos = ray.GetPoint(botDistance);
        // flyTween = bot.transform.DOMove(targetPos, 1.7f * beeBotSpeed).SetEase(botFlyPath);
        
        
        NowLetsTryToShare();
    }

    public void NowLetsTryToShare() {
        ActivityFeed.Instance.OnPauseButton();
        ActivityFeed.Instance.EnablePlayButton(false);
        videoPanel.EnablePlayButton(false);
        StartCoroutine(NowLetsTryToShareEnumerator());
    }

    // --- Screen15
    IEnumerator NowLetsTryToShareEnumerator() {
        weTrackYourActivities.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        tapToClose.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        
        // yield return new WaitForSeconds(0.3f * waitingSpeed);
        // botPos = nowLetsTryToShare.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        // Ray ray = camera.ScreenPointToRay(botPos);
        // botDistance = 9;
        // Vector3 targetPos = ray.GetPoint(botDistance);
        // flyTween = bot.transform.DOMove(targetPos, 1.7f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(0.4f * waitingSpeed);
        // weTrackYourActivities.GetComponent<SingletonForAnimatonEvents>().background.SetActive(false);
        weTrackYourActivities.SetActive(false);
        tapToClose.SetActive(false);
        // nowLetsTryToShare.GetComponent<SingletonForAnimatonEvents>().background.SetActive(true);
        
        // -- to finish
        StartCoroutine(FinishAction());
        ActivityFeed.Instance.HideActivityFeedFast();
        myFeedButton.SetActive(false);
        // yield return new WaitForSeconds(0.2f * waitingSpeed);
        videoPath = "";
        RecordManager.Instance.mediaPlayerIsPause = false;
        
        ActivityFeed.Instance.enabled = false;
        Destroy(ActivityFeed.Instance.mediaPlayer);
        RecordManager.Instance.CleanMediaPlayerPath();
        print("OnBoardManager Done");
        
        yield break;
        
        nowLetsTryToShare.SetActive(true);
        // createMemoriesButton.GetComponent<Button>().interactable = false;
        StartCoroutine(TypeCharacters(nowLetsTryToShare.transform.GetChild(0).GetComponent<Text>(), nowLetsTryToShareText));
        yield return new WaitUntil(() => tempTextNumber == 15);

        // createMemoriesButton.SetActive(true);
        createMemoriesHighlight.SetActive(true);
        
        yield return new WaitForSeconds(0.2f * waitingSpeed);
        
        StartCoroutine(TypeCharacters(createMemoriesHighlight.transform.GetChild(0).GetComponent<Text>(), str22));
        yield return new WaitUntil(() => tempTextNumber == 16);
        createMemoriesButton.SetActive(true);
        // createMemoriesButton.GetComponent<Button>().interactable = true;
    }

    // public void OnScreenShot() {
    //     Texture2D tex = RecordManager.Instance.Screenshot();
    //     Sprite newSprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height),
    //         new Vector2(0.5f, 0.5f), 100.0f);
    //     screenShotButton.GetComponent<Image>().sprite = newSprite;
    // }

    #endregion

    #region CreateMemories

    
    public void CreatingMemories() {
        // StartCoroutine(ClosePanles());
    }
    
    IEnumerator ClosePanles() {
        
        createMemoriesButton.SetActive(false);
        createMemoriesHighlight.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        nowLetsTryToShare.GetComponent<Animator>().SetBool("PanelZoomOut", true);
        
        yield return new WaitForSeconds(0.3f * waitingSpeed);
        nowLetsTryToShare.SetActive(false);
        createMemoriesButton.SetActive(false);
        creatingMemories.SetActive(true);
        
        yield return new WaitForSeconds(0.9f * waitingSpeed);
        ActivityFeed.Instance.HideActivityFeedFast();
        myFeedButton.SetActive(false);
        creatingMemories.SetActive(false);
        nextButton.SetActive(true);
        
        // ActivityFeed.Instance.mediaPlayer.Control.Stop();
        // ActivityFeed.Instance.ShowMemoriesVideo(videoPath);
        // ActivityFeed.Instance.mediaPlayer.Loop = false;
        // mediaPlayer = ActivityFeed.Instance.mediaPlayer;
        
        yield return new WaitForSeconds(2.1f * waitingSpeed);
        // mediaPlayerIsPlaing = true;
    }

    public void OnNextButtonAfterMemories() {
        nextButton.SetActive(false);
        ActivityFeed.Instance.HideMemoriesVideo();
        ActivityFeed.Instance.ShowActivityFeedFast();
        
        ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ShareMemories);
    }

    #endregion
    
    #region Finish

    public void OnImDone() {
        BottomPanelManager.Instance.HideCurrentPanel();
        ActivityFeed.Instance.HideActivityFeed(() => {
            myFeedButton.SetActive(false);
            finishButton.SetActive(true);
        });
    }

    public void OnFinishButton() {
        finishButton.SetActive(false);
        if (anchorObj != null) {
            Destroy(anchorObj);
        }

        StartCoroutine(FinishAction());
    }

    IEnumerator FinishAction() {
        botPos = excellentPanel.GetComponent<SingletonForAnimatonEvents>().GetBotPosition();
        Ray ray = camera.ScreenPointToRay(botPos);
        botDistance = 5;
        Vector3 targetPos = ray.GetPoint(botDistance);
        flyTween = bot.transform.DOMove(targetPos, 1.3f * beeBotSpeed).SetEase(botFlyPath);
        
        yield return new WaitForSeconds(0.9f * waitingSpeed);
        excellentPanel.SetActive(true);
        
        yield return new WaitForSeconds(0.3f * waitingSpeed);
        StartCoroutine(TypeCharacters(excellentPanel.transform.GetChild(0).GetComponent<Text>(), excellentText));
        yield return new WaitUntil(() => tempTextNumber == 16);
        
        lastNextButton.SetActive(true);
    }

    public void OnLastNextButton() {
        backButton.GetComponent<Button>().interactable = true;
        backButton.GetComponent<Image>().enabled = true;
        
        lastNextButton.SetActive(false);
        HideOnBoardContent();
        print("-=- OnBoard End!");

        FinalizeOnBoarding();
        
        InteractionManager.Instance.DestroyARSession();
        
        NativeCall.Instance.EndAR();
    }
    

    #endregion
    
    

    
    public void AddPoints() {
        if(pointsText.text == "0") {
            pointsText.text = "20";
            StartCoroutine(FlyToPointsBar());
        }
        else if(pointsText.text == "20") {
            pointsText.text = "40";
        }
        else if(pointsText.text == "40") {
            pointsText.text = "60";
        }
    }

    void SetFade(OnBoardPanel.ActivityType type, bool disable = false) {
        Debug.Log($"-=- SetFade type = {type.ToString()}");
        videoPanel.EnableButtons(false);
        likesPanel.EnableButtons(false);
        challengePanel.EnableButtons(false);

        if(!disable) {
            if (type == OnBoardPanel.ActivityType.Video) {
                videoPanel.EnableButtons(true);
                arrowTrigger = videoPanel.arrowTrigger;
            }

            if (type == OnBoardPanel.ActivityType.Likes) {
                likesPanel.EnableButtons(true);
                arrowTrigger = likesPanel.arrowTrigger;
            }

            if (type == OnBoardPanel.ActivityType.Challenge) {
                challengePanel.EnableButtons(true);
                arrowTrigger = challengePanel.arrowTrigger;
            }
        }
    }

    // Auto typing of characters
    // Parameters: Txt=GUI Text, str=actual text to be written
    IEnumerator TypeCharacters(Text Txt, string str) {
        Debug.Log($"TypeCharacters str = {str}");
        char[] charArr = str.ToCharArray();
        int charCount = 0;

        foreach (char tempChar in charArr)
        {
            if (charCount >= 13) {
                break;
            }
            yield return new WaitForSeconds(Time.deltaTime * 1.2f);
            Txt.text += tempChar;
            charCount++;
        }
        Txt.text = str;
        charCount = charArr.Length;
        // yield return new WaitForSeconds(0.5f);
        yield return new WaitForSeconds(charCount * 0.027f);
        tempTextNumber++;
    }

    public void EraseTxtInChild_All()
    {
        EraseTxtInChild(whatIsYourNamePanel);
        EraseTxtInChild(welcomePanel);
        EraseTxtInChild(plusBtnHighlightPanel);
        EraseTxtInChild(letsStartActivityPanel);
        EraseTxtInChild(tapToPlaceActivityPanel);

        nameInputIF.text = "";
    }

    void EraseTxtInChild(GameObject GO) {
        for (int i = 0; i < GO.transform.childCount; i++) {
            if(GO.transform.GetChild(i).GetComponent<Text>() != null) {
                GO.transform.GetChild(i).GetComponent<Text>().text = "";
            }
        }
    }

    void FinalizeOnBoarding() {
        print($"FinalizeOnBoarding > photoURL: {photoURL}");
        onboardInput.CreatePost(SBContextManager.Instance.context.GetProperLocation());
        onboardInput.Name = userName;
        // @Dmitry, please use the appropriate values here.        
        onboardInput.AddPhoto(
            "My Social Bee profile picture",
            RecordManager.Instance.filteredFilePath, // use the local path of the photo here
            photoURL // use the URL from our blob server here
            );
        // @Dmitry, assign here the categories that the user selected
        // from the "Choose what you like" (categories) panel.
        // categories = likesPanel.selectedNames.ToList();
        print($"categories.Count = {categories.Count}");
        onboardInput.PreferredCategories = categories;
        NativeCall.Instance.OnLoadActivityFeed(onboardInput.ToJson());        
    }

    public void HideOnBoardContent() {
        root.SetActive(false);
        bot.SetActive(false);
        if (anchorObj != null) {
            Destroy(anchorObj);
        }
    }

    public void ShowOnBoardContent() {
        root.SetActive(true);
        bot.SetActive(true);
    }
}
