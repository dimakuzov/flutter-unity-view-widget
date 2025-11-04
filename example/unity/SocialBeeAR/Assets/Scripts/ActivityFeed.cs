using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using RenderHeads.Media.AVProVideo;
using SocialBeeAR;
using UnityEngine.UI;

public class ActivityFeed : MonoBehaviour
{
    
    [SerializeField] private GameObject root;
    public GameObject contentRoot;

    [SerializeField] Camera filterCamera;
    [SerializeField] private Transform videoPlane;
    [SerializeField] private GameObject likePrefab;
    [SerializeField] private GameObject showActivityFeedButton;
    [SerializeField] private GameObject hideActivityFeedButton;

    [SerializeField] Activity[] activities;
    [SerializeField] Text location;
	
    
    private float width;
    private float height;
    private Vector3 invisiblePos { get; set; }
    private Vector3 visiblePos { set; get; }
    private bool isActive;
    [HideInInspector] public MediaPlayer mediaPlayer;

    
    private static ActivityFeed _instance;
    public static ActivityFeed Instance
    {
	    get
	    {
		    return _instance;
	    }
    }

    private void Awake() {
	    print("ActivityFeed > Awake...");
	    _instance = this;
    }
    
    void Start()
    {
	    print("ActivityFeed > started.");
	    CreateActivities();
	    // --- Hide panel first
        RectTransform rectTransform = root.GetComponent<RectTransform>();
        width = rectTransform.rect.width;
        height = rectTransform.rect.height;

        visiblePos = transform.position;
        // invisiblePos = visiblePos - new Vector3(0, height, 0);
        invisiblePos = Vector3.zero - new Vector3(0, height, 0);
            
        root.SetActive(false);
        currActivityIndex = 0;
        // --- Set anchor

        initContentRootXPos = contentRoot.transform.position.x;
        mediaPlayer = GetComponent<MediaPlayer>();
        // CreateActivities();
        Debug.Log($"-=- initContentRootXPos = {initContentRootXPos}");
        rootRT = contentRoot.GetComponent<RectTransform>();
    }

    Touch touch;
    private void Update() {
	    if (isActive) {
			
		    if (Input.touchCount > 0) {
			    touch = Input.GetTouch(0);
			    // --- can start slide filters only in selected area
			    if (touch.position.y < maxHeightOfTouchForActivities &&
			        touch.position.y > minHeightOfTouchForActivities) {
				    if(touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) {
					    MoveActivities(touch.position.x);
				    }
				    else if(touch.phase == TouchPhase.Began) {
					    // MoveActivities(touch.position.x);
					    OnPauseButton();
					    startTouchXPos = touch.position.x;
					    previousXPos = touch.position.x;
				    }
				    else {
					    StartCoroutine(MoveToSelectedPos(touch.position.x));
				    }
			    }
		    }
	    }
    }


    #region Public methods

    public void ShowActivityFeed() {
	    //show with animation
	    root.SetActive(true);
	    SetVisible(true, root.transform,
		    () => {
			    isActive = true;
			    RectTransform contentTr = contentRoot.GetComponent<RectTransform>();
			    minHeightOfTouchForActivities = contentTr.position.y;
			    maxHeightOfTouchForActivities = contentTr.position.y + contentTr.rect.height;
			    Debug.Log($"-=- minHeightOfTouchForActivities = {minHeightOfTouchForActivities}, maxHeightOfTouchForActivities = {maxHeightOfTouchForActivities}");
			    showActivityFeedButton.SetActive(false);
			    hideActivityFeedButton.SetActive(true);
			    
			    OnBoardManager.Instance.FlyToWeTrack();
			    // print("-=- ShowActivityFeed() complete");
		    });
    }

    public void HideActivityFeed() {
	    HideActivityFeed(() => {});
    }
    
    public void HideActivityFeed(Action postAction = null) {
	    isActive = false;
	    SetVisible(false, root.transform,
		    () => {
			    showActivityFeedButton.SetActive(true);
			    hideActivityFeedButton.SetActive(false);
			    if(postAction != null) {
				    postAction.Invoke();
			    }
			    print("-=- HideActivityFeed() complete");
		    });
    }

    public void HideActivityFeedFast() {
	    isActive = false;
	    showActivityFeedButton.SetActive(false);
	    hideActivityFeedButton.SetActive(false);
	    root.SetActive(false);
    }
    
    public void ShowActivityFeedFast() {
	    isActive = true;
	    showActivityFeedButton.SetActive(false);
	    hideActivityFeedButton.SetActive(true);
	    root.SetActive(true);
    }

    public void OnPlayButton() {
	    Debug.Log("-=- OnPlayButton()");
	    if(String.IsNullOrWhiteSpace(mediaPlayer.MediaPath.Path)) {
		    string path = OnBoardManager.Instance.videoPath;
		    string[] parts = path.Split('/');
		    path = "https://videodelivery.net/" + parts[3] + "/manifest/video.m3u8";
		    mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, path, true);
	    }
	    else {
		    mediaPlayer.Play();
	    }
	    
	    // --- Set videoimage
	    filterCamera.depth = 2;
	    videoPlane.gameObject.SetActive(true);
	    activities[currActivityIndex].image.enabled = false;
	    float k = 2.0f / Screen.height;
	    // Debug.Log($"k = {k}");
	    // Debug.Log($"activities[currActivityIndex].playButton.transform.position.y - Screen.height / 2.0f) = {activities[currActivityIndex].playButton.transform.position.y - Screen.height / 2.0f}");
	    float videoPlaneYPos = (activities[currActivityIndex].playButton.transform.position.y - Screen.height / 2.0f) * k;
	    // Debug.Log($"videoPlaneYPos = {videoPlaneYPos}");
	    Vector3 planePos = videoPlane.localPosition;
	    planePos.y = videoPlaneYPos;
	    videoPlane.localPosition = planePos;
	    
	    
	    activities[currActivityIndex].playButton.SetActive(false);
	    activities[currActivityIndex].pauseButton.SetActive(true);
    }

    public void OnPauseButton() {
	    Debug.Log("-=- OnPauseButton()");
	    if(filterCamera.depth != -1) {
		    mediaPlayer.Stop();
		    filterCamera.depth = -1;
		    videoPlane.gameObject.SetActive(false);
		    activities[currActivityIndex].image.enabled = true;
		    activities[currActivityIndex].playButton.SetActive(true);
		    activities[currActivityIndex].pauseButton.SetActive(false);
	    }
    }

    public void ShowMemoriesVideo(string path) {
	    // Debug.Log("-=- ShowMemoriesVideo()");
	    // string[] parts = path.Split('/');
	    // path = "https://videodelivery.net/" + parts[3] + "/manifest/video.m3u8";
	    // mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, path, true);
	    // mediaPlayer.Play();
	    //
	    // filterCamera.depth = 2;
	    // videoPlane.gameObject.SetActive(true);
	    // videoPlane.localPosition = new Vector3(0, 0, 0.2f);
	    //
	    // videoPlane.localScale = new Vector3(1, 0.26f, 1);
    }

    public void HideMemoriesVideo() {
	    mediaPlayer.Stop();
	    filterCamera.depth = -1;
	    videoPlane.gameObject.SetActive(false);
    }

    public void MakeLikes(string[] selectedNames, OnBoardPanel onBoardPanel) {
	    Debug.Log("MakeLikes");
	    foreach (var name in selectedNames) {
		    if (!String.IsNullOrWhiteSpace(name)) {
			    foreach (var option in onBoardPanel.optionContents) {
				    if (option.title == name) {
					    GameObject like = Instantiate(likePrefab, activities[0].image.transform);
					    like.GetComponentInChildren<Text>().text = name;
					    like.GetComponentInChildren<Image>().sprite = option.sprite;
				    }
			    }
		    }
	    }
    }

    
    public void ApplyPhotoOnBoard() {
	    foreach (var activity in activities) {
		    if (activity.type == "Photo") {
			    
			    var bytes = System.IO.File.ReadAllBytes(RecordManager.Instance.filteredFilePath);
			    Texture2D tex = new Texture2D(1, 1);
			    tex.LoadImage(bytes);
			    activity.rawImage.texture = tex;

			    GameObject panel = activity.rawImage.gameObject;
			    RectTransform panelTr = panel.GetComponent<RectTransform>();
			    
			    float h = (float)tex.height / (float)tex.width;
			    panelTr.localScale = new Vector3(1, h, 1);
		    }
	    }
    }

    public void EnablePlayButton(bool enable) {
	    foreach (var activity in activities) {
		    if (activity.type == "Video") {
			    activity.playButton.GetComponent<Button>().interactable = enable;
			    activity.pauseButton.GetComponent<Button>().interactable = enable;
		    }
	    }
    }
    
    #endregion

    #region Create anchor page

    
    void CreateActivities() {
	    foreach (var activity in activities) {
		    if (activity.type == "Video") {
			    Debug.Log("-=- CreateActivities() Video");
			    activity.playButton.GetComponent<Button>().onClick.AddListener(delegate { OnPlayButton();});
			    activity.pauseButton.GetComponent<Button>().onClick.AddListener(delegate { OnPauseButton();});
		    }
	    }

	    SetAnchorDescription();
    }

    void SetAnchorDescription() {
	    SBContext context = SBContextManager.Instance.context;
	    if(context != null) {
		    location.text = context.UserLocation.City + ", " + context.UserLocation.Country;
	    }
    }
    
    
    #endregion
    
	#region Slide filters and apply selected filter

	
	private float iconDis;
	GameObject selectedIcon;
	private float maxHeightOfTouchForActivities;
	private float minHeightOfTouchForActivities;
	private float startTouchXPos;
	
	
	private bool iconsDraggedSelectedIcon;


	// -- first action, create all icons
	// void CreateIconsLine() {
	// 	iconDis = filtersBar.GetComponent<RectTransform>().rect.width / 5;
	//
	// 	for (int i = 0; i < record.filterNames.Count; i++) {
	// 		Vector3 pos = new Vector3(iconDis * i, 0, 0);
	// 		GameObject icon = Instantiate(filterIconPrefab, Vector3.zero, Quaternion.identity, filtersBar.transform);
	// 		icon.transform.localPosition = pos;
	// 		icon.GetComponentInChildren<Text>().text = record.filterNames[i];
	// 		int num = i;
	// 		icon.GetComponent<Button>().onClick.AddListener(delegate { MoveToSelectedPos(icon, num);});
	// 		icons.Add(icon);
	// 	}
	//
	// 	IncreasingAndSelectingIcon();
	// }



	private float previousXPos = 0;
	private RectTransform rootRT;

	void MoveActivities(float xPos) {
		float moveDis = xPos - previousXPos;

		Vector3 newPos = rootRT.localPosition;
		newPos += new Vector3(moveDis * 1.3f, 0, 0);
		rootRT.localPosition = newPos;
		previousXPos = xPos;
	}

	private int currActivityIndex;
	private float initContentRootXPos;
	// --- when user touch up, we will move selected icon to center.

	public void MoveActivities(bool right = true) {
		if (right) {
			Debug.Log($"-=- MoveActivities");
			startTouchXPos = 0;
			StartCoroutine(MoveToSelectedPos(-300.0f));
		}
	}
	
	IEnumerator MoveToSelectedPos(float currXPos) {
		
		float rootWidth = Screen.width - (initContentRootXPos * 2);
		float contentRootYPos = contentRoot.transform.position.y;
		Vector3 targetPos;
		Debug.Log($"-=- MoveToSelectedPos start");

		if(Math.Abs(startTouchXPos - currXPos) > 250.0f) {
			Debug.Log($"-=- MoveToSelectedPos run startTouchXPos - currXPos = {startTouchXPos - currXPos}, currActivityIndex = {currActivityIndex}");
			if ((startTouchXPos - currXPos) > 0) {
				currActivityIndex++;
				if (currActivityIndex == activities.Length) {
					currActivityIndex = activities.Length - 1;
				}
			}
			else {
				if(currActivityIndex > 0) {
					currActivityIndex--;
				}
			}

			if (currActivityIndex == activities.Length) {
				currActivityIndex = 0;
			}

			targetPos = new Vector3(-currActivityIndex * rootWidth + initContentRootXPos, contentRootYPos, 0);
			// Debug.Log($"-=- MoveToSelectedPos currActivityIndex = {currActivityIndex}, width = {width}");
			contentRoot.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCirc)
				.OnComplete(() => {
					EnableActivityIcon();
					Debug.Log($"-=- MoveToSelectedPos complete");
				});
			yield return null;
		}
		
		// Debug.Log($"-=- MoveToSelectedPos NOT run startTouchXPos - currXPos = {startTouchXPos - currXPos}");
		targetPos = new Vector3(-currActivityIndex * rootWidth + initContentRootXPos, contentRootYPos, 0);
		contentRoot.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutCirc)
			.OnComplete(() => {
				EnableActivityIcon();
				Debug.Log($"-=- MoveToSelectedPos back complete");
			});
		yield return null;
	}

	void EnableActivityIcon() {
		foreach (var activity in activities) {
			activity.selectedIcon.SetActive(false);
		}
		activities[currActivityIndex].selectedIcon.SetActive(true);
	}
	

	#endregion
	
    
	void SetVisible(bool visible, Transform panel, Action postAction = null) {
		if (visible) {
			panel.position = invisiblePos;
			panel.DOMove(Vector3.zero, 0.9f).SetEase(Ease.OutQuint)
				.OnComplete(() => { postAction?.Invoke(); });
		}
		else {
			panel.DOMove(invisiblePos, 0.9f).SetEase(Ease.OutQuint)
				.OnComplete(() => { postAction?.Invoke(); });
		}
	}
    

    [Serializable]
    public class Anchor {
	    public Text title;
	    public Text location;
	    public Text description;
        public Activity[] activities;
    }
    
    [Serializable]
    public class Activity {
	    public string type; // "Photo", "Video", "Audio", "Trivia", "Post"
	    public GameObject selectedIcon;
	    public GameObject playButton;
	    public GameObject pauseButton;
	    public Image image;
	    public RawImage rawImage;
        public string contentPath;
        public string contentLocalPath;
    }
    
}
