using System.Collections;
using System.Collections.Generic;
using System.IO;
using SocialBeeARDK;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for PhotoPanel.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class PhotoFacade : UIFacade
    {

        public enum UIMode
        {
            UnDefined,
            PhotoTaking,
            VideoTaking
        }
        private UIMode currentUIMode = UIMode.UnDefined;
        
        [SerializeField] private GameObject shutterButtonPhoto; //+
        [SerializeField] private GameObject shutterButtonVideo;
        [SerializeField] private GameObject switchCameraButton;
        [SerializeField] private GameObject cancelPhotoButton;
        [SerializeField] private RawImage previewImage;

        // [SerializeField] ARCameraBackground mArBackground;

        [HideInInspector]
        private static IAnchorManager ActiveAnchorManager =>
            SBContextManager.Instance.context.isCreatingGPSOnlyAnchors
                ? AnchorManager.Instance
                : WayspotAnchorManager.Instance;
        
        private static PhotoFacade _instance;
        public static PhotoFacade Instance
        {
            get
            {
                return _instance;
            }
        }


        private void Awake()
        {
            _instance = this;
            Categorize();
        }


        private void Categorize()
        {
            mutualElements.Add(shutterButtonPhoto);
            mutualElements.Add(shutterButtonVideo);
            mutualElements.Add(switchCameraButton);
            mutualElements.Add(cancelPhotoButton);

            bottomElements.Add(shutterButtonPhoto);
            bottomElements.Add(shutterButtonVideo);
            bottomElements.Add(switchCameraButton);
            bottomElements.Add(cancelPhotoButton);
        }


        public UIMode GetUIMode()
        {
            return this.currentUIMode;
        }


        public void SetUIMode(UIMode uiMode)
        {
            if (this.currentUIMode != uiMode)
            {
                this.currentUIMode = uiMode;
                switch (uiMode)
                {
                    case UIMode.PhotoTaking:
                        DeactiveAll();
                        shutterButtonPhoto.SetActive(true);
                        switchCameraButton.SetActive(true);
                        cancelPhotoButton.SetActive(true);
                        break;

                    case UIMode.VideoTaking:
                        DeactiveAll();
                        shutterButtonVideo.SetActive(true);
                        switchCameraButton.SetActive(true);
                        cancelPhotoButton.SetActive(true);
                        break;

                    default:
                        DeactiveAll();
                        shutterButtonPhoto.SetActive(true);
                        switchCameraButton.SetActive(true);
                        cancelPhotoButton.SetActive(true);
                        break;
                }
            }
            UpdateBottomBanner();
        }
        
        
        //TO-BE-INTEGRATED
        public void OnShutterButtonPhotoClicked()
        {
            print("OnShutterButtonPhotoClicked");
            
            //calling native app to launch the camera
            
            //callback
            OnPhotoTaken("");
        }

        public void OnPhotoTaken(string photoUrl)
        {
            UIManager.Instance.RestoreUIMode();
        }
        
        
        //TO-BE-INTEGRATED
        public void OnShutterButtonVideoClicked()
        {
            print("OnShutterButtonVideoClicked");
            
            //calling native app to launch the camera
            
            //callback
            OnVideoTaken(Application.streamingAssetsPath + "/SeattleMuseum.mp4");
        }

        public void OnVideoTaken(string videoUrl)
        {
            // AnchorManager.Instance.GetCurrentAnchorObject().GetComponent<AnchorController>().
            
            UIManager.Instance.RestoreUIMode();
            ActiveAnchorManager.GetCurrentAnchorObject().GetComponentInChildren<VideoContentFacade>().SetUIMode(VideoContentFacade.ContentMode.PrePostEdit);
        }

        public void OnCancelClicked()
        {
            print("OnCancelClicked");
            UIManager.Instance.RestoreUIMode();
        }

    }

}