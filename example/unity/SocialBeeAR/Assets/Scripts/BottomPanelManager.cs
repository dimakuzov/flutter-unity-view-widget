using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


namespace SocialBeeAR
{
    
    /// <summary>
    /// BottomPanelManager is the manager controlling UI panels shown in the bottom area.
    /// </summary>
    public class BottomPanelManager : BaseSingletonClass<BottomPanelManager>
    {
        private GameObject currentVisiblePanel;
        [HideInInspector] public bool isActivePanel;
        [HideInInspector] public bool isOffScreenPanel;

        //This is the 'cover panel' to cover the screen to avoid user interaction when message/dialog is shown
        [SerializeField] private GameObject coverPanel;
        [SerializeField] private GameObject messagePanel;

        [SerializeField] private Transform[] followedPanels;
        private Vector3[] initPoses;

        private void Start() {
            initPoses = new Vector3[followedPanels.Length];
            for (int i = 0; i < followedPanels.Length; i++) {
                initPoses[i] = followedPanels[i].position;
            }
        }

        public void ShowPanel(GameObject bottomPanel, bool showCover, Action postAction = null)
        {
            isActivePanel = true;
            isOffScreenPanel = false;
            //show with animation
            if (currentVisiblePanel) //hide existing first, then show
            {
                //disable cover panel
                if(coverPanel && coverPanel.activeSelf)
                    coverPanel.SetActive(false);
                
                BottomPanel panel = currentVisiblePanel.GetComponent<BottomPanel>();
                if (panel)
                {
                    panel.SetVisible(false, () =>
                    {
                        currentVisiblePanel.SetActive(false);
                    
                        //then the new one
                        currentVisiblePanel = bottomPanel;
                        currentVisiblePanel.SetActive(true);
                        
                        currentVisiblePanel.GetComponent<BottomPanel>().SetVisible(true, () =>
                        {
                            if(coverPanel)
                                coverPanel.SetActive(showCover); //enable 'cover' of the screen if needed
                            
                            if(postAction != null)
                                postAction.Invoke();
                        });
                    });
                }
            }
            else //show
            {
                currentVisiblePanel = bottomPanel;
                currentVisiblePanel.SetActive(true);
                BottomPanel panel = bottomPanel.GetComponent<BottomPanel>();
                
                if (panel)
                {
                    panel.SetVisible(true, () =>
                    {
                        if(coverPanel)
                            coverPanel.SetActive(showCover); //enable 'cover' of the screen if needed
                        
                        if(postAction != null)
                            postAction.Invoke();
                    });    
                }
            }
        }


        public void HideCurrentPanel(Action postAction)
        {
            if (currentVisiblePanel == null)
            {
                if(postAction != null)
                    postAction.Invoke();
                return;
            }
            print("HideCurrentPanel #hidepanel");
            //disable cover panel
            if (coverPanel && coverPanel.activeSelf)
            {
                coverPanel.SetActive(false);
            }

            BottomPanel bottomPanel = currentVisiblePanel.GetComponent<BottomPanel>();
            if (bottomPanel)
            {
                bottomPanel.SetVisible(false, () =>
                {
                    isActivePanel = false;
                    isOffScreenPanel = false;
                    
                    //set it invisible
                    currentVisiblePanel.SetActive(false);

                    //reset reference
                    currentVisiblePanel = null;

                    if(postAction != null)
                        postAction.Invoke();
                });
            }
            else
            {
                if(postAction != null)
                    postAction.Invoke();
            }
        }


        public void HideCurrentPanel()
        {
            HideCurrentPanel(null);
        }


        public void UpdateMessage(string msgStr)
        {
            if (currentVisiblePanel)
            {
                currentVisiblePanel.GetComponent<BottomPanel>().UpdateMessage(msgStr);
            }
        }
        
        
        public void ShowMessagePanel(string message, bool showCover, bool autoClose, Action postAction = null)
        {
            print($"ShowMessagePanel > autoClose={autoClose} | message={message}");
            messagePanel.GetComponent<BottomPanel>().UpdateMessage(message);

            if(autoClose)
                ShowPanel(messagePanel, showCover);
            else
                ShowPanel(messagePanel, showCover, postAction);

            if (autoClose)
                StartCoroutine(AutoClose(postAction));
        }
        
        
        public void ShowMessagePanel(string message, bool autoClose)
        {
            ShowMessagePanel(message, false, autoClose, null);
        }

        public void ShowMessagePanel(string message)
        {
            ShowMessagePanel(message, false, false, null);
        }

        private IEnumerator AutoClose(Action postAction)
        {
            print("AutoClose #hidepanel");
            yield return new WaitForSeconds(2f);
            HideCurrentPanel(() =>
            {
                postAction?.Invoke();    
            });
        }
        
        
        public GameObject GetCurrentVisiblePanel()
        {
            return this.currentVisiblePanel;
        }


        public void MoveFollowedPanel(bool moveUp, float height) {
            if (moveUp) {
                for (int i = 0; i < followedPanels.Length; i++) {
                    if(followedPanels[i].gameObject.activeInHierarchy) {
                        print($"panel.gameObject.name = {followedPanels[i].gameObject.name}, is active & panel.position.y <= 0)");
                        followedPanels[i].DOMove(initPoses[i] + new Vector3(0, height, 0), Const.PANEL_ANIMATION_TIME)
                            .SetEase(Ease.OutQuint).OnComplete(
                                () => { });
                    }
                }
            }
            else {
                for (int i = 0; i < followedPanels.Length; i++) {
                    if(followedPanels[i].position != initPoses[i]) {
                        print($"panel.gameObject.name = {followedPanels[i].gameObject.name}, panel.position.y > 0");
                        followedPanels[i].DOMove(initPoses[i], Const.PANEL_ANIMATION_TIME)
                            .SetEase(Ease.OutQuint).OnComplete(
                                () => { });
                    }
                }
            }
        }
        
    }

}





