using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    public class BottomPanel : MonoBehaviour
    {
        //[SerializeField] private float width;
        //[SerializeField] private float height;
        private float width;
        private float height;


        private Vector3 invisiblePos { get; set; }
        private Vector3 visiblePos { set; get; }

        [SerializeField] private Text messageText;
        [SerializeField] private GameObject hidenButton;


        private Vector3 arMapOriginalPosition;
        private Vector3 arMapChangedPosition;
        private float arHeight;


        public void Start()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            width = rectTransform.rect.width;
            height = rectTransform.rect.height;


            visiblePos = transform.position;
            invisiblePos = visiblePos - new Vector3(0, height, 0);


            // For Ar Navigation Map Positions.

            RectTransform ARrectTransform = MapBoxManager.Instance.navigationMapScreen.GetComponent<RectTransform>();
            arHeight = height;


            arMapOriginalPosition = ARrectTransform.transform.position;
            
            if (arHeight is 0 or > 1200)
                arMapChangedPosition = arMapOriginalPosition + new Vector3(0, 250f - MapBoxManager.Instance.arMapHeightOffset, 0);
            else
                //arMapChangedPosition = arMapOriginalPosition + new Vector3(0, arHeight - MapBoxManager.Instance.arMapHeightOffset, 0);
                arMapChangedPosition = arMapOriginalPosition + new Vector3(0, arHeight , 0);

            // print($"arHeight={arHeight} | arMapChangedPosition={arMapChangedPosition}");

            gameObject.SetActive(false);


        }


        public void SetVisible(bool visible, Action postAction = null)
        {
            print($"BottomPanel arHeight={arHeight}, arMapChangedPosition={arMapChangedPosition}, invisiblePos={invisiblePos}, visiblePos={visiblePos}");
            if (visible)
            {

                transform.position = invisiblePos;

                transform.DOMove(visiblePos, Const.PANEL_ANIMATION_TIME).SetEase(Ease.OutQuint).OnComplete(() =>
                {
                    MapBoxManager.Instance.navigationMapScreen.transform.position = arMapOriginalPosition;
                    MapBoxManager.Instance.navigationMapScreen.GetComponent<RectTransform>()
                        .DOMove(arMapChangedPosition, Const.PANEL_ANIMATION_TIME).SetEase(Ease.OutQuint);
                    
                    postAction?.Invoke();
                });

            }
            else
            {


                transform.DOMove(invisiblePos, Const.PANEL_ANIMATION_TIME).SetEase(Ease.OutQuint).OnComplete(() =>
                {
                    MapBoxManager.Instance.navigationMapScreen.GetComponent<RectTransform>()
                        .DOMove(arMapOriginalPosition, Const.PANEL_ANIMATION_TIME).SetEase(Ease.OutQuint);
                    
                    postAction?.Invoke();
                });
                

            }
            BottomPanelManager.Instance.MoveFollowedPanel(visible, height);
        }


        public void UpdateMessage(string msgStr)
        {
            if (messageText)
            {
                messageText.text = msgStr;
            }
        }

        public void ShowButton(bool show)
        {
            hidenButton.SetActive(show);
        }

        public void EnableButton(bool interactable)
        {
            print($"BottomPanel EnableButton interactable = {interactable}");
            if (!interactable)
            {
                hidenButton.GetComponent<Image>().color = new Color(0.768f, 0.768f, 0.768f, 1);
                hidenButton.GetComponent<Button>().interactable = false;
            }
            else
            {
                hidenButton.GetComponent<Image>().color = new Color(1, 0.568f, 0.345f, 1);
                hidenButton.GetComponent<Button>().interactable = true;
            }
        }

    }
}