using System;
using System.Collections;
using System.Collections.Generic;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class RecordUIManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {

	[Header("UI Elements")] [SerializeField]
	GameObject galleryButton;

	[SerializeField] GameObject timer;
	[SerializeField] GameObject deleteButton;
	[SerializeField] GameObject flashLightButton;
	[SerializeField] GameObject videoIndicator;
	[SerializeField] GameObject instruction;
	[SerializeField] Image circle;
	[SerializeField] GameObject filtersBar;
	[SerializeField] GameObject recordButtonImage;
	[SerializeField] GameObject changeCameraButton;
	[SerializeField] GameObject focalPointButton;
	[SerializeField] GameObject focalPointView;
	[SerializeField] private GameObject showCroppingButton;// --- Enable this when cropping will be

	[SerializeField] GameObject linePrefab; // for long video indicator (white line)
	[SerializeField] GameObject filterIconPrefab;
	[SerializeField] GameObject nextButton;

	[SerializeField] private Transform[] rotatedOrientationObjs;
	
	private Text timerText;
	[HideInInspector] public RecordManager record;


	public UnityEvent onTouchDown, onTouchUp, onPointerClick;
	private bool pressed;
	// const float MaxRecordingTime = 60; // seconds

	[HideInInspector] public float ratio;
	float startTime;
	Slider indicatorSlider;
	Color redColor;

	private float startValue;
	private GameObject line;
	private List<GameObject> indicatorLines = new List<GameObject>();
	List<GameObject> icons = new List<GameObject>();
	private float widthResolution;
	
	private void Start() {
		Reset(false);
		timerText = timer.GetComponentInChildren<Text>();
		indicatorSlider = videoIndicator.GetComponent<Slider>();
		redColor = circle.color;
		circle.color = Color.white;
		// heightOfTouchForFilters = GetComponentInParent<RectTransform>().rect.height;
		heightOfTouchForFilters = 572;
		// widthResolution = FindObjectOfType<CanvasScaler>().referenceResolution.x + 10;
		widthResolution = 1135;
	}

	Touch touch;
	private void Update() {
		if (filtersBar.activeSelf) {
			
			if (Input.touchCount == 0 && previousXPos != 0 && !iconsDraggedSelectedIcon) {
				StartCoroutine(MoveToSelectedPos());
				previousXPos = 0;
			}
			
			if (Input.touchCount == 0) {
				daleyTimeForIconButton = 0;
			}

			if (Input.touchCount > 0) {
				touch = Input.GetTouch(0);
				// --- can start slide filters only in selected area
				if (touch.position.y < heightOfTouchForFilters) {
					daleyTimeForIconButton += Time.deltaTime;
					MoveFilters(touch.position.x);
				}
				// --- then we can slide filters everywhere
				else if(previousXPos != 0) {
					MoveFilters(touch.position.x);
				}
			}
		}
	}

	private void OnEnable() {
		BeforePhotoOrVideo();
	}

	public void Clear() {
		ResetUI();
	}

	public void Reset(bool isVideoTaken) {
		if (isVideoTaken) {
			VideoTaking();
		}
		else {
			BeforePhotoOrVideo();
		}
	}

	void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
		if(record.status != "WaitMicrophone") {
			// Start counting
			record.OnTouchDown();
		}
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
		// Start photo
	}

	void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
		// Reset pressed
		pressed = false;
	}

	// ----------------- Countdown -----------------

	public IEnumerator Countdown() {
		pressed = true;
		// First wait a short time to make sure it's not a tap
		yield return new WaitForSeconds(0.2f);
		if (!pressed || RecordManager.Instance.status == "photoTakeOnly") {
			record.OnClick();
			yield break;
		}

		// Start recording
		MakeNewSliderLine();
		VideoTaking();
		record.StartVideoRecording();

		startTime = Time.time;
		while (pressed && (ratio = (Time.time - startTime) / record.maxVideoRecordingTime) < 1.0f) {
			ShowSpriteSlider();
			yield return null;
		}
		// --- Add finished action record if ratio > 1

		circle.color = Color.white;
		onTouchUp?.Invoke();
	}

	
	public IEnumerator ResumeCountdown() {
		pressed = true;
		record.StartVideoRecording();

		MakeNewSliderLine();
		startTime = Time.time - ratio * record.maxVideoRecordingTime;
		while (pressed && (ratio = (Time.time - startTime) / record.maxVideoRecordingTime) < 1.0f) {
			ShowSpriteSlider();
			yield return null;
		}
		// --- Add finished action record if ratio > 1

		circle.color = Color.white;
		onTouchUp?.Invoke();
	}

	public void DeleteSegment(float clipLength) {
		print($"-=- RecordUIManager DeleteSegment");
		if(indicatorLines.Count > 0) {
			float segmentLength = indicatorLines[indicatorLines.Count - 1].GetComponent<RectTransform>().sizeDelta.x;
			ratio -= (segmentLength + 11) / widthResolution;
			Destroy(indicatorLines[indicatorLines.Count - 1]);
			indicatorLines.RemoveAt(indicatorLines.Count - 1);
			timerText.text =
				"00:" + (record.maxVideoRecordingTime - ratio * record.maxVideoRecordingTime).ToString("00");
			print($"-=- RecordUIManager DeleteSegment ratio = {ratio}, indicatorLines.Count = {indicatorLines.Count}");
		}
	}

	// ----------------- UI Functions -----------------
	void ResetUI() {
		galleryButton.SetActive(false);
		timer.SetActive(false);
		deleteButton.SetActive(false);
		flashLightButton.SetActive(false);
		videoIndicator.SetActive(false);
		instruction.SetActive(false);
		filtersBar.SetActive(false);
		changeCameraButton.SetActive(false);
		nextButton.SetActive(false);
		focalPointButton.SetActive(false);
		focalPointView.SetActive(false);
	}

	void BeforePhotoOrVideo() {
		ResetUI();
		galleryButton.SetActive(true);
		flashLightButton.SetActive(true);
		instruction.SetActive(true);
		recordButtonImage.SetActive(true);
		changeCameraButton.SetActive(true);
		// if(!record.startWithPhotoVideo) {
			focalPointButton.SetActive(true);
		// }
		focalPointView.SetActive(true);
		OffScreenIndicatorManager.Instance.HideArrow();
		MiniMapManager.Instance.HideMiniMap();
	}

	void VideoTaking() {
		ResetUI();
		timer.SetActive(true);
		deleteButton.SetActive(true);
		videoIndicator.SetActive(true);
		flashLightButton.SetActive(true);
		recordButtonImage.SetActive(true);
		changeCameraButton.SetActive(true);
		nextButton.SetActive(true);
		// if(!record.startWithPhotoVideo) {
			focalPointButton.SetActive(true);
		// }
		focalPointView.SetActive(true);
	}

	public void FilterApplying() {
		recordButtonImage.SetActive(false);
		ResetUI();
		nextButton.SetActive(true);
		filtersBar.SetActive(true);
		if (icons.Count == 0) {
			CreateIconsLine();
		}
	}

	public void SetOriginalFilter() {
		if (icons.Count != 0) {
			selectedIcon = icons[0];
			SelectingIcon(selectedIcon, 0);
			StartCoroutine(MoveToSelectedPos());
		}
	}

	void ShowSpriteSlider() {
		circle.color = redColor;
		indicatorSlider.value = ratio;
		float t = Mathf.CeilToInt(record.maxVideoRecordingTime - ratio * record.maxVideoRecordingTime);
		// t = Mathf.CeilToInt(t);
		if (t > 59) {
			timerText.text = (t / 60).ToString("00");
			timerText.text += ":" + (t % 60).ToString("00");
		}
		else {
			timerText.text = "00:" + t.ToString("00");
		}
		
		float x = (ratio - startValue) * widthResolution; // --- 1125 - CanvasScaler.referenceResolution.x in scene + offset
		line.GetComponent<RectTransform>().sizeDelta = new Vector2(x - 11, 14);
	}

	void MakeNewSliderLine() {
		indicatorSlider.value = ratio;
		startValue = ratio;
		line = Instantiate(linePrefab, new Vector3(11, 0, 0),
			Quaternion.identity, indicatorSlider.transform);
		line.transform.localPosition = new Vector3(ratio * widthResolution + 11, 0, 0);
		indicatorLines.Add(line);
	}


	#region Slide filters and apply selected filter

	
	private float iconDis;
	GameObject selectedIcon;
	private float heightOfTouchForFilters;
	private bool iconsDraggedSelectedIcon;
	private float daleyTimeForIconButton;

	// -- first action, create all icons
	void CreateIconsLine() {
		iconDis = filtersBar.GetComponent<RectTransform>().rect.width / 5;

		for (int i = 0; i < record.filterNames.Count; i++) {
			Vector3 pos = new Vector3(iconDis * i, 0, 0);
			GameObject icon = Instantiate(filterIconPrefab, Vector3.zero, Quaternion.identity, filtersBar.transform);
			icon.transform.localPosition = pos;

			//set filter text
			string filterName = record.filterNames[i];
			icon.GetComponentInChildren<Text>().text = filterName;

			//set filter icon
			icon.GetComponentInChildren<Image>().sprite = record.filterSprites[i];

			//add listener
			int num = i;
			icon.GetComponent<Button>().onClick.AddListener(delegate { MoveToSelectedPos(icon, num);});
			icons.Add(icon);
		}

		IncreasingAndSelectingIcon();
	}

	private void IncreasingAndSelectingIcon() {
		if (icons.Count == 0) {
			return;
		}

		// --- scale icons
		float startShowIncreaseDis = 0.95f;
		for (int i = 0; i < icons.Count; i++) {
			float xPosition = icons[i].transform.localPosition.x;
			GameObject iconImageObject = icons[i].GetComponentInChildren<Image>().gameObject;
			if (xPosition > -iconDis * startShowIncreaseDis && xPosition < iconDis * startShowIncreaseDis) {
				float addedScale = (1 - Mathf.Abs(xPosition / iconDis) / startShowIncreaseDis) * 0.5f;
				float scale = 1 + addedScale;
				iconImageObject.transform.localScale = new Vector3(scale, scale, scale);

				// --- apply filter
				if(!iconsDraggedSelectedIcon) {
					if (!selectedIcon && xPosition > -iconDis * 0.5f && xPosition < iconDis * 0.5f ||
					    selectedIcon.transform != icons[i].transform &&
					    xPosition > -iconDis * 0.5f && xPosition < iconDis * 0.5f) {

						SelectingIcon(selectedIcon, i);
						selectedIcon = icons[i];
					}
				}
			}
			else {
				iconImageObject.transform.localScale = new Vector3(1, 1, 1);
			}
		}
	}

	void SelectingIcon(GameObject selectedIcon, int numberSelected) {
		print($"-=- Select filter = {record.filterNames[numberSelected]}");
		record.ChangeFilter(numberSelected);
	}

	private float previousXPos = 0;

	void MoveFilters(float xPos) {
		if (previousXPos == 0) {
			previousXPos = xPos;
			return;
		}

		if (previousXPos != xPos) {
			float moveDis = xPos - previousXPos;

			foreach (var icon in icons) {
				Vector3 newPos = icon.GetComponent<RectTransform>().localPosition;
				newPos += new Vector3(moveDis * 1.3f, 0, 0);
				icon.GetComponent<RectTransform>().localPosition = newPos;
			}

			previousXPos = xPos;
			IncreasingAndSelectingIcon();
		}
	}
	
	void MoveToSelectedPos(GameObject icon, int numberOfSelectedIcon) {
		if(!iconsDraggedSelectedIcon && daleyTimeForIconButton < 0.3f) {
			selectedIcon = icon;
			SelectingIcon(selectedIcon, numberOfSelectedIcon);
			StartCoroutine(MoveToSelectedPos());
		}
	}
	
	// --- when user touch up, we will move selected icon to center.
	IEnumerator MoveToSelectedPos() {
		iconsDraggedSelectedIcon = true;
		RectTransform currRectTr = selectedIcon.GetComponent<RectTransform>();
		float reverseTime = 0.17f;
		float moveDis = -currRectTr.localPosition.x;
		int times = (int)(reverseTime / Time.deltaTime);
		moveDis = moveDis / times;

		for (int i = 0; i < times; i++) {
			foreach (var icon in icons) {
				Vector3 newPos = icon.GetComponent<RectTransform>().localPosition;
				newPos += new Vector3(moveDis, 0, 0);
				icon.GetComponent<RectTransform>().localPosition = newPos;
			}
			IncreasingAndSelectingIcon();
			yield return null;
		}
		
		moveDis = -selectedIcon.GetComponent<RectTransform>().localPosition.x;
		foreach (var icon in icons) {
			Vector3 newPos = icon.GetComponent<RectTransform>().localPosition;
			newPos += new Vector3(moveDis, 0, 0);
			icon.GetComponent<RectTransform>().localPosition = newPos;
		}
		iconsDraggedSelectedIcon = false;
		IncreasingAndSelectingIcon();
		yield return null;
	}
	

	#endregion

	private IEnumerator orientation;
	private float targetDegree;
	private float currentDegree = 0;
	
	public void ChangeOrientation(float degree) {
		targetDegree = degree;
		if(orientation != null) {
			StopCoroutine(orientation);
		}
		orientation = RotateOrientation();
		StartCoroutine(orientation);
	}

	IEnumerator RotateOrientation() {
		float frame = (targetDegree - currentDegree) / 9;

		if(currentDegree > targetDegree) {
			for (float f = currentDegree; f > targetDegree; f += frame) {
				foreach (var tr in rotatedOrientationObjs) {
					tr.rotation = Quaternion.Euler(new Vector3(0, 0, f));
					currentDegree = f;
				}
				yield return null;
			}
		}
		else {
			for (float f = currentDegree; f < targetDegree; f += frame) {
				foreach (var tr in rotatedOrientationObjs) {
					tr.rotation = Quaternion.Euler(new Vector3(0, 0, f));
					currentDegree = f;
				}
				yield return null;
			}
		}
		
		foreach (var tr in rotatedOrientationObjs) {
			tr.rotation = Quaternion.Euler(new Vector3(0, 0, targetDegree));
		}

		currentDegree = targetDegree;
		print($"RotateOrientation complete");
	}



}