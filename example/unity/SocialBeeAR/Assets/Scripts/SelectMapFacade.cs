using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for SelectMapPanel.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class SelectMapFacade : UIFacade
    {

        public enum UIMode
        {
            UnDefined,
            SelectMap_NoMapSelected,
            SelectMap_MapSelected,
            SelectMap_MapDeleted
        }
        private UIMode currentUIMode = UIMode.UnDefined;

        //SelectMapPanel elements
        [SerializeField] GameObject mapListPanel; //+
        public RectTransform mapListContentParent;
        public ToggleGroup mapListContentToggleGroup;
        public GameObject mapInfoElementPrefab;

        [SerializeField] GameObject cancelSelectingMapButton; //+
        [SerializeField] GameObject selectMapButtons;
        [SerializeField] GameObject loadMapButton; //+
        [SerializeField] GameObject deleteMapButton; //+


        private static SelectMapFacade _instance;
        public static SelectMapFacade Instance
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
            mutualElements.Add(mapListPanel);
            mutualElements.Add(cancelSelectingMapButton);
            mutualElements.Add(selectMapButtons);
            //allElements.Add(loadMapButton);
            //allElements.Add(deleteMapButton);

            //any button or button parent must be added into the list.
            bottomElements.Add(cancelSelectingMapButton);
            bottomElements.Add(selectMapButtons);
            //bottomElements.Add(loadMapButton);
            //bottomElements.Add(deleteMapButton);
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
                    case UIMode.SelectMap_NoMapSelected:
                        DeactiveAll();
                        mapListPanel.SetActive(true);
                        cancelSelectingMapButton.SetActive(true);
                        break;

                    case UIMode.SelectMap_MapSelected:
                        DeactiveAll();
                        mapListPanel.SetActive(true);
                        cancelSelectingMapButton.SetActive(true);
                        selectMapButtons.SetActive(true);
                        break;

                    case UIMode.SelectMap_MapDeleted: //disable buttons to avoid conflict operation
                        DeactiveAll();
                        mapListPanel.SetActive(true);
                        cancelSelectingMapButton.SetActive(true);
                        break;

                    default:
                        DeactiveAll();
                        mapListPanel.SetActive(true);
                        cancelSelectingMapButton.SetActive(true);
                        break;
                }
            }

            UpdateBottomBanner();
        }


    }

}