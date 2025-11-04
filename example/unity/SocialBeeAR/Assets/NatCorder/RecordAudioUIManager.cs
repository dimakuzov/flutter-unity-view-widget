using System;
using System.Collections;
using System.Collections.Generic;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class RecordAudioUIManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

	[Header("UI Elements")]
	[SerializeField] GameObject timer;
	[SerializeField] GameObject deleteButton;
	[SerializeField] GameObject videoIndicator;
	[SerializeField] GameObject instruction;
	[SerializeField] GameObject nextButton;
	[SerializeField] Image circle;
	[SerializeField] Image mic;

	[SerializeField] GameObject linePrefab; // for long video indicator (white line)


	private Text timerText;
	[HideInInspector] public AudioActivity audioActivity;


	public UnityEvent onTouchDown, onTouchUp;
	private bool pressed;
	float maxRecordingTime; // seconds

	[HideInInspector] public float ratio;
	float startTime;
	Slider indicatorSlider;
	Color redColor;

	private float startValue;
	private GameObject line;
	private List<GameObject> indicatorLines = new List<GameObject>();
	private float widthResolution;

	private void Start() {
		timerText = timer.GetComponentInChildren<Text>();
		indicatorSlider = videoIndicator.GetComponent<Slider>();
		redColor = circle.color;
		circle.color = Color.white;
		mic.color = redColor;
		maxRecordingTime = RecordManager.Instance.maxAudioRecordingTime;
		// widthResolution = FindObjectOfType<CanvasScaler>().referenceResolution.x + 10;
		widthResolution = 1135;
		BeforeAudioTaken();
	}

	void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
		if (RecordManager.Instance.status != "WaitMicrophone") {
			RecordManager.Instance.OnRecordButtonDown();
			if (RecordManager.Instance.audioSegmentPaths.Count == 0) {
				AudioTaking();
				StartCoroutine(Countdown());
			}
			else {
				StartCoroutine(ResumeAudioCountdown());
			}
		}
	}

	void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
		if (pressed) {
			// Reset pressed
			RecordManager.Instance.OnRecordButtonUp();
			pressed = false;
		}
	}

	// ----------------- Countdown -----------------

	IEnumerator Countdown() {
		pressed = true;

		// Start recording
		MakeNewSliderLine();

		startTime = Time.time;
		while (pressed && (ratio = (Time.time - startTime) / maxRecordingTime) < 1.0f) {
			ShowSpriteSlider();
			yield return null;
		}
		// --- Add finished action record if ratio > 1

		circle.color = Color.white;
		mic.color = redColor;
		onTouchUp?.Invoke();
	}

	
	IEnumerator ResumeAudioCountdown() {
		pressed = true;

		MakeNewSliderLine();
		startTime = Time.time - ratio * maxRecordingTime;
		while (pressed && (ratio = (Time.time - startTime) / maxRecordingTime) < 1.0f) {
			ShowSpriteSlider();
			yield return null;
		}
		// --- Add finished action record if ratio > 1

		circle.color = Color.white;
		mic.color = redColor;
		onTouchUp?.Invoke();
	}

	public void DeleteAudioSegment(float clipLength) {
		float segmentLength = indicatorLines[indicatorLines.Count - 1].GetComponent<RectTransform>().sizeDelta.x;
		ratio -= (segmentLength + 11) / widthResolution ;
		Destroy(indicatorLines[indicatorLines.Count - 1]);
		indicatorLines.RemoveAt(indicatorLines.Count - 1);
		timerText.text = "00:" + (maxRecordingTime - ratio * maxRecordingTime).ToString("00");
		print($"-=- RecordAudioUIManager DeleteSegment ratio = {ratio}, indicatorLines.Count = {indicatorLines.Count}");
	}

	// ----------------- UI Functions -----------------
	void ResetUI() {
		timer.SetActive(false);
		deleteButton.SetActive(false);
		videoIndicator.SetActive(false);
		instruction.SetActive(false);
		nextButton.SetActive(false);
	}

	public void BeforeAudioTaken() {
		ResetUI();
		instruction.SetActive(true);
	}

	void AudioTaking() {
		ResetUI();
		timer.SetActive(true);
		deleteButton.SetActive(true);
		videoIndicator.SetActive(true);
		nextButton.SetActive(true);
	}

	void ShowSpriteSlider() {
		circle.color = redColor;
		mic.color = Color.white;
		indicatorSlider.value = ratio;
		float t = Mathf.CeilToInt(maxRecordingTime - ratio * maxRecordingTime);
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
	
}