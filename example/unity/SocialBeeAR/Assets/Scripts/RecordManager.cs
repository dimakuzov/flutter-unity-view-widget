using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NatCorder;
using NatCorder.Clocks;
using NatCorder.Inputs;
using SocialBeeAR;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using System.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Plugins.Core.PathCore;
using RenderHeads.Media.AVProVideo;
using SocialBeeARDK;
#if UNITY_IPHONE
using UnityEngine.Apple.ReplayKit;
#endif
using UnityEngine.XR.ARFoundation;

public class RecordManager : MonoBehaviour
{
    // Refreshing repo commit...
#if UNITY_ANDROID
    [DllImport ("libARWrapper.so")]
    public static extern string GetSettingsURL(); 
#elif UNITY_IPHONE 
    [DllImport ("__Internal")]
    public static extern string GetSettingsURL(); 
#endif

    int videoWidth = 720;
    int videoHeight = 1280;

    [Header(" --- General settings")]
    /// <summary>
    /// It sets the maximum video time
    /// </summary>
    public float maxVideoRecordingTime;


    /// <summary>
    /// It sets the maximum video time
    /// </summary>
    public float maxAudioRecordingTime;

    /// <summary>
    /// Created and saveed content on this device
    /// Dictionary<Key = activity Id, Value = content path>
    /// </summary>
    public Dictionary<string, string> oldContents = new Dictionary<string, string>();

    /// <summary>
    /// this data is specified by the user when he crops a photo/video, and it is used to show on the AR panel
    /// </summary>
    [HideInInspector] public Vector3 croppingData = Vector3.zero;

    /// <summary>
    /// This is flag that we start AR session from Photo/Video taking
    /// </summary>
    [HideInInspector] public bool startWithPhotoVideo;

    /// <summary>
    /// This is flag that we have on the screen the record UI
    /// </summary>
    [HideInInspector] public bool isRecordUI;
    
    /// <summary>
    /// This is the URL of the photo/video either from our blob server or from an external resource.
    /// </summary>
    [HideInInspector] public string contentPublicPath;
    /// <summary>
    /// This is the URL of the photo from the user's device.
    /// </summary>
    [HideInInspector] public string photoPath;
    [HideInInspector] public string videoPath;
    [HideInInspector] public string filteredFilePath;
    [HideInInspector] public string audioPath;


    [Header("Video output setting")]
    public bool recordMicrophone;
    public int frameRate = 30;
    public int bitrate = 5500000;
    public int fileWidthSize = 750;
    private int oldFrameRate;

    [Header(" --- Filter's stuff")]
    [SerializeField] Camera filterCamera;
    [SerializeField] private GameObject photoPlaneForRecord;
    public List<Material> filterMats = new List<Material>();
    public List<string> filterNames = new List<string>();
    public List<Sprite> filterSprites = new List<Sprite>();
    [SerializeField] private Material photoPlaneMat;
    public Vector2 croppingMinSize;
    [SerializeField] private Renderer playerTexture1;
    [SerializeField] private Renderer playerTexture2;

    [Header(" --- Audio's stuff")]
    [SerializeField] private RecordAudioUIManager recordAudioUIManager;
    [SerializeField] private VideoPlayer audioRecordPlayer;

    [Header(" --- Cropping Page")]
    [SerializeField]
    private GameObject croppingPage;
    [SerializeField] private CroppingButton selectedArea;
    // [SerializeField] private GameObject showCroppingButton;
    // [SerializeField] private GameObject focalPointPage;
    [SerializeField] private Toggle focalPointButton;
    [SerializeField] private GameObject exitFullscreenButton;

    VideoPlayer[] recordFilterVideoPlayers;
    [HideInInspector] public int selectedMatInList;
    [HideInInspector] public bool isPhotoTaking;
    private Texture2D originalPhotoTexture2D;

    // --- video segments creations
    [HideInInspector] public List<string> segmentPaths = new List<string>();
    private string segmentPath;
    private List<float> segmentLengths = new List<float>();
    float segmentLength;

    [Header(" --- For Camera Changing")]
    [SerializeField]
    ARFaceManager arFaceManager;
    ARPlaneManager arPlaneManager;


    [Header(" --- Components in scene")]
    [SerializeField] private Toggle flashlight;
    [SerializeField] private RecordUIManager recordUiManager;
    [SerializeField] private MediaPlayer mediaPlayer;
    [SerializeField] private ApplyToMesh applyToMesh;
    [SerializeField] private GameObject mediaPlayerFullscreen;

    private IMediaRecorder recorder;
    private CameraInput cameraInput;
    private AudioInput audioInput;
    [HideInInspector] public AudioSource microphoneSource;
    private RealtimeClock clock;
    [HideInInspector] public string status = ""; // "", "pause", "record", "waitPhoto", "filterApplying", "photoTakeOnly", "WaitMicrophone"
    private AudioSource audioSourcesForMergeContent; // maybe it no need already

    public bool isNewPhotoVideo = false;
    public bool isNewAudio = false;
    private InteractionManager interactionManager;
    private bool landscapeOrient;
    // private Texture2D previewForARPlane;

    // --- we need it for setting cancel button
    [HideInInspector] public bool isCreateMode;
    [HideInInspector] public Texture2D filteredThumbnail;
    [HideInInspector] public OnBoardPanel onBoard;
    [HideInInspector] public OnBoardPanel onBoardChallenge;
    private bool wasPreRecord;
    
    [Header(" --- Permission panels")]
    [SerializeField] private GameObject cameraPermissionPanel;
    [SerializeField] private GameObject microphonePermissionPanel;
    [SerializeField] private GameObject waitMicrophonePanel;
    [SerializeField] private GameObject waitMicrophonePanelAudio;

    [SerializeField] private MediaReference emptyMediaReference;
    
    // Permission flags
    [HideInInspector] public bool isCameraPermission;
    [HideInInspector] public bool isMicrophonePermission;

    [HideInInspector]
    private static IAnchorManager ActiveAnchorManager =>
        SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
            ? AnchorManager.Instance
            : WayspotAnchorManager.Instance;
    
    private static RecordManager _instance;
    public static RecordManager Instance
    {
        get
        {
            return _instance;
        }
    }

    public Action<string, string> OnAudioMergeCompleted;

    #region Unity Functions


    private void Awake()
    {
        _instance = this;
        // isCameraPermission = true;
    }

    private IEnumerator Start()
    {        
        recordUiManager.record = this;
        videoWidth = Screen.width;
        videoHeight = Screen.height;
        if (fileWidthSize > videoWidth) {
            fileWidthSize = videoWidth;
        }
        interactionManager = transform.GetComponent<InteractionManager>();

        microphoneSource = gameObject.AddComponent<AudioSource>();

        // #lightship-REMOVE_TEMPORARILY: No front-facing camera support in ARDK.
        // arFaceManager = FindObjectOfType<ARFaceManager>();
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        // Input.location.Start();
        LoadContentPath();
        mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
        mergeAudioSource = mediaPlayer.gameObject.GetComponent<AudioSource>();
        
        origPlane = origMergeCamera.GetComponentInChildren<Renderer>().transform;
        filterPlane = filterMergeCamera.GetComponentInChildren<Renderer>().transform;
        print($"origPlane.position = {origPlane.position}, filterPlane.position = {filterPlane.position}");
        oldFrameRate = Application.targetFrameRate;
        yield return new WaitForSeconds(1.1f);
        StartCheckingCameraPermissions();
        yield return null;
    }

    private string lastStatus = "123";
    private void Update()
    {        
        if (lastStatus != status)
        {            
            lastStatus = status;
            print($"-=- status = {status}");
        }

        //Check orientation
        if (isRecordUI && status == "" || isRecordUI && status == "photoTakeOnly") {
            if(landscapeOrient != IsLandscapeOrientation()) {
                ChangeOrientation(IsLandscapeOrientation());
                landscapeOrient = !landscapeOrient;
                print($"-=- landscapeOrient != IsLandscapeOrientation() = {landscapeOrient}");
            }
        }

        if (mediaPlayerIsPlaing && mediaPlayer.Control.IsPaused() && !isRecordingMergeVideo)
        {
            if (lastConsumeVideoActivity != null)
            {
                lastConsumeVideoActivity.CompleteActivity();
                lastConsumeVideoActivity.ShowPlayButton();
                lastConsumeVideoActivity.StopAllClipLoader(true);
            }

            if (lastCreateVideoActivity != null)
            {
                lastCreateVideoActivity.ShowPlayButton();
                lastCreateVideoActivity.StopAllClipLoader(true);
            }

            if (lastAudioActivity != null)
            {
                lastAudioActivity.CompleteActivity();
                lastAudioActivity.ShowPlayButton();
            }

            if (onBoard != null)
            {
                print($"-=- RecordManager onBoard CompleteActivity");
                onBoard.CompleteVideo();
                onBoard.ShowPlayButton();
            }

            mediaPlayerIsPlaing = false;
            mediaPlayer.Control.Rewind();
            print($"-=- RecordManager video complete");
        }
        // else if(isRecordFilteredVideo && mediaPlayer.Control.IsPaused() && isPlayingFilteredVideo) {
        //     PlayBothAllSegmentsOneTime();
        // }
    }

    private void OnEnable()
    {
        recordFilterVideoPlayers = filterCamera.gameObject.GetComponents<VideoPlayer>();
        videoWidth = Screen.width;
        videoHeight = Screen.height;

        float w = videoWidth;
        float h = videoHeight;
        float orthographicSize = 1 - ((2 - (h / w)) / 2);
        filterCamera.orthographicSize = orthographicSize;

        // --- here we scaling camera for showing video/photo before filter applying
        ScalePanelByWidth(filterCamera.gameObject);

        // --- creating one .mp4 with filters
        recordFilterVideoPlayers[0].prepareCompleted += StartCreateVideoWithFilter;

        // --- showing segmented video with filters
        recordFilterVideoPlayers[0].prepareCompleted += PauseClip;
        recordFilterVideoPlayers[1].prepareCompleted += PauseClip;
        recordFilterVideoPlayers[0].loopPointReached += PlayNewClip;
        recordFilterVideoPlayers[1].loopPointReached += PlayNewClip;

        // --- audio record settings
        audioSourcesForMergeContent = audioRecordPlayer.GetComponent<AudioSource>();
        audioRecordPlayer.SetTargetAudioSource(0, audioSourcesForMergeContent);
        recordFilterVideoPlayers[0].SetTargetAudioSource(0, audioSourcesForMergeContent);
        recordFilterVideoPlayers[1].SetTargetAudioSource(0, audioSourcesForMergeContent);
        audioRecordPlayer.loopPointReached += ChangeClip;
        audioRecordPlayer.prepareCompleted += StartFirstAudioClipForMerge;
    }

    private void OnDisable()
    {
        // --- creating one .mp4 with filters
        recordFilterVideoPlayers[0].prepareCompleted -= StartCreateVideoWithFilter;

        // --- showing segmented video with filters
        recordFilterVideoPlayers[0].prepareCompleted -= PauseClip;
        recordFilterVideoPlayers[1].prepareCompleted -= PauseClip;
        recordFilterVideoPlayers[0].loopPointReached -= PlayNewClip;
        recordFilterVideoPlayers[1].loopPointReached -= PlayNewClip;

        // --- audio
        audioRecordPlayer.loopPointReached -= ChangeClip;
        audioRecordPlayer.prepareCompleted -= StartFirstAudioClipForMerge;
    }

    private void OnDestroy()
    {
        // Stop microphone
        microphoneSource.Stop();
        Microphone.End(null);
    }


    #endregion

    #region Public functions

    public void PhotoTakeOnly()
    {
        status = "photoTakeOnly";
    }

    public void OnTouchDown()
    {
        print($"RM > OnTouchDown > status={status}");
        if (status == "" || status == "photoTakeOnly")
        {
            isNewPhotoVideo = true;
            StartCoroutine(recordUiManager.Countdown());
        }
        else if (status == "pause")
        {
            isNewAudio = true;
            StartCoroutine(recordUiManager.ResumeCountdown());
            status = "record";
        }
        else
        {
            isNewAudio = true;
        }
    }

    public void OnClick()
    {
        print($"RM > OnClick > status={status}");
        if (status is "" or "photoTakeOnly")
        {
            isNewPhotoVideo = true;
            GetPhoto();
        }
        else
        {
            isNewAudio = true;
        }
    }

    public void OnTouchUp()
    {
        print("RM > OnTouchDown");
        if (recordUiManager.ratio >= 1.0f)
        {
            status = "timeIsUp";
            StopRecording();
        }
        else if (status == "record")
        {
            status = "pause";
            StopRecording();
        }
    }

    public void NextButton()
    {
        print($"status={status}");
        if (status == "pause")
        {
            StartShowVideoWithFilterFirstTime();
        }
        else if (status == "filterApplying")
        {
            DoApplyFilter();
        }
        else
        {
            recordUiManager.Reset(false);
        }
    }

    private void DoApplyFilter()
    {
        if (!String.IsNullOrWhiteSpace(filteredFilePath) && !SBContextManager.Instance.IsOnBoarding())
        {
            DeletePhotoOrVideo(false);
            status = "filterApplying";
        }

        filteredThumbnail = Screenshot();
        if (startWithPhotoVideo)
        {
            print($"RecordManager NextButton() startWithPhotoVideo");
            // if it is QuickPhoto from native
            if (ActiveAnchorManager.GetAnchorObjectList().Count <= 0)
            {
                //create
                print($"RecordManager NextButton() > Create mode > StartAsIntegratedModule...");
                InteractionManager.Instance.StartAsIntegratedModule(() =>
                {
                    UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
                    if (SBContextManager.Instance.context.isPlanning || SBContextManager.Instance.context.markerPlacementType == (int)SBAnchorPlacementType.GPS) //planning
                    {
                        print($"RecordManager NextButton() > Create mode > isPlanning > OnNewActivityClicked...");
                        if (SBContextManager.Instance.context.markerPlacementType == (int)SBAnchorPlacementType.GPS)
                        {
                            SBContextManager.Instance.UpdateIsCreatingGPSOnlyAnchors(true);    
                        }
                        InteractionManager.Instance.OnNewActivityClicked();
                    }
                    else //normal create (non-planning)
                    {
                        print($"RecordManager NextButton() > Create mode > NOT Planning > AskIndoorOrOutdoor...");
                        InteractionManager.Instance.AskIndoorOrOutdoor();
                    }
                });
            }
            else
            {
                //edit
                // print($"RecordManager NextButton() > Edit mode > OnNewActivityClicked...");
                InteractionManager.Instance.OnNewActivityClicked();
            }

            // if we edit video
            if (currPhotoVideoActivity != null && !isPhotoTaking)
            {
                if (currPhotoVideoActivity.videoPaths.Count == 0 ||
                    currPhotoVideoActivity.videoPaths[0] != segmentPaths[0])
                {
                    // print($"StopRecordBothVideos() NextButton() PhotoVideoActivity.videoPaths.Count == 0 and PhotoVideoActivity.videoPaths[0] != segmentPaths[0]");
                    currPhotoVideoActivity.ConfigureSegments(segmentPaths, selectedMatInList);
                    currPhotoVideoActivity.ShowThumbnail(filteredThumbnail);
                    currPhotoVideoActivity.ShowPlayButtons();
                }
            }
        }
        else
        {
            if (!isPhotoTaking)
            {
                interactionManager.ShowVideoThumbnail(filteredThumbnail);
                currPhotoVideoActivity.DisableSaveButton(true);
                ShowVideoSegmentsOnPanel();
            }

            OffScreenIndicatorManager.Instance.ShowArrow();
        }

        SaveFileWithFilter();
        ShowARContent();
    }

    public void CancelButton()
    {
        isNewPhotoVideo = false;
        isNewAudio = false;
        if (status == "filterApplying")
        {
            filterCamera.depth = -2;
            if (segmentPaths.Count > 0 && !isPhotoTaking)
            {
                recordUiManager.Reset(true);
                status = "pause";
                foreach (var videoPlayer in recordFilterVideoPlayers)
                {
                    videoPlayer.Stop();
                    videoPlayer.clip = null;
                }
            }
            else
            {
                recordUiManager.Reset(false);
                status = "";
            }

            if (fromGallery)
            {
                recordUiManager.Reset(false);
                fromGallery = false;
                playerTexture1.transform.localScale = Vector3.one;
                playerTexture2.transform.localScale = Vector3.one;
                photoPlaneForRecord.transform.localScale = Vector3.one;
                segmentPaths.Clear();
                status = "";
            }
            else if(!SBContextManager.Instance.context.startWithPhotoVideo)
            {
                DeletePhotoOrVideo(false);
            }

            StartEnableMicrophone();

            // --- Recomment for enable Cropping
            // focalPointButton.gameObject.SetActive(true);
            // showCroppingButton.SetActive(false);
            // focalPointButton.isOn = true;
        }
        else
        {
            ShowARContent();//recover AR content
            
            if (startWithPhotoVideo)
            {
                // --- Back to native...
                status = "";
                DeleteAllSegmentsData();
                ClearPaths(false);
                CancelRecording();
                DeletePhotoOrVideo(true);
                
                if(ActiveAnchorManager.GetAnchorObjectList().Count <= 0) {
                    StopMicrophone();
                    ChangeCamera(true);
                    flashlight.isOn = true;
#if UNITY_IOS
                    FlashlightController.Instance.TurnOff();
#endif
                    InteractionManager.Instance.DoBackToNative(true);
                    startWithPhotoVideo = false;
                    return;
                }
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.NextAnchorOrSaveMap);
            }

            if (status == "pause")
            {
                if(!SBContextManager.Instance.context.startWithPhotoVideo || segmentLengths.Count > 1) {
                    int segmentCount = segmentLengths.Count;
                    for (int i = 0; i < segmentCount; i++) {
                        DeleteSegment();
                    }
                }
                else {
                    recordUiManager.DeleteSegment(segmentLengths[0]);
                }
            }

            if (isCreateMode)
            {
                if(!SBContextManager.Instance.context.startWithPhotoVideo) {
                    currPhotoVideoActivity.OnCancelCreate();
                }
                else {
                    CancelRecording();
                    status = "";
                }
            }
            else if (currPhotoVideoActivity != null)
            {
                currPhotoVideoActivity.OnCancelEdit();
                ShowThumbnail(currPhotoVideoActivity.contentBoard); // --- it is work!!
            }
            // --- if it is challenge
            else if(currPhotoVideoActivityForConsume != null){
                currPhotoVideoActivityForConsume.OnCancelChallenge();
                CancelRecording();
                DeletePhotoOrVideo(true);
            }
            StopMicrophone();
            ChangeCamera(true);
            flashlight.isOn = true;
#if UNITY_IOS
            FlashlightController.Instance.TurnOff();
#endif
            SetFrameRate(oldFrameRate);
        }
    }

    public void CancelRecording()
    {
        recordUiManager.Clear();
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
    }

    public void DeletePhotoOrVideo(bool onlyPhoto)
    {

        print("FROM AR: RecordManager.DeletePhotoOrVideo");
        if (!String.IsNullOrEmpty(photoPath))
        {
            if (File.Exists(photoPath))
            {
                try
                {
                    Directory.Delete(photoPath.Remove(photoPath.Length - 6), true);
                }
                catch (Exception ex)
                {
                    print($"DeletePhotoOrVideo Directory.Delete Exception: {ex.Message}");
                }

            }
        }

        if (!String.IsNullOrEmpty(filteredFilePath)) {
            if (Directory.Exists(filteredFilePath.Remove(filteredFilePath.Length - 6)))
            {
                try
                {
                    Directory.Delete(filteredFilePath.Remove(filteredFilePath.Length - 6), true);
                }
                catch (Exception ex)
                {
                    print($"DeletePhotoOrVideo Directory.Delete Exception: {ex.Message}");
                }

            }
        }

        if (status == "pause" && !onlyPhoto)
        {
            status = "";
        }
        else if (!onlyPhoto)
        {
            if (!String.IsNullOrEmpty(videoPath))
            {
                if (File.Exists(videoPath))
                {
                    try
                    {
                        File.Delete(videoPath);
                        videoPath = "";
                    }
                    catch (Exception ex)
                    {
                        print($"DeletePhotoOrVideo File.Delete Exception: {ex.Message}");
                    }
                    status = "";
                }
            }
            if (!String.IsNullOrEmpty(filteredFilePath))
            {
                if (File.Exists(filteredFilePath))
                {
                    try
                    {
                        File.Delete(filteredFilePath);
                        filteredFilePath = "";
                    }
                    catch (Exception ex)
                    {
                        print($"DeletePhotoOrVideo File.Delete Exception: {ex.Message}");
                    }
                    status = "";
                }
            }
        }
    }

    public void DeleteSegment()
    {
        if (segmentLengths.Count > 0)
        {
            recordUiManager.DeleteSegment(segmentLengths[segmentLengths.Count - 1]);
        }

        if (segmentPaths.Count > 0)
        {
            string path = segmentPaths[segmentPaths.Count - 1];
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    print($"DeletePhotoOrVideo File.Delete Exception: {ex.Message}");
                }
            }

            segmentPaths.RemoveAt(segmentPaths.Count - 1);
            segmentLengths.RemoveAt(segmentLengths.Count - 1);
        }
        if (segmentPaths.Count == 0)
        {
            print($"-=- Last segment was removed");
            // recordUiManager.Reset(false);
            status = "";
        }
    }

    public void DeleteAllSegmentsData()
    {
        int countOfSegments = segmentPaths.Count;
        for (int i = 0; i < countOfSegments; i++)
        {
            DeleteSegment();
        }
        print($"-=- RecordManager segmentPaths.Count = {segmentPaths.Count}");
        segmentPaths = new List<string>();
        segmentLengths = new List<float>();
        segmentPath = null;
        segmentLength = 0;
    }

    void ChangeCamera(bool isRearCamera) {
        if (isRearCamera)
        {
            // #lightship-REMOVE_TEMPORARILY: No front-facing camera support in ARDK.
            // if (arFaceManager.isActiveAndEnabled)
            // {
            //      WorldPositionСorrection.Instance.SecondRotation();
            //     // print($"-=- RecordManager ChangeCamera, arFaceManager.isActiveAndEnabled");
            //     arFaceManager.enabled = false;
            //     
            //     // PlaneManager.Instance.SetARPlanesVisible(false);
                 ARHelper.Instance.StopPlaneDetection();
            // }
            // END.
        }
        else {
            // WorldPositionСorrection.Instance.FirstRotation();
            // print($"-=- RecordManager ChangeCamera ");
            // PlaneManager.Instance.SetARPlanesVisible(false);
            ARHelper.Instance.StopPlaneDetection();
            
            // #lightship-REMOVE_TEMPORARILY: No front-facing camera support in ARDK.
            // arFaceManager.enabled = true;
        }
    }

    public void ChangeCameraButton()
    {
        // #lightship-REMOVE_TEMPORARILY: No front-facing camera support in ARDK.
        // if (!arFaceManager.isActiveAndEnabled)
        // {
        //     ChangeCamera(false);
        // }
        // else
        // {
        //     ChangeCamera(true);
        // }
        // END.
    }

    public void ClearPaths(bool enableMic = true)
    {
        print($"-=- RecordManager ClearPaths()");


        photoPath = null;
        videoPath = null;
        filteredFilePath = null;
        selectedMatInList = 0;
        fromGallery = false;

        // focalPointButton.gameObject.SetActive(true);
        // showCroppingButton.SetActive(false);
        if (enableMic) {
            
            StartEnableMicrophone();
 
            SetFrameRate(frameRate);
        }
        else {
            SetFrameRate(oldFrameRate);
        }
    }

    void StopMicrophone()
    {
        microphoneSource.Stop();
        Microphone.End(null);
        print($"-=- RecordManager Microphone Stop");
    }

    public void StartEnableMicrophone() {
        StartCoroutine(StartMicrophone());
    }
    
    private string previousStatus;
    IEnumerator StartMicrophone()
    {
        print($"-=- RecordManager Microphone Start");

        previousStatus = status;
        status = "WaitMicrophone";
        ShowWaitMicrophonePanel(true);
        bool wasMicPermission = isMicrophonePermission;
        if(!isMicrophonePermission) {
            wasMicPermission = CheckMicPermission();
            StartCheckingMicrophonPermissions();
        
            // wait while we get permission
            bool startPreRecord = false;
            while (!startPreRecord) {
                if (recordMicrophone && isMicrophonePermission || !recordMicrophone) {
                    startPreRecord = true;
                }
                yield return null;
            }
        }
        
        if(startWithPhotoVideo || !wasMicPermission) {
            yield return new WaitForSeconds(0.35f);
        }
        
        microphoneSource.mute =
            microphoneSource.loop = true;
        microphoneSource.bypassEffects =
            microphoneSource.bypassListenerEffects = false;
        
        microphoneSource.clip = Microphone.Start(null, true, 111, AudioSettings.outputSampleRate);
        yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
        
        microphoneSource.Play();
        

        // if (!wasMicPermission) {
        //     yield return new WaitForSeconds(1.7f);
        // }
        
        if(startWithPhotoVideo || !wasMicPermission) {
            yield return new WaitForSeconds(0.5f);
            if (!wasMicPermission) {
                yield return new WaitForSeconds(0.2f);
            }
        }
        else {
            yield return new WaitForSeconds(0.2f);
        }

        PreRecord();
    }

    public void EnableMic(bool disable = true) {
        recordMicrophone = !disable;
    }

    public void ToggleSelectedView()
    {
        if (!startWithPhotoVideo)
        {
            if (focalPointButton.isOn)
            {
                ShowARContent();
            }
            else
            {
                HideARContent();
            }
        }
    }

    private int everythingCullingMask;
    public void HideARContent()
    {
        if(Camera.main.cullingMask != 0) {
            print($"-=- RecordManager HideARContent()");
            everythingCullingMask = Camera.main.cullingMask;
            Camera.main.cullingMask = 0;
            if (startWithPhotoVideo) {
                recordUiManager.Reset(false);
            }
        }
    }

    public void ShowARContent()
    {
        print($"-=- RecordManager ShowARContent() everythingCullingMask = {everythingCullingMask}");
        if (everythingCullingMask != 0)
        {
            Camera.main.cullingMask = everythingCullingMask;
        }
        focalPointButton.isOn = true;
    }

    public void ShowCroppingPage()
    {
        croppingPage.SetActive(true);
        // showCroppingButton.SetActive(false);
    }

    public void HideCroppingPage()
    {
        croppingData = selectedArea.CroppingData();
        print($"-=- croppingData = {croppingData}");
        croppingPage.SetActive(false);
        // showCroppingButton.SetActive(true);
    }

    private VideoPlayer fullscreenPlayer;
    private bool isShowingVideo;
    [HideInInspector] public bool isFullscreen;

    private void DoFullScreenForVideo(VideoPlayer vp, bool pathFromURL, int filterIndex = 0)
    {
        if (!pathFromURL)
        {
            photoPlaneForRecord.SetActive(true);
            isShowingVideo = true;
            vp.targetMaterialRenderer = photoPlaneForRecord.GetComponent<MeshRenderer>();
            vp.targetMaterialRenderer.material = filterMats[filterIndex];
            vp.targetMaterialProperty = "_MainTex";
            vp.Play();
            fullscreenPlayer = vp;
                
            //Set plane scale by content resolution
            Texture vidTex = vp.texture;
            float k = ((float)vidTex.height / vidTex.width) * (Screen.width / (float)Screen.height);
            photoPlaneForRecord.transform.localScale = new Vector3(1, k, 1);
            print($"-=- Fullscreen()3 vidTex.width = {vidTex.width}, vidTex.height = {vidTex.height}");

            return;
        }
       
        print($"RecordManager Width = {mediaPlayer.Info.GetVideoWidth()}\nHeight = {mediaPlayer.Info.GetVideoHeight()}");
                
        mediaPlayerFullscreen.GetComponent<ApplyToMesh>().Offset = Vector2.zero;
        mediaPlayerFullscreen.GetComponent<ApplyToMesh>().Scale = Vector2.one;
        mediaPlayerFullscreen.GetComponent<Renderer>().material = filterMats[filterIndex];
                
        Vector3 scale = Vector3.one;
        scale.y = (float)videoWidth / videoHeight;
        scale.y *= (float)mediaPlayer.Info.GetVideoHeight() / mediaPlayer.Info.GetVideoWidth();
        mediaPlayerFullscreen.transform.localScale = scale;
        mediaPlayerFullscreen.SetActive(true);
    }
    public void Fullscreen(VideoPlayer vp, bool isVideo, bool pathFromURL = false, int filterIndex = 0)
    {
        if (isVideo)
        {
            DoFullScreenForVideo(vp, pathFromURL, filterIndex);
        }
        else
        {
            photoPlaneForRecord.SetActive(true);
            isShowingVideo = false;
            var rend = photoPlaneForRecord.GetComponent<Renderer>();
            rend.material = filterMats[0];
            // Texture newText = vp.GetComponent<MeshRenderer>().material.GetTexture("_Albedo");
            Texture newText = vp.GetComponent<MeshRenderer>().material.GetTexture("_MainTex");
            print($"-=- Fullscreen isPhoto");
            rend.material.mainTexture = newText;
            
            // Change AR plane scale by photo resolution
            float k = ((float)newText.height / newText.width) * (Screen.width / (float)Screen.height);
            photoPlaneForRecord.transform.localScale = new Vector3(1, k, 1);
            print($"-=- Fullscreen() photo newText.width = {newText.width}, newText.height = {newText.height}");
        }
        filterCamera.depth = 2;
        exitFullscreenButton.SetActive(true);
        isFullscreen = true;
        
        OffScreenIndicatorManager.Instance.HideArrow();
        ActivityUIFacade.Instance.ShowActivityCompleteUI(false);
    }

    public void ExitFullscreen()
    {
        print("#debugcamerafrozen");
        filterCamera.depth = -2;
        isFullscreen = false;

        if (isShowingVideo)
        {
            fullscreenPlayer.targetMaterialRenderer = null;
            // fullscreenPlayer.targetMaterialProperty = "_Albedo";
            fullscreenPlayer.targetMaterialProperty = "_MainTex";
            StartCoroutine(ShowThumbnail());
            if (currPhotoVideoActivity != null) {
                currPhotoVideoActivity.StopAllClipLoader();
            }
            else if (currPhotoVideoActivityForConsume != null) {
                currPhotoVideoActivityForConsume.StopAllClipLoader();
            }
        }
        exitFullscreenButton.SetActive(false);
        photoPlaneForRecord.SetActive(false);
        mediaPlayerFullscreen.SetActive(false);
        
        OffScreenIndicatorManager.Instance.ShowArrow();
        ActivityUIFacade.Instance.ShowActivityCompleteUI(true);
    }

    public void ShowGallery()
    {
        PickImageOrVideo();
    }


    #endregion

    #region Make Video Segment

    public void StartVideoRecording()
    {
        if (status != "record")
        {
            isPhotoTaking = false;
            // Start recording
            var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
            var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
            clock = new RealtimeClock();
            recorder = new MP4Recorder(fileWidthSize, (int)(videoHeight * ((float)fileWidthSize / videoWidth)), frameRate, sampleRate, channelCount);
            // recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount);
            // Create recording inputs
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            audioInput = recordMicrophone ? new AudioInput(recorder, clock, microphoneSource, true) : null;
            // Unmute microphone
            microphoneSource.mute = audioInput == null;
            segmentLength = Time.time;
            status = "record";
            fromGallery = false;
        }
    }

    async void StopRecording()
    {
        // Mute microphone
        microphoneSource.mute = true;
        // Stop recording
        audioInput?.Dispose();
        cameraInput.Dispose();
        segmentPath = await recorder.FinishWriting();
        segmentPaths.Add(segmentPath);
        segmentLength -= Time.time;
        segmentLength = Mathf.Abs(segmentLength);
        segmentLengths.Add(segmentLength);
        
        if (status != "timeIsUp") return;
        
        // --- All segments is done, start to show video
        print("-=- RecordManager StopRecording()");
        StartShowVideoWithFilterFirstTime();
    }

    void SetFrameRate(int newFrameRate) {
        if(Application.targetFrameRate != newFrameRate) {
            Application.targetFrameRate = newFrameRate;
        }
    }
    
    #endregion

    #region Make Photo

    void GetPhoto()
    {
        print($"RecordManager GetPhoto() start");
        ResetSizePlaneForRecord();
        isPhotoTaking = true;
        recorder = new JPGRecorder(videoWidth, videoHeight);
        var clock = new RealtimeClock();
        cameraInput = new CameraInput(recorder, clock, Camera.main);
        StartCoroutine(StopPhotoSeq());
        fromGallery = false;
    }

    IEnumerator StopPhotoSeq()
    {
        yield return new WaitForSeconds(0.22f);
        StopPhotoAsynx();
        yield return null;
    }

    async void StopPhotoAsynx()
    {
        print($"1 - StopPhotoAsynx");
        cameraInput.Dispose();
        var path = await recorder.FinishWriting();
        photoPath = path + "/1.jpg";

        // --- remove other .JPGs
        print($"StopPhotoAsynx > remove other .JPGs");
        string[] files = Directory.GetFiles(path);
        foreach (var file in files)
        {
            if (file == path + "/1.jpg") continue;
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        // --- Save Texture2D for filter
        print($"StopPhotoAsynx > Save Texture2D for filter");
        var bytes = System.IO.File.ReadAllBytes(photoPath);
        originalPhotoTexture2D = new Texture2D(1, 1);
        originalPhotoTexture2D.LoadImage(bytes);
        status = "filterApplying";

        // --- Show Photo with filters on full screen from another camera
        print($"StopPhotoAsynx > Show Photo with filters on full screen from another camera");
        photoPlaneForRecord.SetActive(true);
        flashlight.isOn = true;
#if UNITY_IOS
        FlashlightController.Instance.TurnOff();
#endif
        recordUiManager.SetOriginalFilter();
        print($"StopPhotoAsynx > ShowImageWithFilter...");
        ShowImageWithFilter();
        print($"StopPhotoAsynx > ChangeCamera...");
        ChangeCamera(true);
        print($"StopPhotoAsynx > ChangeOrientation...");
        ChangeOrientation(false);

        // focalPointButton.gameObject.SetActive(false);
        // ShowARContent();
        // focalPointPage.SetActive(false);
        // showCroppingButton.SetActive(true);

        // If do not do this, the photo will not be recorded correctly.
        print($"StopPhotoAsynx > DelayForStopMicrophonAfterTakePhoto...");
        StartCoroutine(DelayForStopMicrophonAfterTakePhoto());
    }

    IEnumerator DelayForStopMicrophonAfterTakePhoto()
    {
        yield return new WaitForSeconds(0.1f);
        StopMicrophone();
        yield return null;
    }

    #endregion

    #region Show Photo On AR Panel

    //-----------------  Show Photo -----------------
    public void ShowPhoto(GameObject panel)
    {
        //print($"RecordManager.ShowPhoto @ {photoPath}");
        panel.SetActive(true);

        var photoWasShown = false;
        Texture2D tex = null;
        if (!filteredFilePath.IsNullOrWhiteSpace())
        {
            // Then let's load the photo from the user's local device.
            try
            {
                print($"RecordManager.ShowPhoto - loading from PhotoPathWithNewGUID(filteredFilePath): {PhotoPathWithNewGUID(filteredFilePath)}");
                var bytes = System.IO.File.ReadAllBytes(filteredFilePath);
                // var bytes = System.IO.File.ReadAllBytes(PhotoPathWithNewGUID(filteredFilePath));
                tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                DoShowPhoto(tex, panel);
                photoWasShown = true;
            }
            catch (Exception ex)
            {
                // This occurs if the user has deleted the photo already
                // or if the local path is wrong in the beginning.

                // ToDo: log this error and notify ourselves.
                print($"RecordManager.ShowPhoto - show photo failed, Exception: {ex.Message}");
            }
        }
        if (!contentPublicPath.IsNullOrWhiteSpace() && !photoWasShown)
        {
            print($"RecordManager.ShowPhoto - loading from contentPublicPath: {contentPublicPath}");
            StartCoroutine(DownloadPhoto(contentPublicPath, panel, DoShowPhoto));
        }
    }


    // public void FlyToPanelTransitionAnimation(GameObject panel)
    // {
    //     Camera cameraObj = Camera.main;
    //     Vector3 targetPos = panel.transform.position;
    //     panel.transform.position = cameraObj.transform.position; //+ (cameraObj.transform.forward * 0.1f); ?
    //
    //     //panel.transform.position = cameraObj.transform.position + (cameraObj.transform.forward * 0.3f);
    //     panel.transform.DOMove(targetPos, 2f).SetEase(Ease.OutQuint);
    // }


    public void FlyToPanelTransitionAnimation(GameObject panelObj)
    {
        print("-=- PhotoVideoActivity FlyToPanelTransitionAnimation()");

        Camera cameraObj = Camera.main;
        Vector3 destPos = panelObj.transform.position;
        Vector3 destRot = panelObj.transform.rotation.eulerAngles;
        Quaternion destQuater = panelObj.transform.rotation;

        //preparing the path
        float duration = 2f;
        Vector3 intermediatePoint = CalIntermediatePoint(cameraObj, panelObj, out duration);

        Vector3[] points = new Vector3[3];
        points[0] = cameraObj.transform.position;
        points[1] = intermediatePoint;
        points[2] = destPos;
        DG.Tweening.Plugins.Core.PathCore.Path path = new DG.Tweening.Plugins.Core.PathCore.Path(
            PathType.CatmullRom, points, 10, Const.FLY_TO_PANEL_PATH);

        //1. move to camera position
        panelObj.transform.position = cameraObj.transform.position;
        panelObj.transform.rotation = cameraObj.transform.rotation;
        panelObj.transform.LookAt(cameraObj.transform.position + (cameraObj.transform.forward * -1), cameraObj.transform.up); //revert

        //2. start moving along path
        Ease easeEffect = Ease.InQuart; //less: InOutCubic, more: InOutExpo, or OutQuart
        panelObj.transform.DOPath(path, duration, PathMode.Full3D).SetEase(easeEffect);
        panelObj.transform.DORotate(destRot, duration, RotateMode.Fast).SetEase(easeEffect);

        StartCoroutine(SaveButtonForFinishEffect(duration, panelObj));
    }

    IEnumerator SaveButtonForFinishEffect(float duration, GameObject panelObj)
    {
        yield return new WaitForSeconds(duration + 0.3f);
        currPhotoVideoActivity.EnableSaveButton();
        panelObj.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
        print($"-=- RecordManager SaveButton For Finish Effect");
        yield return null;
    }


    private Vector3 CalIntermediatePoint(Camera cameraObj, GameObject panel, out float duration)
    {
        //calculating the angle between the panel(expected on the activity board) and the camera forward
        float angle = Vector3.Angle(panel.transform.forward * -1, cameraObj.transform.forward);
        print(string.Format("The angle between 'camera' and 'panel on board' = {0}", angle));

        float distance = Vector3.Distance(panel.transform.position, cameraObj.transform.position);
        float intermediaDistance = 0;

        Vector3 intermediatePoint = cameraObj.transform.position; //by default it's at the camera's position

        if (distance <= 1)
        {
            if (angle <= 30) //if user is very close & with an small angle to the activity board
            {
                //do nothing to the intermediaPoint
                duration = 1f;
            }
            else //if user is very close but towards a big angle
            {
                intermediatePoint = cameraObj.transform.position + (cameraObj.transform.forward * 0.5f);
                duration = 1.5f;
            }
        }
        else //if user is far away
        {
            if (angle <= 45)
            {
                intermediaDistance = (float)(distance * 0.33);
                duration = (distance <= 3 ? 2f : distance / 1.5f);
            }
            else
            {
                intermediaDistance = (float)(distance * 0.66);
                duration = (distance <= 3 ? 2.25f : distance / 1.33f);
            }
            intermediaDistance = (intermediaDistance >= 1 ? 1f : intermediaDistance);
            duration = duration >= 5f ? 5f : duration;
            print(string.Format("Intermediate distance = '{0}', duration = '{1}'", intermediaDistance, duration));

            intermediatePoint = cameraObj.transform.position + (cameraObj.transform.forward * intermediaDistance);
        }

        return intermediatePoint;
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

    void DoShowPhoto(Texture2D texture, GameObject panel)
    {
        print("FROM AR: DoShowPhoto");
        Material newMat = new Material(Shader.Find("Photo Filters/Color Remap"));
        newMat.CopyPropertiesFromMaterial(photoPlaneMat);
        // newMat.SetTexture("_Albedo", texture);
        newMat.SetTexture("_MainTex", texture);
        if (texture != null)
            panel.GetComponent<Renderer>().material = newMat;
        SetARPanelByContentSize(panel, texture.width, texture.height);
        // ToDo: ELSE what do we want to do here?
        //  Perhaps we can show a default image or an empty frame?
    }

    public void SetCroppingPositionOnARPanel(GameObject panel)
    {

        // --- To Enable Cropping you need just recomment next 3 lines, and enable CroppingButton components in selected frame object in scene
        // if (croppingData == Vector3.zero) {
        // croppingData = new Vector3(0.5f,0.5f, 1);
        // }
        croppingData = new Vector3(0.5f, 0.5f, 1);

        // --- at first set the scale of panel
        RectTransform panelTr = panel.GetComponent<RectTransform>();
        // print($"-=- panelTr.localPosition = {panelTr.localPosition}");

        // --- init scale (like without cropping)
        float h = (float)videoHeight / (float)videoWidth;
        panelTr.localScale = new Vector3(486, h * 486, 1);
        // print($"-=- panelTr.localScale = {panelTr.localScale}");

        // --- apply scale
        panelTr.localScale = new Vector3(panelTr.localScale.x * croppingData.z, panelTr.localScale.y * croppingData.z, 1);


        // --- calculate and apply position of future center
        float x = panelTr.localScale.x * croppingData.x;
        float y = panelTr.localScale.y * croppingData.y;
        Vector3 translateVector = new Vector3(panelTr.localScale.x / 2 - x, panelTr.localScale.y / 2 - y, 0.0f);
        // print($"-=- calculate and apply position translateVector = {translateVector}");
        // panelTr.Translate(translateVector);
        panelTr.localPosition = new Vector3(panelTr.localPosition.x - translateVector.x,
            panelTr.localPosition.y + translateVector.y, 0);

        print($"-=- SetCroppingPositionOnARPanel panelTr.localPosition = {panelTr.localPosition}");
    }

    // new way to set AR Panel
    public void SetARPanelByContentSize(GameObject panel, int width, int hight) {
        RectTransform panelTr = panel.GetComponent<RectTransform>();
        float k = (float)hight / width;
        //Portrait
        if (k > 1) {
            panelTr.localScale = new Vector3(486, k * 486, 1);
        }
        //Landscape
        else {
            panelTr.localScale = new Vector3((1 / k) * 486, 486, 1);
        }
        print($"-=- SetARPanelByContentSize panelTr.localPosition = {panelTr.localPosition}, width = {width}, hight = {hight}");
        
    }

    #endregion

    #region Show Video On AR Panel
    //-----------------  Show Video -----------------

    private GameObject videoPlane;
    [HideInInspector] public PhotoVideoActivity currPhotoVideoActivity;
    [HideInInspector] public PhotoVideoActivityForConsume currPhotoVideoActivityForConsume;
    private Texture initPlayPanelTex;

    public void ShowLocalVideo(GameObject panel)
    {
        print($"-=- RecordManager ShowDownloadedVideo()");
        VideoPlayer vp = panel.GetComponent<VideoPlayer>();
        vp.url = videoPath;
    }

    public void ConfigureVideoElements(GameObject panel, GameObject activity, bool isCreating)
    {// Was ShowVideo
        print($"-=- RecordManager ConfigureVideoElements()");

        if (videoPlane == null)
        {
            videoPlane = panel;
            // initPlayPanelTex = videoPlane.GetComponent<MeshRenderer>().material.mainTexture;
            // initPlayPanelTex = videoPlane.GetComponent<MeshRenderer>().material.GetTexture("_Albedo");
        }
        videoPlane = panel;
        if (isCreating)
        {
            currPhotoVideoActivity = activity.GetComponent<PhotoVideoActivity>();
            currPhotoVideoActivityForConsume = null;
        }
        else
        {
            currPhotoVideoActivityForConsume = activity.GetComponent<PhotoVideoActivityForConsume>();
            currPhotoVideoActivity = null;
        }
    }

    public void ShowVideoOnPanel()
    {
        print($"videoPath={RecordManager.Instance.videoPath}");
        print($"contentPublicPath={RecordManager.Instance.contentPublicPath}");
        // if (filteredFilePath.IsNullOrWhiteSpace())
        // {
        //     filteredFilePath = RecordManager.Instance.videoPath.IsNullOrWhiteSpace()
        //         ? RecordManager.Instance.contentPublicPath
        //         : RecordManager.Instance.videoPath;
        // }

        if (videoPlane && !String.IsNullOrWhiteSpace(videoPath))
        {
            videoPlane.SetActive(true);

            if (isLocalPath(videoPath))
            {
                StartCoroutine(ShowThumbnail());

                VideoPlayer vp = videoPlane.GetComponent<VideoPlayer>();
                vp.url = videoPath;
                print($"ShowVideoOnPanel: isLocalPath(videoPath) = true");
            }

        }
        else if (videoPlane)
        {
            print($"ShowVideoOnPanel: else if (videoPlane)");
        }
    }

    void ShowVideoSegmentsOnPanel() {
        if (currPhotoVideoActivity) {
            print($"ShowVideoSegmentsOnPanel() currPhotoVideoActivity");
            videoPlane.SetActive(true);
            
            currPhotoVideoActivity.ConfigureSegments(segmentPaths, selectedMatInList);
            StartCoroutine(ShowThumbnail());
        }
    }
    
    public void ShowVideoByMediaPlayer(Renderer mesh, string path, PhotoVideoActivityForConsume consumeActiv,
        PhotoVideoActivity createActiv, bool isCreate, Texture tex)
    {
        if (lastAudioActivity != null)
        {
            mediaPlayerIsPause = false;
            lastAudioActivity = null;
        }
        else if (isCreate)
        {
            if (lastCreateVideoActivity != null && createActiv.GetActivityId() != lastCreateVideoActivity.GetActivityId())
            {
                print(
                    $"-=- RecordManager createActiv.GetActivityId() != lastCreateVideoActivity.GetActivityId()");
                StartCoroutine(lastCreateVideoActivity.LoadThumbnail(true));
                mediaPlayerIsPause = false;
                onBoard = null;
            }
        }
        else if (lastConsumeVideoActivity != null &&
                 consumeActiv.GetActivityId() != lastConsumeVideoActivity.GetActivityId())
        {
            print(
                $"-=- RecordManager lastConsumeVideoActivity.GetActivityId() != lastConsumeVideoActivity.GetActivityId()");
            StartCoroutine(lastConsumeVideoActivity.LoadThumbnail(true));
            mediaPlayerIsPause = false;
            onBoard = null;
        }

        lastConsumeVideoActivity = consumeActiv;
        lastCreateVideoActivity = createActiv;
        string cloudPath = CorrectedCloudflarePath(path);
        print($"-=- RecordManager ShowVideoByMediaPlayer, cloudPath = {cloudPath}");
        print($"-=- RecordManager ShowVideoByMediaPlayer, mediaPlayer.MediaReference.MediaPath.Path = {mediaPlayer.MediaReference.MediaPath.Path}");

        if (!mediaPlayerIsPause)
        {
            // need to call it 
            // mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, cloudPath, true);
            MediaReference mediaReference = new MediaReference()
            {
                MediaPath = new MediaPath(cloudPath, MediaPathType.AbsolutePathOrURL)
            };
            mediaPlayer.OpenMedia(mediaReference, true);
            applyToMesh.MeshRenderer = mesh;
            // applyToMesh.TexturePropertyName = "_Albedo";
            applyToMesh.TexturePropertyName = "_MainTex";

            if (isCreate && onBoard == null)
            {
                createActiv.ShowVideoLoader();
            }
            else if(consumeActiv != null)
            {
                consumeActiv.ShowVideoLoader();
            }
        }
        else
        {
            mediaPlayer.Control.Play();
        }
    }

    public void Pause()
    {
        print($"-=- RecordManager Pause()");
        mediaPlayerIsPause = true;
        mediaPlayerIsPlaing = false;
        mediaPlayer.Control.Pause();
    }

    IEnumerator ShowThumbnail()
    {
        yield return new WaitForSeconds(0.13f);
        VideoPlayer vp = videoPlane.GetComponent<VideoPlayer>();
        vp.Play();
        
        while (vp.texture == null) {
            yield return null;
        }
        Texture vidTex = vp.texture;
        SetARPanelByContentSize(videoPlane, vidTex.width, vidTex.height);
        // yield return new WaitForSeconds(0.13f);
        vp.Pause();
        currPhotoVideoActivity.ShowPlayButtons();
        
        print($"-=- ShowThumbnail finish");
        yield return null;
    }

    public void ShowThumbnail(GameObject contentBoard)
    {
        videoPlane = contentBoard;
        StartCoroutine(ShowThumbnail());
    }

    // --- scale showing plane as a image proportion base on width

    void ScalePanelByWidth(GameObject panel)
    {
        float h = videoHeight;
        float w = videoWidth;
        float rootW = panel.transform.localScale.x;
        float rootZ = panel.transform.localScale.z;
        float k = h / w;

        panel.transform.localScale = new Vector3(rootW, rootW * k, rootZ);
    }

    // private void ScaleMaskTexture(GameObject panel) {
    //     Renderer rend = panel.GetComponent<Renderer> ();
    //     float k = videoHeight / videoWidth;
    //     float yScale = 0.5f * k;
    //
    //     rend.material.SetTextureScale("Culling Mask", new Vector2(1, yScale));
    //     float yOffset = (1 - yScale)/2;
    //
    //     rend.material.SetTextureOffset("Culling Mask", new Vector2(0, yOffset));
    // }

    public Texture2D Screenshot()
    {
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        tex.Apply();
        return tex;
    }

    public void PreLoadVideo(string path) {
        mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, CorrectedCloudflarePath(path), false);
    }

    public void PlayIntro(Renderer mesh) {
        mesh.GetComponent<ApplyToMaterial>().Player = mediaPlayer;
        mediaPlayer.Control.Play();
    }


    #endregion

    #region Show Photo/Video with filters

    private int mergeringClipStart;
    private float previousClipLength;
    private int clipNumber;
    private bool lastSection;
    private float realStartPlayTime;

    void StartShowVideoWithFilterFirstTime()
    {
        status = "filterApplying";
        photoPlaneForRecord.SetActive(false);
        flashlight.isOn = true;
#if UNITY_IOS
        FlashlightController.Instance.TurnOff();
#endif
        recordUiManager.SetOriginalFilter();
        StopMicrophone();

        // if (fromGallery) {
        //     
        //     ShowImageWithFilter();
        // }
        if (segmentPaths.Count == 1 || fromGallery)
        {
            // ShowImageWithFilter();
            ShowLoopVideo(segmentPaths[0]);
            ShowImageWithFilter();
        }
        else
        {
            // ShowImageWithFilter();
            StartPlayMergeredClip();
            ShowImageWithFilter();
        }

        ChangeCamera(true);
        ChangeOrientation(false);
        // focalPointButton.gameObject.SetActive(false);
        // ShowARContent();
        // focalPointPage.SetActive(false);
        // showCroppingButton.SetActive(true);
        
        if (status != "recordVideoWithFilter" && filterCamera.depth < 0 && status != "")
        {
            recordUiManager.FilterApplying();
            filterCamera.depth = 1;
        }
    }

    public void ChangeFilter(int filterNumber)
    {
        selectedMatInList = filterNumber;
        ShowImageWithFilter();
    }

    private bool fromGallery = false;
    // --- Show Photo or Video With Filter on panel or something else
    void ShowImageWithFilter()
    {
        if (isPhotoTaking)
        {
            var rend = photoPlaneForRecord.GetComponent<Renderer>();
            rend.material = filterMats[selectedMatInList];
            rend.material.mainTexture = originalPhotoTexture2D;
            if (fromGallery)
            {
                // CorrectImageDimension(rend.material.mainTexture.height, rend.material.mainTexture.width);
                CorrectImageDimension(rend.material.mainTexture.width, rend.material.mainTexture.height);
            }

            if (filterCamera.depth < 0)
            {
                recordUiManager.FilterApplying();
                filterCamera.depth = 1;
            }
        }
        else
        {
            for (int i = 0; i < recordFilterVideoPlayers.Length; i++)
            {
                Renderer rend = recordFilterVideoPlayers[i].targetMaterialRenderer.GetComponent<Renderer>();
                rend.material = filterMats[selectedMatInList];
            }

            if (fromGallery)
            {
                StartCoroutine(WaitVideoPlayerData());
            }
        }
    }

    IEnumerator WaitVideoPlayerData()
    {
        print($"-=- WaitVideoPlayerData() start");
        while ((int)recordFilterVideoPlayers[0].width == 0)
        {
            yield return null;
        }
        CorrectImageDimension((int)recordFilterVideoPlayers[0].width,
            (int)recordFilterVideoPlayers[0].height);
    }

    // --- Start showing video from segments
    void StartPlayMergeredClip()
    {
        print(
            $"-=- RecordManager StartPlayMergeredClip() segmentPaths.Count = {segmentPaths.Count}");
        mergeringClipStart = 0;
        clipNumber = 0;
        lastSection = false;

        if (recordFilterVideoPlayers.Count() > 0)
        {
            recordFilterVideoPlayers[0].isLooping = false;
            recordFilterVideoPlayers[0].targetMaterialRenderer.gameObject.SetActive(true);
            recordFilterVideoPlayers[0].targetMaterialRenderer.transform.localPosition = new Vector3(0, 0, 1);

            if (recordFilterVideoPlayers.Count() > 1)
            {
                recordFilterVideoPlayers[1].targetMaterialRenderer.gameObject.SetActive(true);
                recordFilterVideoPlayers[1].targetMaterialRenderer.transform.localPosition = new Vector3(0, 0, 2);
            }            

            recordFilterVideoPlayers[0].url = segmentPaths[0];
            recordFilterVideoPlayers[0].Play();

            clipNumber++;
        }        
    }

    // ------- Called when clip finish  (Finish call) -------
    void PlayNewClip(UnityEngine.Video.VideoPlayer calledVP)
    {
        if (segmentPaths.Count != 1)
        {
            print($"-=- PlayNewClip Time.time = {Time.time}, clipNumber = {clipNumber}");
            if (clipNumber == segmentPaths.Count + 1)
            {
                clipNumber = 0;
                lastSection = true;
            }
            // --- Stop Showing, this is the end
            if (lastSection)
            {
                print($"-=- Finish showing video");
                if (status != "recordVideoWithFilter" && status != "")
                {
                    StartPlayMergeredClip();
                }
                else if (status == "recordVideoWithFilter")
                {
                    FinishCreateVideoWithFilter();
                }
                else if (status == "")
                {
                    recordFilterVideoPlayers[0].Stop();
                }
            }
            else
            {
                foreach (var videoPlayer in recordFilterVideoPlayers)
                {
                    // --- Next clip
                    if (videoPlayer != calledVP)
                    {
                        videoPlayer.targetMaterialRenderer.transform.localPosition = new Vector3(0, 0, 1);
                        if (clipNumber != 0)
                        {
                            previousClipLength = segmentLengths[clipNumber - 1];
                        }
                    }
                }

                foreach (var videoPlayer in recordFilterVideoPlayers)
                {
                    // --- After Next clip
                    if (videoPlayer == calledVP)
                    {
                        if (clipNumber < segmentPaths.Count)
                        {
                            videoPlayer.url = segmentPaths[clipNumber];
                            videoPlayer.Play();
                        }

                        videoPlayer.targetMaterialRenderer.transform.localPosition = new Vector3(0, 0, 2);
                        videoPlayer.targetMaterialRenderer.gameObject.SetActive(false);
                    }
                }

                clipNumber++;
            }
        }
        else if (status == "recordVideoWithFilter")
        {
            FinishCreateVideoWithFilter();
        }
    }

    // ------- Called when next clip ready to start (First call) -------
    void PauseClip(UnityEngine.Video.VideoPlayer calledVP)
    {
        if (segmentPaths.Count != 1)
        {
            print($"-=- PauseClip Time.time = {Time.time}");

            // --- first video
            if (mergeringClipStart == 0)
            {
                mergeringClipStart++;
                // --- start second video
                recordFilterVideoPlayers[1].url = segmentPaths[1];
                recordFilterVideoPlayers[1].Play();
                realStartPlayTime = Time.time;

                StartCoroutine(StartChangingNewClip(recordFilterVideoPlayers[1], segmentLengths[clipNumber - 1]));
                clipNumber++;

                // todo here we can start record mergered video


            }
            // --- second video
            else if (mergeringClipStart == 1)
            {
                calledVP.Pause();
                mergeringClipStart++;
            }
            else
            {
                calledVP.Pause();
                foreach (var videoPlayer in recordFilterVideoPlayers)
                {
                    if (videoPlayer == calledVP && !lastSection)
                    {
                        StartCoroutine(StartChangingNewClip(videoPlayer, previousClipLength));
                    }
                }
            }

        }
    }

    // ------- It start play next video on hidden video player -------
    IEnumerator StartChangingNewClip(VideoPlayer videoPlayer, float previousClipL)
    {

        // --- it is time when current clip has started to playing
        float time = previousClipL - 0.4f + realStartPlayTime;
        while (time > Time.time)
        {
            yield return null;
        }

        if (status == "pause" || status == "record" || status == "")
        {
            print($"-=- After IEnumerator status = {status}");
            yield break;
        }

        if (!isRecordingMergeVideo) {
            // --- it is time minus next clip's delay from when current clip will have to finish
            videoPlayer.targetMaterialRenderer.gameObject.SetActive(true);
            print($"-=- After IEnumerator Time.time = {Time.time}");
            realStartPlayTime = Time.time;
            videoPlayer.Play();
            yield return null;
        }
    }

    // --- Here we start show one .mp4 as a loop
    void ShowLoopVideo(string path)
    {
        status = "filterApplying";
        recordFilterVideoPlayers[0].targetMaterialRenderer.gameObject.SetActive(true);
        recordFilterVideoPlayers[0].Stop();
        recordFilterVideoPlayers[1].targetMaterialRenderer.gameObject.SetActive(false);

        recordFilterVideoPlayers[0].url = path;
        recordFilterVideoPlayers[0].Play();
        recordFilterVideoPlayers[0].isLooping = true;
        recordFilterVideoPlayers[0].targetMaterialRenderer.transform.rotation = Quaternion.identity;
    }

    #endregion

    #region Make Video with filter (and without filter too)

    // --- call after press 'Next' or 'Filter' button
    void SaveFileWithFilter()
    {
        filterCamera.depth = -2;
        filteredFilePath = null;

        if (isPhotoTaking)
        {
            print($"-=- SaveFileWithFilter() Photo Taking");
            SavePhotoWithFilter();
        }
        // Video
        else
        {
            // Set duration of loader
            float neededTime = 0;
            foreach (var segment in segmentLengths) {
                neededTime += segment;
            }
            StartCoroutine(ApplyFilterLoader(neededTime + segmentPaths.Count * 0.2f + 0.7f));

            print($"-=- SaveFileWithFilter() Video Taking");
            status = "recordVideoWithFilter";

            MergeAllSegments();
        }
    }

    void NoApplyFilterFinish() {
        // isRecorded = false;
        // if (segmentPaths.Count == 1) {
        //     if(segmentLengths.Count > 0) {
        //         recordUiManager.DeleteSegment(segmentLengths[0]);
        //     }
        //     segmentPaths.Clear();
        //     segmentLengths.Clear();
        //     segmentPath = null;
        //     segmentLength = 0;
        // }
        // else {
        //     DeleteAllSegmentsData();
        // }
        // segmentPaths.Clear();
        // status = "";
        // ResetSizePlaneForRecord();
        //
        // recordFilterVideoPlayers[0].Stop();
        // recordFilterVideoPlayers[1].Stop();
        // // interactionManager.BeforeOnVideoTaken();
        // // --- Check if AR session was started with Photo/Video taking
        // if (!startWithPhotoVideo)
        // {
        //     interactionManager.BeforeOnVideoTaken();
        //     interactionManager.OnVideoTaken();
        //     recordUiManager.Reset(false);
        // }
        // ArrowForMoveDeviceManager.Instance.ShowArrow();
        // SetFrameRate(oldFrameRate);
        // fromGallery = false;
    }
    
    // --- called by videoPlayer
    private bool isRecorded;
    void StartCreateVideoWithFilter(VideoPlayer vp)
    {
        // if (status != "recordVideoWithFilter" && filterCamera.depth < 0 && status != "")
        // {
        //     recordUiManager.FilterApplying();
        //     filterCamera.depth = 1;
        // }
        // if (status == "recordVideoWithFilter" && !isRecorded)
        // {
        //     print($"-=- StartCreateVideoWithFilter");
        //     isRecorded = true;
        //     StartCoroutine(TimeRecordOneClip());
        //     if(!startWithPhotoVideo) {
        //         recordUiManager.Reset(false);
        //     }
        //     interactionManager.BeforeOnVideoTaken();
        //     UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        //     ArrowForMoveDeviceManager.Instance.ShowArrow();
        // }
    }

    IEnumerator TimeRecordOneClip() {
        // // --- if we record video from gallery we have to spend more time for start
        // if (segmentLengths.Count > 0)
        // {
        //     yield return new WaitForSeconds(0.1f);
        // }
        // else
        // {
        //     yield return new WaitForSeconds(0.33f);
        // }
        // print($"-=- TimeForRecordOneClip()");
        // var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
        // var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
        // var clock = new RealtimeClock();
        // recorder = new MP4Recorder(fileWidthSize, (int)(videoHeight * ((float)fileWidthSize / videoWidth)), frameRate, sampleRate, channelCount);
        // // recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount);
        // cameraInput = new CameraInput(recorder, clock, filterCamera);
        // audioInput = new AudioInput(recorder, clock, audioSourcesForMergeContent, true);
        yield return null;
    }

    async void FinishCreateVideoWithFilter()
    {
        // microphoneSource.mute = true;
        // audioInput?.Dispose();
        // cameraInput.Dispose();
        // var path = await recorder.FinishWriting();
        // isRecorded = false;
        //
        // if (String.IsNullOrEmpty(filteredFilePath))
        // {
        //     filteredFilePath = path;
        //     print($"-=- Finish record filtered video, segmentPaths.Count = {segmentPaths.Count}");
        //     if (segmentPaths.Count != 1)
        //     {
        //         // --- if NO need apply filters
        //         if (selectedMatInList == 0) {
        //             videoPath = path;
        //             DeleteAllSegmentsData();
        //             NoApplyFilterFinish();
        //         }
        //         // --- Start Play video again
        //         else {
        //             selectedMatInList = 0;
        //             ShowImageWithFilter();
        //             StartPlayMergeredClip();
        //         }
        //     }
        //     else
        //     {
        //         recordFilterVideoPlayers[0].Stop();
        //         if (segmentLengths.Count > 0)
        //         {
        //             recordUiManager.DeleteSegment(segmentLengths[0]);
        //         }
        //         segmentPaths = new List<string>();
        //         segmentLengths = new List<float>();
        //         segmentPath = null;
        //         segmentLength = 0;
        //         status = "";
        //         ResetSizePlaneForRecord();
        //         
        //         // check if panel was bornin
        //         bool wasStartWithPhotoVideo = false;
        //         GameObject currentAnchorObj = AnchorManager.Instance.GetCurrentAnchorObject();
        //         if (currentAnchorObj != null) {
        //             wasStartWithPhotoVideo = currentAnchorObj.GetComponentInChildren<ActivityManager>().wasStartWithPhotoVideo;
        //         }
        //
        //         if (!startWithPhotoVideo || wasStartWithPhotoVideo)
        //         {
        //             if (wasStartWithPhotoVideo) {
        //                 await Task.Delay(1000);
        //             }
        //             interactionManager.OnVideoTaken();
        //         }
        //         SetFrameRate(oldFrameRate);
        //         fromGallery = false;
        //     }
        // }
        // else
        // {
        //     print($"-=- Finish record original video");
        //     videoPath = path;
        //     DeleteAllSegmentsData();
        //     status = "";
        //
        //     // --- Check if AR session was started with Photo/Video taking
        //     if (!startWithPhotoVideo)
        //     {
        //         interactionManager.OnVideoTaken();
        //     }
        // }
    }

    
    // -------- new video segment merging with filter --------

    private bool isRecordingMergeVideo;
    private int currSegmentNum = 0;
    
    [Header("Content Recorder")]
    [SerializeField] private Camera origMergeCamera;
    [SerializeField] private Camera filterMergeCamera;
    
    private IMediaRecorder origRecorder;
    private IMediaRecorder filterRecorder;
    private CameraInput origCameraInput;
    private CameraInput filterCameraInput;
    private AudioInput origAudioInput;
    private AudioInput filterAudioInput;
    private AudioSource mergeAudioSource;

    private Transform origPlane;
    private Transform filterPlane;
    
    // call first and every new clip
    void MergeAllSegments() {
        currSegmentNum = 0;
        isRecordingMergeVideo = true;
        mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, segmentPaths[currSegmentNum]);
        mediaPlayer.Control.Play();
        filterMergeCamera.GetComponentInChildren<Renderer>().material = filterMats[selectedMatInList];

        origPlane.localScale = new Vector3(1, (float)videoHeight / videoWidth, 1);
        filterPlane.localScale = new Vector3(1, (float)videoHeight / videoWidth, 1);

        // hide other players
        recordFilterVideoPlayers[0].Stop();
        // recordFilterVideoPlayers[0].enabled = false;
        recordFilterVideoPlayers[1].Stop();
        // recordFilterVideoPlayers[1].enabled = false;
        
        if(!startWithPhotoVideo) {
            recordUiManager.Reset(false);
            interactionManager.BeforeOnVideoTaken();
        }
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        OffScreenIndicatorManager.Instance.ShowArrow();
        print($"-=- RecordManager MergeAllSegments() segmentLengths.Count = {segmentLengths.Count}");
    }
    
    IEnumerator StartRecordBothVideos(int frameWidth = 0, int frameHeight = 0) {
        yield return new WaitForSeconds(segmentLengths[currSegmentNum] * 0.045f);
        
        var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
        var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
        clock = new RealtimeClock();
        
        // All screen with limits
        // if (!landscapeOrient) {
        //     origRecorder = new MP4Recorder(fileWidthSize, (int)(videoHeight * ((float)fileWidthSize / videoWidth)), frameRate, sampleRate, channelCount, bitrate);
        //     filterRecorder = new MP4Recorder(fileWidthSize, (int)(videoHeight * ((float)fileWidthSize / videoWidth)), frameRate, sampleRate, channelCount, bitrate);
        // }
        // else {
        //     origRecorder = new MP4Recorder((int)(videoHeight * ((float)fileWidthSize / videoWidth)), fileWidthSize, frameRate, sampleRate, channelCount, bitrate);
        //     filterRecorder = new MP4Recorder((int)(videoHeight * ((float)fileWidthSize / videoWidth)), fileWidthSize, frameRate, sampleRate, channelCount, bitrate);
        // }

        print($"-=- RecordManager StartRecordBothVideos 0, frameWidth = {frameWidth}, frameHeight = {frameHeight}, landscapeOrient = {landscapeOrient}, fromGallery = {fromGallery}");
        if(fromGallery) {
            float rotate = NativeGallery.GetVideoProperties(segmentPaths[0]).rotation;
            float f = 0;
            if (rotate != 0 && rotate != 180) {
                print($"StartRecordBothVideos 1 rotate = {rotate}");
                int temp = frameWidth;
                frameWidth = frameHeight;
                frameHeight = temp;
                RotateFilteredPlanes(-1 * rotate);
                rotateDueCuttingContent = -1 * rotate;
                origPlane.localScale = new Vector3((float)frameHeight / frameWidth, 1, 1);
                filterPlane.localScale = new Vector3((float)frameHeight / frameWidth, 1, 1);
                f = landscapeOrient ? 2 : (2 * (3 / 4.0f));
            }
            else if (landscapeOrient) {
                origPlane.localScale = new Vector3(frameWidth/(float)frameHeight, 1, 1);
                filterPlane.localScale = new Vector3(frameWidth/(float)frameHeight, 1, 1);
                f = 2;
            }
            else {
                origPlane.localScale = new Vector3(1, (float)frameHeight / frameWidth, 1);
                filterPlane.localScale = new Vector3(1, (float)frameHeight / frameWidth, 1);
                f = 2 * (3 / 4.0f);
            }
            
            origPlane.localScale *= f;
            // filterPlane.localScale *= f;
            print($"-=- RecordManager StartRecordBothVideos, origPlane.localScale = {origPlane.localScale}");
        }
        
        // 3/4
        if (!landscapeOrient) {
            origRecorder = new MP4Recorder(frameWidth, (frameWidth / 3) * 4, frameRate, sampleRate, channelCount, bitrate);
            filterRecorder = new MP4Recorder(frameWidth, (frameWidth / 3) * 4, frameRate, sampleRate, channelCount, bitrate);
        }
        else {
            origRecorder = new MP4Recorder((frameHeight / 3) * 4, frameHeight, frameRate, sampleRate, channelCount, bitrate);
            filterRecorder = new MP4Recorder((frameHeight / 3) * 4, frameHeight, frameRate, sampleRate, channelCount, bitrate);
        }
        if(!fromGallery) {
            ChangeScaleRecordPlanesByOrientation3l4();
        }
        
        // Square
        // origRecorder = new MP4Recorder(videoWidth, videoWidth, frameRate, sampleRate, channelCount, bitrate);
        // filterRecorder = new MP4Recorder(videoWidth, videoWidth, frameRate, sampleRate, channelCount, bitrate);

        // ChangeScaleRecordPlanesByOrientationSquare();
        
        print($"-=- RecordManager StartRecordBothVideos1 origPlane.localScale = {origPlane.localScale.ToString()}");
        origCameraInput = new CameraInput(origRecorder, clock, origMergeCamera);
        filterCameraInput = new CameraInput(filterRecorder, clock, filterMergeCamera);
        // origAudioInput = new AudioInput(origRecorder, clock, mergeAudioSource, true);
        origAudioInput = new AudioInput(origRecorder, clock, mergeAudioSource, false);
        filterAudioInput = new AudioInput(filterRecorder, clock, mergeAudioSource, true);
    }

    IEnumerator ResumeRecording() {
        yield return new WaitForSeconds(segmentLengths[currSegmentNum] * 0.045f);
        
        clock.Paused = false;
        origCameraInput = new CameraInput(origRecorder, clock, origMergeCamera);      
        filterCameraInput = new CameraInput(filterRecorder, clock, filterMergeCamera);  
        // origAudioInput = new AudioInput(origRecorder, clock, mergeAudioSource, true);
        origAudioInput = new AudioInput(origRecorder, clock, mergeAudioSource, false);
        filterAudioInput = new AudioInput(filterRecorder, clock, mergeAudioSource, true);
    }

    void PauseRecording () {
        clock.Paused = true;
        origCameraInput.Dispose();
        filterCameraInput.Dispose();
        origAudioInput?.Dispose();
        filterAudioInput?.Dispose();
        
        currSegmentNum++;
        mediaPlayer.OpenMedia(MediaPathType.RelativeToStreamingAssetsFolder, segmentPaths[currSegmentNum]);
        mediaPlayer.Control.Play();
    }
    
    async void StopRecordBothVideos() {
        origAudioInput?.Dispose();
        filterAudioInput?.Dispose();
        origCameraInput.Dispose();
        filterCameraInput.Dispose();
        
        videoPath = await origRecorder.FinishWriting();
        filteredFilePath = await filterRecorder.FinishWriting();
        
        isRecordingMergeVideo = false;

        if (landscapeOrient && !fromGallery) {
            RotateFilteredPlanes(-90);
        }
        else if(fromGallery) {
            RotateFilteredPlanes(-1 * rotateDueCuttingContent);
        }
        rotateDueCuttingContent = 0;

        print($"StopRecordBothVideos() videoPath = {videoPath}");
        print($"StopRecordBothVideos() filteredFilePath = {filteredFilePath}");
        
        // DeleteAllSegmentsData();
        status = "";
        ResetSizePlaneForRecord();

        // check if panel was bornin
        bool wasStartWithPhotoVideo = false;
        GameObject currentAnchorObj = ActiveAnchorManager.GetCurrentAnchorObject();
        if (currentAnchorObj != null) {
            wasStartWithPhotoVideo = currentAnchorObj.GetComponentInChildren<ActivityManager>().wasStartWithPhotoVideo;
        }
        
        if (!startWithPhotoVideo || wasStartWithPhotoVideo)
        {
            if (wasStartWithPhotoVideo) {
                await Task.Delay(1000);
            }
            // interactionManager.BeforeOnVideoTaken();
            // interactionManager.OnVideoTaken();
            recordUiManager.Reset(false);
        }
        OffScreenIndicatorManager.Instance.ShowArrow();
        SetFrameRate(oldFrameRate);
        fromGallery = false;
        
        if (currPhotoVideoActivity) {
            currPhotoVideoActivity.EnableSaveButton();
        }
    }

    private bool filterLoaderStartWithVideo;
    IEnumerator ApplyFilterLoader(float neededTime) {
        if(currPhotoVideoActivity != null) {
            filterLoaderStartWithVideo = false;
            currPhotoVideoActivity.waitLoader.gameObject.SetActive(true);
        }
        else {
            filterLoaderStartWithVideo = true;
        }

        for (float f = 0; f < 1; f += Time.deltaTime / neededTime) {
            if (status == "" && !filterLoaderStartWithVideo) {
                currPhotoVideoActivity.waitLoader.gameObject.SetActive(false);
                yield break;
            }

            if (filterLoaderStartWithVideo) {
                if (currPhotoVideoActivity != null) {
                    currPhotoVideoActivity.waitLoader.gameObject.SetActive(true);
                    currPhotoVideoActivity.waitLoader.value = f;
                    filterLoaderStartWithVideo = false;
                }
            }
            else {
                currPhotoVideoActivity.waitLoader.value = f;
            }
            
            yield return null;
        }
        if(!filterLoaderStartWithVideo) {
            currPhotoVideoActivity.waitLoader.gameObject.SetActive(false);
        }
    }

    void RotateFilteredPlanes(float degree) {
        print($"RotateFilteredPlanes degree = {degree}");
        origPlane.transform.Rotate(new Vector3(0,0,degree)); 
        filterPlane.transform.Rotate(new Vector3(0,0,degree)); 
    }
    

    #endregion
    
    #region Make and crop Photo with filter

    private float rotateDueCuttingContent = 0;
    
    void SavePhotoWithFilter()
    {
        Renderer fRent = filterPlane.GetComponent<Renderer>();
        fRent.material = filterMats[selectedMatInList];
        fRent.material.mainTexture = originalPhotoTexture2D;
        origPlane.GetComponent<Renderer>().material.mainTexture = originalPhotoTexture2D;
        
        float w = originalPhotoTexture2D.width;
        float h = originalPhotoTexture2D.height;
        
        print($"-=- RecordManager SavePhotoWithFilter, originalPhotoTexture2D.width = {originalPhotoTexture2D.width}, originalPhotoTexture2D.height = {originalPhotoTexture2D.height}, landscapeOrient = {landscapeOrient}, fromGallery = {fromGallery}");

        if (!fromGallery) {
            origPlane.localScale = new Vector3(1, (float)videoHeight / videoWidth, 1);
            filterPlane.localScale = new Vector3(1, (float)videoHeight / videoWidth, 1);
        }
        else {
            float f = 0;
            float rotate = NativeGallery.GetVideoProperties(photoPath).rotation;
            if (rotate != 0 && rotate != 180) {
                print($"SavePhotoWithFilter 1 rotate = {rotate}");
                float temp = w;
                w = h;
                h = temp;
                
                RotateFilteredPlanes(-1 * rotate);
                rotateDueCuttingContent = -1 * rotate;
                origPlane.localScale = new Vector3(h / w, 1, 1);
                filterPlane.localScale = new Vector3(h / w, 1, 1);
                f = landscapeOrient ? 2 : (2 * (3 / 4.0f));
            }
            else if (landscapeOrient) {
                origPlane.localScale = new Vector3(w/h, 1, 1);
                filterPlane.localScale = new Vector3(w/h, 1, 1);
                f = 2;
            }
            else {
                origPlane.localScale = new Vector3(1, h / w, 1);
                filterPlane.localScale = new Vector3(1, h / w, 1);
                f = 2 * (3 / 4.0f);
            }
            
            origPlane.localScale *= f;
            filterPlane.localScale *= f;
        }
        
        // 3/4
        if(!landscapeOrient) {
            origRecorder = new JPGRecorder((int)w, ((int)w / 3) * 4);
            filterRecorder = new JPGRecorder((int)w, ((int)w / 3) * 4);
        }
        else {
            origRecorder = new JPGRecorder(((int)h / 3) * 4, (int)h);
            filterRecorder = new JPGRecorder(((int)h / 3) * 4, (int)h);
        }
        if(!fromGallery) {
            ChangeScaleRecordPlanesByOrientation3l4();
        }
        
        // Square format 
        // origRecorder = new JPGRecorder(videoWidth, videoWidth);
        // filterRecorder = new JPGRecorder(videoWidth, videoWidth);
        // ChangeScaleRecordPlanesByOrientationSquare();
        print($"SavePhotoWithFilter()3 origPlane.localScale = {origPlane.localScale}, origPlane.position = {origPlane.position}");

        var clock = new RealtimeClock();
        origCameraInput = new CameraInput(origRecorder, clock, origMergeCamera);
        filterCameraInput = new CameraInput(filterRecorder, clock, filterMergeCamera);
        StartCoroutine(StopPhotoSeqWithFilter());
    }

    IEnumerator StopPhotoSeqWithFilter()
    {
        yield return new WaitForSeconds(0.25f);
        FinishPhotoCreatingAsynxWithFilter();
        yield return null;
    }

    async void FinishPhotoCreatingAsynxWithFilter()
    {
        origCameraInput.Dispose();
        filterCameraInput.Dispose();
        string origPath = photoPath;
        
        var originalPaths = await origRecorder.FinishWriting();
        photoPath = originalPaths + "/1.jpg";
        print($"FinishPhotoCreatingAsynxWithFilter()1 photoPath = {photoPath}");
        var filterdPaths = await filterRecorder.FinishWriting();
        filteredFilePath = filterdPaths + "/1.jpg";
        print($"FinishPhotoCreatingAsynxWithFilter()1 filteredFilePath = {filteredFilePath}");
        
        string[] files = Directory.GetFiles(originalPaths);
        foreach (var file in files) {
            if (file != photoPath) {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }
        files = Directory.GetFiles(filterdPaths);
        foreach (var file in files) {
            if (file != filteredFilePath) {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }
        if (Directory.Exists(origPath.Remove(origPath.Length - 6))) {
            try {
                Directory.Delete(origPath.Remove(origPath.Length - 6), true);
            }
            catch (Exception ex) {
                print($"Delete origPath Exception: {ex.Message}");
            }
        }
        
        if (landscapeOrient && !fromGallery) {
            RotateFilteredPlanes(-90);
        }
        else if(fromGallery) {
            RotateFilteredPlanes(-1 * rotateDueCuttingContent);
        }
        rotateDueCuttingContent = 0;

        recordUiManager.Reset(false);
        // interactionManager.OnPhotoTaken(filteredFilePath);
        status = "";
        // We are deprecating this --> SavePath(photoPath);
        if (onBoardChallenge == null)
        {
            // --- Check if AR session was started with Photo/Video taking
            if (!startWithPhotoVideo && currPhotoVideoActivityForConsume == null)
            {
                interactionManager.OnPhotoTaken(filteredFilePath);
            }

            if (currPhotoVideoActivityForConsume != null)
            {
                currPhotoVideoActivityForConsume.OnSubmit();
                // currPhotoVideoActivityForConsume.GetPhotoKeywords(filteredFilePath);
            }
        }
        else
        {
            onBoardChallenge.ShowPhoto();
        }
        
        if(!startWithPhotoVideo) {
            OffScreenIndicatorManager.Instance.ShowArrow();
        }

        ResetSizePlaneForRecord();
        SetFrameRate(oldFrameRate);
        fromGallery = false;
    }

    //Set Crop position and size // 3/4 format bottom size is 679
    void ChangeScaleRecordPlanesByOrientation3l4() {
        float f = 0;
        // Portret
        if (!landscapeOrient) {
            origPlane.position -= new Vector3(0, 0.32f, 0);
            filterPlane.position -= new Vector3(0, 0.32f, 0);
            f = 2 * (3 / 4.0f);
        }
        // Landscape
        else {
            if(!fromGallery) {
                RotateFilteredPlanes(90);
            }
            origPlane.position += new Vector3(0.32f, 0, 0);
            filterPlane.position += new Vector3(0.32f, 0, 0);
            f = 2;
        }

        origPlane.localScale *= f;
        filterPlane.localScale *= f;
        print($"ChangeScaleRecordPlanesByOrientation3l4()1 f = {f}");
    }

    //Set Crop position and size // square format
    void ChangeScaleRecordPlanesByOrientationSquare() {
        float f = 2;
        // Portret
        if (!landscapeOrient) {
            origPlane.position -= new Vector3(0, 0.4f, 0);
            filterPlane.position -= new Vector3(0, 0.4f, 0);
        }
        // Landscape
        else {
            RotateFilteredPlanes(90);
            origPlane.position += new Vector3(0.4f, 0, 0);
            filterPlane.position += new Vector3(0.4f, 0, 0);
        }

        origPlane.localScale *= f;
        filterPlane.localScale *= f;
        print($"ChangeScaleRecordPlanesByOrientationSquare() f = {f}");
    }
    
    
    #endregion

    #region RecordAudio

    [HideInInspector] public List<string> audioSegmentPaths = new List<string>();
    [HideInInspector] public bool isRecordingAudio;
    private List<float> audioSegmentLenghts = new List<float>();
    private AudioActivity currAudioActivity;
    private int currAudioClip;



    // -------------------- Public functions ----------------------

    public void SetPanel(AudioActivity panel, bool startRecording)
    {
        currAudioActivity = panel;
        DeleteAllAudioSegments();
        if (startRecording) {
            StartEnableMicrophone();
        }
    }

    public void NextAudioButton()
    {
        UIManager.Instance.SetUIMode(UIManager.UIMode.Activity);
        StopMicrophone();
        OffScreenIndicatorManager.Instance.ShowArrow();
        currAudioActivity.EnableEditSoundButton(false);
        if(audioSegmentPaths.Count > 1) {
            StartMergeAudioFile();
            currAudioActivity.EnableSaveButton(false);
        }
        else {
            audioPath = audioSegmentPaths[0];
            audioSegmentPaths.Clear();
            recordAudioUIManager.DeleteAudioSegment(audioSegmentLenghts[0]);
            audioSegmentLenghts.Clear();
            recordAudioUIManager.BeforeAudioTaken();
            OnAudioMergeCompleted(audioPath, currAudioActivity.GetActivityId());
            isRecordingAudio = false;
        }
    }

    public void StartMergeAudioFile()
    {
        isRecordingAudio = true;
        currAudioClip = 0;
        ChangeClip(audioRecordPlayer);
        currAudioActivity.ShowWaitAudioClipAlert(true);
    }

    public void CancelAudioButton()
    {
        if (currAudioActivity.isBorn)
        {
            currAudioActivity.OnCancelCreate();
        }
        else
        {
            currAudioActivity.OnCancelEdit();
        }
        StopMicrophone();
    }


    // -------------------- Record & Delete ----------------------

    public async void OnRecordButtonUp()
    {
        microphoneSource.mute = true;
        audioInput?.Dispose();
        cameraInput.Dispose();
        var path = await recorder.FinishWriting();
        print($"-=- Saved audio recording to: {path}");

        audioSegmentPaths.Add(path);
        segmentLength -= Time.time;
        segmentLength = Mathf.Abs(segmentLength);
        audioSegmentLenghts.Add(segmentLength);
        segmentLength = 0;
        // currAudioActivity.ShowSaveButtonAsEnable();
        // currAudioActivity.EnableSaveButton(true);
        // --- Disable AudioVisualization
        AudioVisualization.Instance.audioSource = null;
    }

    public void OnRecordButtonDown()
    {
        print($"OnRecordButtonDown > status={status}");
        // ToDo: Double check that we are only using this for recording an audio
        //      and NOT for photo/video.
        isNewAudio = true;        

        var sampleRate = AudioSettings.outputSampleRate;
        var channelCount = (int)AudioSettings.speakerMode;
        var clock = new RealtimeClock();
        recorder = new MP4Recorder(2, 2, frameRate, sampleRate, channelCount);
        // Create recording inputs
        cameraInput = new CameraInput(recorder, clock, Camera.main);
        audioInput = new AudioInput(recorder, clock, microphoneSource, true);
        microphoneSource.mute = audioInput == null;
        segmentLength = Time.time;

        // --- Enable AudioVisualization
        AudioVisualization.Instance.audioSource = microphoneSource;
    }

    public void DeleteAudioSegment()
    {
        int indexLastSegment = audioSegmentPaths.Count - 1;
        if (!String.IsNullOrEmpty(audioSegmentPaths[indexLastSegment]))
        {
            if (File.Exists(audioSegmentPaths[indexLastSegment]))
            {
                try
                {
                    print($"-=- Audio was deleted, path = {audioSegmentPaths[indexLastSegment]}");
                    File.Delete(audioSegmentPaths[indexLastSegment]);
                    // print($"-=- Audio was deleted");
                }
                catch (Exception ex)
                {
                    print($"DeleteAudio Directory.Delete Exception: {ex.Message}");
                }
            }
        }

        audioSegmentPaths.RemoveAt(indexLastSegment);
        recordAudioUIManager.DeleteAudioSegment(audioSegmentLenghts[indexLastSegment]);
        audioSegmentLenghts.RemoveAt(indexLastSegment);
        if (indexLastSegment == 0)
        {
            recordAudioUIManager.BeforeAudioTaken();
        }
        // if(recordAudioUIManager.gameObject.activeInHierarchy) {
        //     currAudioActivity.ShowSaveButtonAsEnable();
        // }
    }

    void DeleteAllAudioSegments()
    {
        int count = audioSegmentPaths.Count;
        if (count != 0)
        {
            for (int i = 0; i < count; i++)
            {
                DeleteAudioSegment();
            }
        }
        audioSegmentPaths = new List<string>();
        audioSegmentLenghts = new List<float>();
        currAudioClip = 0;
    }


    // ------------------------ Merge Audio Segments Part ----------------------------

    void StartMergeAudioClips()
    {
        print($"-=- RecordManager StartMergeAudioClips()");
        var sampleRate = AudioSettings.outputSampleRate;
        var channelCount = (int)AudioSettings.speakerMode;
        var clock = new RealtimeClock();

        recorder = new MP4Recorder(2, 2, frameRate, sampleRate, channelCount);
        // Create recording inputs
        cameraInput = new CameraInput(recorder, clock, Camera.main);
        audioInput = new AudioInput(recorder, clock, audioSourcesForMergeContent, true);
    }

    async void StopMergeAudioClips()
    {
        audioInput?.Dispose();
        cameraInput.Dispose();
        audioPath = await recorder.FinishWriting();
        DeleteAllAudioSegments();
        OnAudioMergeCompleted(audioPath, currAudioActivity.GetActivityId());
        isRecordingAudio = false;
        currAudioActivity.ShowWaitAudioClipAlert(false);
        print($"-=- RecordManager StopMergeAudioClips() audioPath = {audioPath}");
    }

    public void ChangeClip(VideoPlayer calledVP)
    {
        if (currAudioClip == audioSegmentPaths.Count)
        {
            if (isRecordingAudio)
            {
                StopMergeAudioClips();
                calledVP.Stop();
            }
            else
            {
                currAudioClip = 0;
                calledVP.url = audioSegmentPaths[currAudioClip];
                print($"-=- RecordManager ChangeClip last segment, curr path = {audioSegmentPaths[currAudioClip]}, currAudioClip = {currAudioClip}");
                currAudioActivity.ShowPlayButton();
            }
        }
        else
        {
            calledVP.url = audioSegmentPaths[currAudioClip];
            calledVP.Play();
            currAudioActivity.EnableAudioVisualization();
            print($"-=- RecordManager ChangeClip, curr path = {audioSegmentPaths[currAudioClip]}, currAudioClip = {currAudioClip}");
            currAudioClip++;
        }
    }

    void StartFirstAudioClipForMerge(VideoPlayer calledVP)
    {
        if (currAudioClip == 1 && isRecordingAudio)
        {
            StartMergeAudioClips();
        }
    }


    #endregion

    #region PickImageFromGallery


    // ------------- Based on asset: https://github.com/yasirkula/UnityNativeGallery ---------------
    private int maxSize = 1920;

    private void PickImageOrVideo()
    {
        if (flashlight.isOn == false)
        {
            flashlight.isOn = true;
#if UNITY_IOS
            FlashlightController.Instance.TurnOff();
#endif
        }
        if (status != "photoTakeOnly")
        {
            if (NativeGallery.CanSelectMultipleMediaTypesFromGallery())
            {
                NativeGallery.Permission permission = NativeGallery.GetMixedMediaFromGallery((path) =>
                {
                    print("-=- Media path: " + path);
                    if (path != null)
                    {
                        fromGallery = true;

                        // Determine if user has picked an image, video or neither of these
                        switch (NativeGallery.GetMediaTypeOfFile(path))
                        {
                            case NativeGallery.MediaType.Image:
                                print("photo selected from the gallery");
                                ShowPhotoFromGallery(path);
                                break;
                            case NativeGallery.MediaType.Video:
                                print("video selected from the gallery");
                                ShowVideoFromGallery(path);
                                break;
                            default:
                                Debug.Log("Probably picked something else");
                                break;
                        }
                    }
                }, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video, "Select an image or video");

                print("Permission result: " + permission);
            }
        }
        else
        {
            PickPhoto();
        }
    }

    void PickPhoto()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            print("Image path: " + path);
            if (path != null)
            {
                ShowPhotoFromGallery(path);
            }
        }, "Select a PNG image", "image/png");

        print("Permission result: " + permission);
    }

    void ShowVideoFromGallery(string path)
    {
        if (path != null)
        {
            isPhotoTaking = false;
            print("-=- Play video from gallery");
            segmentPaths.Add(path);
            
            segmentLengths.Add(NativeGallery.GetVideoProperties(path).duration * 0.001f);
            StartShowVideoWithFilterFirstTime();
        }
    }

    void ShowPhotoFromGallery(string path)
    {
        if (path != null)
        {
            // Create Texture from selected image
            Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize);
            if (texture == null)
            {
                print("Couldn't load texture from " + path);
                return;
            }

            print("ShowPhotoFromGallery > texture was loaded from the path");
            isPhotoTaking = true;
            photoPath = path;
            // --- Save Texture2D for filter
            originalPhotoTexture2D = texture;
            status = "filterApplying";

            // --- Show Photo with filters on full screen from another camera
            photoPlaneForRecord.SetActive(true);
            flashlight.isOn = true;
#if UNITY_IOS
            FlashlightController.Instance.TurnOff();
#endif
            recordUiManager.SetOriginalFilter();
            ShowImageWithFilter();
            ChangeCamera(true);

            // --- This is for cropping, which disable currently
            // focalPointButton.gameObject.SetActive(false);
            // ShowARContent();
            // focalPointPage.SetActive(false);
            // showCroppingButton.SetActive(true);
        }
    }

    void CorrectImageDimension(int originalWidth, int originalHeight)
    {
        if (segmentPaths.Count > 1)
        {
            print("CorrectImageDimension: segmentPaths.Count > 1.");
            return;
        }
        
        bool rotate90 = false;
        float rotate = 0;
        if (fromGallery && segmentPaths.Count == 1) {
            rotate = NativeGallery.GetVideoProperties(segmentPaths[0]).rotation;
            if (rotate != 0 && rotate != 180) {
                rotate90 = true;
                int temp = originalWidth;
                originalWidth = originalHeight;
                originalHeight = temp;
            }
        }
        
        float ySize = 1;
        float xSize = 1;
        float screenHeightToWidth = (float)videoHeight / videoWidth;
        float fileHeightToWidth = (float)originalHeight / originalWidth;

        if (originalWidth <= originalHeight)
        {
            if(!rotate90) {
                ySize = fileHeightToWidth / screenHeightToWidth;
            }
            else {
                xSize = fileHeightToWidth / screenHeightToWidth;
            }
            print(
                $"-=- CorrectImageDimension isPortret, originalWidth = {originalWidth}, originalHeight = {originalHeight}, ySize = {ySize}, xSize = {xSize}, rotate = {rotate}");
            landscapeOrient = false;
        }
        else
        {
            if (!rotate90) {
                ySize = videoWidth / (float) videoHeight;
                xSize = originalWidth / (float) originalHeight;
            }
            else {
                xSize = videoWidth / (float) videoHeight;
                ySize = originalWidth / (float) originalHeight;
            }
            print(
                $"-=- CorrectImageDimension is Landscape, originalWidth = {originalWidth}, originalHeight = {originalHeight}, ySize = {ySize}, xSize = {xSize}, rotate = {rotate}");
            landscapeOrient = true;
        }
        playerTexture1.transform.localScale = new Vector3(xSize, ySize, 1);
        playerTexture2.transform.localScale = new Vector3(xSize, ySize, 1);
        photoPlaneForRecord.transform.localScale = new Vector3(xSize, ySize, 1);
        if (fromGallery) {
            Quaternion quaternion = Quaternion.Euler(0,0,-rotate);
            playerTexture1.transform.rotation = quaternion;
        }
    }
    

    #endregion

    #region Saving path contents in device memory


    public void SaveContentPath()
    {
        print($"1 - Memory used={System.GC.GetTotalMemory(true)}");
        string path = System.IO.Path.Combine(Application.persistentDataPath, "oldContents.json");
        print($"-=- RecordManager SaveContentPath() path: {path}, oldContents.Count = {oldContents.Count}");
        DictionaryToJson dictionaryToJson = new DictionaryToJson();
        foreach (var oldContent in oldContents)
        {
            dictionaryToJson.keys.Add(oldContent.Key);
            dictionaryToJson.values.Add(oldContent.Value);
        }

        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                print($"Delete oldContents.json Exception: {ex.Message}");
            }
        }

        string json = JsonUtility.ToJson(dictionaryToJson);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        Save("oldContents.json", bytes);
        print($"-=- RecordManager SaveContentPath() success!");
        print($"2 - Memory used={System.GC.GetTotalMemory(true)}");
    }

    void LoadContentPath()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "oldContents.json");
        Debug.Log($"-=- LoadContentPath, path with Combine = {path}");

        if (File.Exists(path))
        {
            try
            {
                var loadedBytes = Load("oldContents.json");
                string json = System.Text.Encoding.UTF8.GetString(loadedBytes);
                Debug.Log($"-=- LoadContentPath, json = {json}");

                DictionaryToJson dictionaryToJson = new DictionaryToJson();
                dictionaryToJson = JsonUtility.FromJson<DictionaryToJson>(json);
                oldContents = new Dictionary<string, string>();
                for (int i = 0; i < dictionaryToJson.keys.Count; i++)
                {
                    oldContents.Add(dictionaryToJson.keys[i], dictionaryToJson.values[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"-=- LoadContentPath() file does NOT exist. Exception: {ex.Message}");
            }
        }
    }

    void Save(string name, byte[] bytes)
    {
        var path = System.IO.Path.Combine(Application.persistentDataPath, name);
        System.IO.File.WriteAllBytes(path, bytes);
    }

    byte[] Load(string name)
    {
        var path = System.IO.Path.Combine(Application.persistentDataPath, name);
        return System.IO.File.ReadAllBytes(path);
    }

    // --- only audio/video
    public string PathWithNewGUID(string oldPath)
    {
        string[] parts = oldPath.Split('/');
        string newPath = Application.persistentDataPath + "/" + parts[parts.Length - 1];
        return newPath;
    }

    // --- only photo
    public string PhotoPathWithNewGUID(string oldPath)
    {
        string[] parts = oldPath.Split('/');
        string newPath = Application.persistentDataPath + "/" + parts[parts.Length - 2] + "/" + parts[parts.Length - 1];
        return newPath;
    }


    #endregion

    #region Stream Showing helper

    private PhotoVideoActivityForConsume lastConsumeVideoActivity;
    private PhotoVideoActivity lastCreateVideoActivity;
    private AudioActivity lastAudioActivity;
    [HideInInspector] public bool mediaPlayerIsPause;
    private bool mediaPlayerIsPlaing;

    // bool IsCloudflarePath(string path) {
    //     print($"-=- IsCloudflarePath, path = {path}");
    //     string[] parts = path.Split('/');
    //     // print($"-=- parts[0] = {parts[0]}\nparts[1] = {parts[1]}\nparts[2] = {parts[2]}\nparts[3] = {parts[3]}");
    //     if (parts[2] == "watch.cloudflarestream.com") {
    //         return true;
    //     }
    //     return false;
    // }

    public string CorrectedCloudflarePath(string path)
    {
        print($"-=- CorrectedPath");
        string[] parts = path.Split('/');
        string newPath = "https://videodelivery.net/" + parts[3] + "/manifest/video.m3u8";
        return newPath;
    }

    bool isLocalPath(string path)
    {
        // print($"-=- isLocalPath, path = {path}");
        string[] parts = path.Split('/');
        if (parts[0] == "file:" || parts[1] == "var" || parts[0] == "var")
        {
            // print($"-=- isLocalPath true, parts[0] = {parts[0]}");
            return true;
        }
        return false;
    }

    void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
    {
        switch (et)
        {
            case MediaPlayerEvent.EventType.Started:
                print("-=-= MediaPlayerEvent.EventType.Started");
                if (lastConsumeVideoActivity != null)
                {
                    lastConsumeVideoActivity.HideVideoLoader();
                    lastConsumeVideoActivity.SetAllClipLoader((float)mp.Info.GetDuration());
                    lastConsumeVideoActivity.PlayAllClipLoader();
                    lastConsumeVideoActivity.CorrectBoardScaleByVideoSize(mp);
                }
                else if (lastCreateVideoActivity != null)
                {
                    lastCreateVideoActivity.HideVideoLoader();
                    lastCreateVideoActivity.SetAllClipLoader((float)mp.Info.GetDuration());
                    lastCreateVideoActivity.PlayAllClipLoader();
                }
                else if (onBoard != null)
                {
                    onBoard.HideVideoLoader();
                }

                if(!isRecordingMergeVideo) {
                    if(currSegmentNum == 0) {
                        StartCoroutine(SetIsPlaing());
                    }
                }
                break;

            case MediaPlayerEvent.EventType.FirstFrameReady:
                print("-=-= MediaPlayerEvent.EventType.FirstFrameReady");
                if(isRecordingMergeVideo) {
                    if (currSegmentNum == 0) {
                        StartCoroutine(StartRecordBothVideos(mp.Info.GetVideoWidth(), mp.Info.GetVideoHeight()));
                    }
                    else {
                        StartCoroutine(ResumeRecording());
                    }
                }
                break;
            
            case MediaPlayerEvent.EventType.FinishedPlaying:
                print($"-=- MediaPlayerEvent.EventType.FinishedPlaying");
                if(isRecordingMergeVideo) {
                    if(currSegmentNum >= segmentPaths.Count - 1) {
                        StopRecordBothVideos();
                    }
                    else {
                        PauseRecording();
                    }
                }
                break;
            
            case MediaPlayerEvent.EventType.Stalled:
                print("-=-= MediaPlayer Stalled event trigger");
                if (!mediaPlayerIsPause)
                {
                    if (lastConsumeVideoActivity != null)
                    {
                        lastConsumeVideoActivity.ShowVideoLoader();
                        lastConsumeVideoActivity.StopAllClipLoader();
                    }
                    else if (lastCreateVideoActivity != null)
                    {
                        lastCreateVideoActivity.ShowVideoLoader();
                        lastCreateVideoActivity.StopAllClipLoader();
                    }
                    else if (onBoard != null)
                    {
                        onBoard.ShowVideoLoader();
                    }
                }
                break;

            case MediaPlayerEvent.EventType.Unstalled:
                print("-=-= MediaPlayer Unstalled event trigger");
                
                if (!mediaPlayerIsPause && !isRecordingMergeVideo)
                {
                    if (lastConsumeVideoActivity != null)
                    {
                        lastConsumeVideoActivity.HideVideoLoader();
                        lastConsumeVideoActivity.PlayAllClipLoader();
                    }
                    else if (lastCreateVideoActivity != null)
                    {
                        lastCreateVideoActivity.HideVideoLoader();
                        lastCreateVideoActivity.PlayAllClipLoader();
                    }
                    else if (onBoard != null)
                    {
                        onBoard.HideVideoLoader();
                    }
                    mediaPlayerIsPlaing = true;
                }
                break;
        }
    }

    IEnumerator SetIsPlaing()
    {
        float time = Time.time + 15;
        bool over = false;
        while (!over)
        {
            print("-=-= SetIsPlaing() mediaPlayer.Control.IsPaused()");
            if (!mediaPlayer.Control.IsPaused())
            {
                over = true;
            }
            if (time < Time.time)
            {
                over = true;
            }
            yield return null;
        }
        mediaPlayerIsPlaing = true;
    }

    public void SoundAudioByMediaPlayer(string path, AudioActivity audioActivity)
    {
        lastAudioActivity = audioActivity;

        if (lastCreateVideoActivity != null)
        {
            StartCoroutine(lastCreateVideoActivity.LoadThumbnail(true));
            lastCreateVideoActivity = null;
            mediaPlayerIsPause = false;
        }

        if (lastConsumeVideoActivity != null)
        {
            StartCoroutine(lastConsumeVideoActivity.LoadThumbnail(true));
            lastConsumeVideoActivity = null;
            mediaPlayerIsPause = false;
        }

        string cloudPath = CorrectedCloudflarePath(path);
        print($"-=- RecordManager SoundAudioByMediaPlayer, cloudPath = {cloudPath}");

        if (!mediaPlayerIsPause)
        {
            applyToMesh.MeshRenderer = null;
            MediaReference mediaReference = new MediaReference()
            {
                MediaPath = new MediaPath(cloudPath, MediaPathType.AbsolutePathOrURL)
            };
            mediaPlayer.OpenMedia(mediaReference, true);
        }
        else
        {
            mediaPlayer.Control.Play();
        }
    }

    #endregion

    #region OtherMethods

    void PreRecord() {
        if(!wasPreRecord) {
            StartCoroutine(StartPreRecord());
            StartCoroutine(PreRecordExeption());
            wasPreRecord = true;
        }
        else {
            status = previousStatus;
            ShowWaitMicrophonePanel(false);
        }
    }
    // if we get error while PreRecord, we enable record button
    IEnumerator PreRecordExeption() {
        yield return new WaitForSeconds(0.5f);
        status = previousStatus;
        ShowWaitMicrophonePanel(false);
    }

    IEnumerator StartPreRecord() {
        print($"StartPreRecord()");

        // var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
        // var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
        var sampleRate = 0;
        var channelCount = 0;
        var clock = new RealtimeClock();
        recorder = new MP4Recorder(2, 2, frameRate, sampleRate, channelCount);
        cameraInput = new CameraInput(recorder, clock, Camera.main);
        // audioInput = new AudioInput(recorder, clock, microphoneSource, true);  

        yield return new WaitForSeconds(0.3f);

        StopPreRecord();
    }
    
    
    async void StopPreRecord() {
        print($"StopPreRecord()");
        // audioInput?.Dispose();
        cameraInput.Dispose();
        var path = await recorder.FinishWriting();

        File.Delete(path);
        // status = previousStatus;
        // ShowWaitMicrophonePanel(false);
    }

    // public void RestoreAR() {
    //     print($"RecordManager RestoreAR()");
    //     if (!isCameraPermission) {
    //         print($"RecordManager CheckCameraPermissions()");
    //         StartCoroutine(CheckCameraPermissions());
    //     }
    //
    //     if (!isMicrophonePermission) {
    //         print($"RecordManager CheckMicrophonePermissions()");
    //         StartCoroutine(CheckMicrophonePermissions());
    //     }
    // }
    
    void StartCheckingCameraPermissions() {
        isCameraPermission = false;
        StartCoroutine(CheckCameraPermissions());
    }

    public void GoToSettings() {
        string url = GetSettingsURL();
        print("the settings url is:" + url);
        Application.OpenURL(url);
    }
    
    public void StartCheckingMicrophonPermissions() {
        isMicrophonePermission = false;
        StartCoroutine(CheckMicrophonePermissions());
    }

    IEnumerator CheckCameraPermissions() {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (Application.HasUserAuthorization(UserAuthorization.WebCam)) {
            isCameraPermission = true;
            cameraPermissionPanel.SetActive(false);
            Debug.Log("-=- webcam found");
        }
        else {
            isCameraPermission = false;
            cameraPermissionPanel.SetActive(true);
            Debug.Log("-=- webcam not found");
            yield return new WaitForSeconds(0.1f);
            RepeatCheckCamera();
            Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }
    }
    
    bool RepeatCheckCamera() {
        if (Application.HasUserAuthorization(UserAuthorization.WebCam)) {
            cameraPermissionPanel.SetActive(false);
            isCameraPermission = true;
            Debug.Log("-=- webcam found RepeatCheckCamera()");
            return true;
        }
        Invoke("RepeatCheckCamera", 0.1f);
        return false;
    }
    
    
    IEnumerator CheckMicrophonePermissions() {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (Application.HasUserAuthorization(UserAuthorization.Microphone)) {
            microphonePermissionPanel.SetActive(false);
            isMicrophonePermission = true;
            Debug.Log("-=- Microphone found");
        }
        else {
            isMicrophonePermission = false;
            microphonePermissionPanel.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            CheckMicPermission();
            Debug.Log("-=- Microphone not found");
        }
    }

    bool CheckMicPermission() {
        if (Application.HasUserAuthorization(UserAuthorization.Microphone)) {
            microphonePermissionPanel.SetActive(false);
            isMicrophonePermission = true;
            // Invoke("HideMicPermissionPanel", 1);
            Debug.Log("-=- Microphone found");
            return true;
        }
        if(recordMicrophone){
            Invoke("CheckMicPermission", 0.1f);
        }
        return false;
    }
    
    void ShowWaitMicrophonePanel(bool enable) {
        waitMicrophonePanel.SetActive(enable);
        waitMicrophonePanelAudio.SetActive(enable);
    }

    public void CleanMediaPlayerPath() {
        mediaPlayer.OpenMedia(emptyMediaReference, false);
    }

    bool IsLandscapeOrientation() {
        // z = 90 or 270
        Vector3 cameraRotationAngles = Camera.main.transform.rotation.eulerAngles;
        if (cameraRotationAngles.z > 65 && cameraRotationAngles.z < 115 ||
            cameraRotationAngles.z > 245 && cameraRotationAngles.z < 295) {
            return true;
        }
        return false;
    }

    void ChangeOrientation(bool isLandscape) {
        if(isLandscape) {
            recordUiManager.ChangeOrientation(-90);
        }
        else {
            recordUiManager.ChangeOrientation(0);
        }
    }
    
    void ResetSizePlaneForRecord() {
        playerTexture1.transform.localScale = Vector3.one;
        playerTexture2.transform.localScale = Vector3.one;
        photoPlaneForRecord.transform.localPosition = new Vector3(0, 0, 0.5f);
        photoPlaneForRecord.transform.localScale = Vector3.one;
        Quaternion quaternion = Quaternion.Euler(0,0,0);
        playerTexture1.transform.rotation = quaternion;
        origPlane.position = new Vector3(0,1200, 1);
        filterPlane.position = new Vector3(0,1250, 1);
    }

    
    #endregion
    
}

[Serializable]
public class DictionaryToJson {
    public List<string> keys = new List<string>();
    public List<string> values = new List<string>();
}