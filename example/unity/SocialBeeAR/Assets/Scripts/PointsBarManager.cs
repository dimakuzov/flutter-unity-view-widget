using System;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PointsBarManager : MonoBehaviour {

    [SerializeField] private GameObject root;
    [SerializeField] private Text pointsText;
    [SerializeField] private Text timeConsumedText;
    [SerializeField] private Text distanceTravelledText;
    [SerializeField] private Text stemsTakenText;
    [SerializeField] private Text elevatedGainedText;
    
    [SerializeField] private GameObject info;
    [SerializeField] private GameObject arrow;
    private float width;
    
    private Vector3 invisiblePos { get; set; }
    private Vector3 visiblePos { get; set; }
    
    private static PointsBarManager _instance;
    public static PointsBarManager Instance
    {
        get
        {
            return _instance;
        }
    }
    
    private void Awake() {
        _instance = this;
    }

    private void Start() {
        visiblePos = info.transform.position;
        
        RectTransform rectTransform = info.GetComponent<RectTransform>();
        width = rectTransform.rect.height;
        rectTransform = arrow.GetComponent<RectTransform>();
        invisiblePos = visiblePos - new Vector3(width - rectTransform.rect.height, 0, 0);

        info.transform.position = invisiblePos;
        info.SetActive(false);
    }
    
    public void ShowPointsBar() {
        print("-=- PointsBarManager ShowPointsBar()");
        root.SetActive(true);
    }

    public void HidePointsBar() {
        print("-=- PointsBarManager HidePointsBar()");
        root.SetActive(false);
    }
     
    public void SetPointsBar(ExperienceStatistics statistics, bool? showNow = false)
    {
        SetPoints(statistics.Points);        
        timeConsumedText.text = statistics.GetFormattedFinalTotalTime();        
        SetDistanceTravelled(statistics.Distance);
        SetStepsTaken(statistics.Steps);
        SetElevatedGained(statistics.Elevation);
        if (showNow ?? false)
        {
            ShowPointsBar();
        }
    }


    public void SetPoints(int points) {
        pointsText.text = points.ToString();
    }
    
    public void SetTimeConsumed(int timeConsumed) {
        timeConsumedText.text = timeConsumed.ToString("t");
    }

    public void SetTimeConsumed(string formattedTime)
    {
        timeConsumedText.text = formattedTime;
    }

    public void SetDistanceTravelled(double distanceTravelled) {
        // The distance and elevation control names were interchanged.
        elevatedGainedText.text = distanceTravelled.MeterToStatsString();
    }
    
    public void SetStepsTaken(int steps) {
        stemsTakenText.text = ((double)steps).Shortened();
    }
    
    public void SetElevatedGained(double elevatedGained) {
        // The distance and elevation control names were interchanged.
        distanceTravelledText.text = elevatedGained.MeterToStatsString();
    }

    public void ShowInfo() {
        if(!info.activeSelf) {
            arrow.SetActive(false);
            SetVisible(true);
        }
        else {
            SetVisible(false, () => {
                arrow.SetActive(true);
            });
        }
    }

    void SetVisible(bool showOptions, Action postAction = null) {
        print($"-=- Point Info SetVisible = {showOptions}");

        if (showOptions) {
            info.SetActive(true);
            info.transform.position = invisiblePos;
            info.transform.DOMove(visiblePos, 0.3f).SetEase(Ease.OutQuint).OnComplete(() => {
                postAction?.Invoke();
            });
        }
        else {
            info.transform.DOMove(invisiblePos, 0.3f).SetEase(Ease.OutQuint).OnComplete(() => {
                postAction?.Invoke();
                info.SetActive(false);
            });
        }
    }

}
