using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Niantic.Lightship.AR.VpsCoverage;
using SocialBeeAR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SocialBeeAR.ActivityUIFacade;

public class WayspotMarker : MonoBehaviour
{

    public LocalizationTarget wayspotData;
    public SpriteRenderer markerThumbnail;
    public Canvas popUp;
    private Texture2D markerTexture;
    public RawImage thumbnailImage;
    public GameObject thumbnailProgress;
    public TextMeshProUGUI Text_LocationName;
    public TextMeshProUGUI Text_distance;
    public Button button_Localize;
    public Button button_Navigate;

    private void OnEnable()
    {
        if (popUp.worldCamera == null)
            popUp.worldCamera = MapBoxManager.Instance.mapCamera;

        button_Localize.onClick.AddListener(delegate {
            popUp.transform.localScale = Vector3.zero;
            popUp.gameObject.SetActive(false);
            NearbyVPSManager.Instance.OnLocalizeWayspot(wayspotData);
            MapBoxManager.Instance.DisableMarker(wayspotData);
        });
        button_Navigate.onClick.AddListener(delegate {
            popUp.transform.localScale = Vector3.zero;
            popUp.gameObject.SetActive(false);
            NearbyVPSManager.Instance.OnNavigateToWayspot(wayspotData);
            MapBoxManager.Instance.DisableMarker(wayspotData);
        });
    }

    private void OnDisable()
    {
        SBThumbnailSelector.Instance.ReportTexture -= GetThumbnail;
    }

    private void OnMouseDown()
    {
        if (!MapBoxManager.Instance.isMapLoading)
        {
            thumbnailProgress.SetActive(true);
            SBThumbnailSelector.Instance.LoadInitialThumbnail(wayspotData.ImageURL);
            SBThumbnailSelector.Instance.ReportTexture += GetThumbnail;
            MapBoxManager.Instance.DisablePopUp();
            NearbyVPSManager.Instance.OnMapMarkerSelected(wayspotData);
            popUp.gameObject.SetActive(true);
            SBThumbnailSelector.Instance.ReportTexture(markerTexture);
            TweenPromptScale();
            Text_LocationName.text = wayspotData.Name;

            Text_distance.text = (int)MapBoxManager.Instance.DistanceToDisplay(wayspotData) + "m";

            // Check Either to Navigate or Localize..
            if (MapBoxManager.Instance.DistanceToDisplay(wayspotData) <= NearbyVPSManager.Instance.distanceThreshold)
                button_Localize.gameObject.SetActive(true);
            else
                button_Navigate.gameObject.SetActive(true);
        }
    }

    private void GetThumbnail(Texture2D thumbnailTexture)
    {
        thumbnailImage.texture = null;
        markerTexture = thumbnailTexture;
        thumbnailImage.texture = thumbnailTexture;

        if (thumbnailImage.texture != null)
            thumbnailProgress.SetActive(false);
        else
            thumbnailProgress.SetActive(true);
    }

    void TweenPromptScale()
    {
        popUp.transform.DOScale((float)0.02048081f, 0.5f);
    }
}
