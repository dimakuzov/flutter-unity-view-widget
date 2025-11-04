using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for InitPanel.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class InitFacade : UIFacade
    {

        public enum UIMode
        {
            UnDefined,
            Initializing,
            Inititialized
        }
        protected UIMode currentUIMode = UIMode.UnDefined;
        public UIMode GetUIMode()
        {
            return this.currentUIMode;
        }


        //InitPanel elements
        [SerializeField] GameObject newActivityButton;
        [SerializeField] GameObject searchActivityButton; //+
        [SerializeField] GameObject backToNativeButton; //+


        private static InitFacade _instance;
        public static InitFacade Instance
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
            mutualElements.Add(newActivityButton);
            mutualElements.Add(searchActivityButton);
            mutualElements.Add(backToNativeButton);

            bottomElements.Add(newActivityButton);
            bottomElements.Add(searchActivityButton);
            bottomElements.Add(backToNativeButton);
        }
        

        public void SetUIMode(UIMode uiMode)
        {
            if (this.currentUIMode != uiMode)
            {
                this.currentUIMode = uiMode;
                switch (uiMode)
                {
                    case UIMode.Initializing:
                        DeactiveAll();
                        break;

                    case UIMode.Inititialized:
                        DeactiveAll();
                        newActivityButton.SetActive(true);
                        searchActivityButton.SetActive(true);
                        backToNativeButton.SetActive(true);
                        break;

                    default:
                        DeactiveAll();
                        break;
                }
            }

            UpdateBottomBanner();
        }


    }

}
