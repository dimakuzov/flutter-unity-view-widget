using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for LocalizationPanel.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class LocalizationFacade : UIFacade
    {
        
        public enum UIMode
        {
            UnDefined,
            Localization_Unlocalized,
            Localization_Localized
        }
        private UIMode currentUIMode = UIMode.UnDefined;


        //SelectMapPanel elements
        public RawImage mapThumbnail;
        [SerializeField] GameObject localizationButtons;
        [SerializeField] GameObject exitMapButton; //+
        [SerializeField] GameObject extendMapButton; //+
        [SerializeField] GameObject updateAcvitityButton; //+


        private static LocalizationFacade _instance;
        public static LocalizationFacade Instance
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
            mutualElements.Add(localizationButtons);
            mutualElements.Add(mapThumbnail.gameObject);

            bottomElements.Add(localizationButtons);
            //bottomElements.Add(exitMapButton);
            //bottomElements.Add(extendMapButton);
            //bottomElements.Add(updateAcvitityButton);
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
                    case UIMode.Localization_Unlocalized:
                        DeactiveAll();
                        localizationButtons.SetActive(false);
                        mapThumbnail.gameObject.SetActive(true);
                        break;

                    case UIMode.Localization_Localized:
                        DeactiveAll();
                        localizationButtons.SetActive(true);
                        break;

                    default:
                        DeactiveAll();
                        localizationButtons.SetActive(false);
                        mapThumbnail.gameObject.SetActive(true);
                        break;
                }
            }
            UpdateBottomBanner();
        }

    }

}