using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for ActivitySettingPanel.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class ActivitySettingFacade : UIFacade
    {

        public enum UIMode
        {
            UnDefined,
            ActivitySetting_PlacingReticle,
            ActivitySetting_SpawnedButNoActivitySeleted,
            ActivitySetting_SpawnedAndActivitySelected
        }
        private UIMode currentUIMode = UIMode.UnDefined;


        //SelectMapPanel elements
        [SerializeField] GameObject confirmActivitySettingButton; //+
        [SerializeField] GameObject cancelActivitySettingButton; //+

        //the animation for assistance
        [SerializeField] Animator tapDeviceAnimation;

        private static ActivitySettingFacade _instance;
        public static ActivitySettingFacade Instance
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
            mutualElements.Add(confirmActivitySettingButton);
            mutualElements.Add(cancelActivitySettingButton);

            bottomElements.Add(confirmActivitySettingButton);
            bottomElements.Add(cancelActivitySettingButton);
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
                    case UIMode.ActivitySetting_PlacingReticle:
                        DeactiveAll();
                        cancelActivitySettingButton.SetActive(true);
                        break;
                    
                    case UIMode.ActivitySetting_SpawnedButNoActivitySeleted:
                        DeactiveAll();
                        cancelActivitySettingButton.SetActive(true);
                        break;

                    case UIMode.ActivitySetting_SpawnedAndActivitySelected:
                        DeactiveAll();
                        cancelActivitySettingButton.SetActive(true);
                        confirmActivitySettingButton.SetActive(true);
                        break;

                    default:
                        DeactiveAll();
                        cancelActivitySettingButton.SetActive(true);
                        break;
                }
            }

            UpdateBottomBanner();
        }
        
        
        public void EnableTapAnimation(bool enable)
        {
            tapDeviceAnimation.SetTrigger(enable ? Const.ANIMATION_FADE_ON : Const.ANIMATION_FADE_OFF);
        }
        
    }

}