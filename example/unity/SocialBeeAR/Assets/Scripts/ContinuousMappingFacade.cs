using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for ContinuousMappingPanel.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class ContinuousMappingFacade : UIFacade
    {

        public enum UIMode
        {
            UnDefined,
            Init,
            SavingMap
        }
        private UIMode currentUIMode = UIMode.UnDefined;


        //SelectMapPanel elements
        [SerializeField] GameObject continuosNewActivityButton; //+
        [SerializeField] GameObject saveMapButton; //+
        [SerializeField] GameObject cancelSavingButton; //+


        private static ContinuousMappingFacade _instance;
        public static ContinuousMappingFacade Instance
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
            mutualElements.Add(continuosNewActivityButton);
            mutualElements.Add(saveMapButton);
            mutualElements.Add(cancelSavingButton);

            bottomElements.Add(continuosNewActivityButton);
            bottomElements.Add(saveMapButton);
            bottomElements.Add(cancelSavingButton);
        }


        public UIMode GetUIMode()
        {
            return this.currentUIMode;
        }


        public void SetUIMode(UIMode uiMode)
        {
            if(this.currentUIMode != uiMode)
            {
                this.currentUIMode = uiMode;
                switch (uiMode)
                {
                    case UIMode.Init:
                        DeactiveAll();
                        cancelSavingButton.SetActive(true);
                        continuosNewActivityButton.SetActive(true);
                        saveMapButton.SetActive(true);
                        break;

                    case UIMode.SavingMap: //do not show any element when saving map
                        DeactiveAll();
                        break;

                    default:
                        DeactiveAll();
                        cancelSavingButton.SetActive(true);
                        continuosNewActivityButton.SetActive(true);
                        saveMapButton.SetActive(true);
                        break;
                }
            }

            UpdateBottomBanner();
        }

    }

}