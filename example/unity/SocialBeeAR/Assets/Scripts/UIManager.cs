using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GifPlayer;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace SocialBeeAR
{

    /// <summary>
    /// UIManager is the top-level class for accessing any 2D UI elements.
    /// </summary>
    public class UIManager : UIFacade
    {
        
        /// <summary>
        /// UI 'mode' of the app. The same UI mode + 'UI' postfix can be found in Unity hierarchy
        /// </summary>
        public enum UIMode
        {
            Undefined,
            Entrance, //EntranceUI in hierarchy, just the entrance for testing.
            Activity, //ActivityUI in hierarchy, for both activity creation or consuming
            PhotoVideo, // PhotoVideoUI in hierarchy, for photo/video taking
            Audio, // AudioUI in hierarchy, for audio taking

            // //Todo: to be removed
            Init,
            SelectMap,
            ActivitySetting,
            Mapping,
            ContinuousMapping,
            Localization
        }
        

        //UIs
        [SerializeField] public GameObject entranceUI;
        [SerializeField] private GameObject activityUI;
        [SerializeField] private GameObject photoVideoUI;
        [SerializeField] private GameObject audioUI;

        //Other elements
        [SerializeField] private GameObject topPanel;
        [SerializeField] private GameObject coverPanel;
        [SerializeField] private GameObject debugPanel;
        // [SerializeField] private GameObject bottomPanelFixed;
        [SerializeField] private UnityGif initGif;
        [SerializeField] private GameObject initCover;
        [SerializeField] private GameObject onlineStatus;
        [SerializeField] private GameObject offlineStatus;

        [Header("Characters Limit Alert")]
        [SerializeField] private Text text;
        [SerializeField] GameObject panel;
        private InputField inputField;
        private bool trackCharactersLimit;

        private UIMode currentUIMode = UIMode.Undefined; //the current UI mode
        private UIMode lastUIMode = UIMode.Undefined; //for being able to switch back to the last mode
        
        //for fade in UI
        private IEnumerator fadeThread;
        private float defaultFadeTime = 0.5f;
        

        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                return _instance;
            }
        }
        
        
        private void Awake()
        {
            // print("#awake");
            _instance = this;
        }


        private void Start()
        {
            print("#debugStart: UIManager");
            Categorize();
            // print("entranceUI.SetActive...");
            // entranceUI.SetActive(false);
            // print("entranceUI.SetActive DONE.");
        }

        void Update()
        {
            if (trackCharactersLimit) {
                if (inputField.text.Length >= (int)((inputField.characterLimit/5.0f) * 4)) {
                    panel.SetActive(true);
                    text.text = $"Сharacters limit is {inputField.characterLimit}\nthe length of your text is {inputField.text.Length}.";
                }
                else {
                    panel.SetActive(false);
                }
            }
        }

        private void Categorize()
        {
            //UIs
            mutualElements.Add(entranceUI);
            mutualElements.Add(activityUI);
            
            mutualElements.Add(photoVideoUI);
            mutualElements.Add(audioUI);
            
            //common elements
            mutualElements.Add(topPanel);
            mutualElements.Add(coverPanel);
            mutualElements.Add(debugPanel);
            // mutualElements.Add(bottomPanelFixed);
        }


        //-----------------------------Other methods----------------------------
        

        public void SetUIMode(UIMode uiMode)
        {
            if (uiMode == currentUIMode) return;
            this.lastUIMode = currentUIMode; //save as the last UI mode
            this.currentUIMode = uiMode;
                
            DeactiveAll();
            RecordManager.Instance.isRecordUI = false;
                
            //apply change
            switch (uiMode)
            {
                case UIMode.Entrance:
                    //Todo: keep entrance UI for standalone mode
                    entranceUI.SetActive(true);
                    Instance.HideLoader();
                    Instance.FadeOutInitCover();
                    break;
                    
                case UIMode.Activity:
                    topPanel.SetActive(true);
                    activityUI.SetActive(true);
                    debugPanel.SetActive(true);
                    break;

                case UIMode.PhotoVideo:
                    photoVideoUI.SetActive(true);
                    debugPanel.SetActive(true);
                    RecordManager.Instance.isRecordUI = true;
                    break;

                case UIMode.Undefined:
                    debugPanel.SetActive(true);
                    break;
                    
                case UIMode.Audio:
                    audioUI.SetActive(true);
                    debugPanel.SetActive(true);
                    break;
            }
        }

        public void DeactivateScreens()
        {
            DeactiveAll();
        }
        

        public UIMode GetCurrentUIMode()
        {
            return this.currentUIMode;
        }
        

        public void RestoreUIMode()
        {
            this.SetUIMode(this.lastUIMode);
        }
        

        public void UpdateSBContext()
        {
            SBContext context = SBContextManager.Instance.context;

            TopPanelFacade topPanelFacade = topPanel.GetComponent<TopPanelFacade>();
            topPanelFacade.experienceNameText.text = context.TitleToDisplay;
            //topPanelFacade.collectionNameText.text = context.collectionName;
        }

        public void SetTopBar(string title) {
            TopPanelFacade topPanelFacade = topPanel.GetComponent<TopPanelFacade>();
            topPanelFacade.experienceNameText.text = title;
        }
        
        
        public void EnableBottomBanner(bool enabled)
        {
            // this.bottomPanelFixed.SetActive(enabled);
        }


        public void StartCameraUI()
        {
            StartCoroutine(DoStartCameraUI());
        }
        
        
        private IEnumerator DoStartCameraUI()
        {
            //yield return new WaitForSeconds(transformTime + 0.1f);
            yield return new WaitForSeconds(0.1f);
            UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
        }
        
        
        public void StartAudioUI()
        {
            SetUIMode(UIManager.UIMode.Audio);
        }


        public void FadeInCameraUI()
        {
            if (photoVideoUI.activeSelf == true)
                return;
			
            if (fadeThread != null)
            {
                return;
                //StopCoroutine(fadeInThread);
            }
            fadeThread = FadeInAlpha(photoVideoUI);
            StartCoroutine(fadeThread);
        }
        
        
        public void FadeInAudioUI()
        {
            if (audioUI.activeSelf == true)
                return;
			
            if (fadeThread != null)
            {
                return;
                //StopCoroutine(fadeInThread);
            }
            fadeThread = FadeInAlpha(audioUI);
            StartCoroutine(fadeThread);
        }


        public void FadeOutInitCover()
        {
            if (initCover.activeSelf == false)
                return;
			
            if (fadeThread != null)
            {
                return;
                //StopCoroutine(fadeInThread);
            }
            
            fadeThread = FadeOutAlpha(initCover, 0.75f, 1f);
            StartCoroutine(fadeThread);
        }
        

        IEnumerator FadeInAlpha(GameObject uiObj, float initAlpha = -1, float fadeTime = -1)
        {
            //safe check
            if (uiObj == null)
                yield return null;
            CanvasGroup canvasGroup = uiObj.GetComponent<CanvasGroup>();
            if(canvasGroup == null)
                yield return null;
            
            //enable the UI, but set alpha as 0
            if(uiObj == photoVideoUI)
                UIManager.Instance.SetUIMode(UIMode.PhotoVideo);
            else if(uiObj == audioUI)
                UIManager.Instance.SetUIMode(UIMode.Audio);
            
            canvasGroup.alpha = (initAlpha == -1 ? 0 : initAlpha);
            yield return new WaitForSeconds(0.1f);
            
            //fade in
            float finalFadeTime = fadeTime == -1 ? defaultFadeTime : fadeTime;
            while (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.deltaTime / finalFadeTime;
                yield return null;
            }

            //done
            fadeThread = null;
            yield return null;
        }
        
        
        IEnumerator FadeOutAlpha(GameObject uiObj, float initAlpha = -1, float fadeTime = -1)
        {
            //safe check
            if (uiObj == null)
                yield return null;
            CanvasGroup canvasGroup = uiObj.GetComponent<CanvasGroup>();
            if(canvasGroup == null)
                yield return null;
            
            //enable the UI, but set alpha as 1
            if(uiObj == photoVideoUI)
                UIManager.Instance.SetUIMode(UIMode.PhotoVideo);
            else if(uiObj == audioUI)
                UIManager.Instance.SetUIMode(UIMode.Audio);
            
            canvasGroup.alpha = (initAlpha == -1 ? 1f : initAlpha);
            yield return new WaitForSeconds(0.1f);
            
            //fade out
            float finalFadeTime = fadeTime == -1 ? defaultFadeTime : fadeTime;
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.deltaTime / finalFadeTime;
                yield return null;
            }

            //done
            fadeThread = null;
            yield return null;
        }

        
        public void HideLoader() 
        {
            initGif.Pause();
            initGif.gameObject.SetActive(false);
        }


        public void StartTrack(InputField input) {
            trackCharactersLimit = true;
            inputField = input;
        }
    
        public void FinishTrack() {
            trackCharactersLimit = false;
            panel.SetActive(false);
        }

        public void ShowOfflineStatus(bool offline, bool hideAll = false) {
            if (hideAll) {
                onlineStatus.SetActive(false);
                offlineStatus.SetActive(false);
            }
            else {
                onlineStatus.SetActive(!offline);
                offlineStatus.SetActive(offline);
            }
        }
    }

}


