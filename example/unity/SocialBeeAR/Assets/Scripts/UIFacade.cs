using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SocialBeeAR
{

    /// <summary>
    /// Parent class of all UIFacade for *Panels on the 2D UI
    /// </summary>
    public class UIFacade : MonoBehaviour
    {

        //logical elements
        protected List<GameObject> mutualElements = new List<GameObject>();
        protected List<GameObject> bottomElements = new List<GameObject>();


        protected void DeactiveAll()
        {
            foreach (GameObject element in mutualElements)
            {
                if(element)
                {
                    element.SetActive(false);
                }
            }
        }


        public bool IsAnyBottomButtonActive()
        {
            foreach (GameObject button in this.bottomElements)
            {
                if (button && button.activeSelf)
                    return true;
            }
            return false;
        }


        protected void UpdateBottomBanner()
        {

            bool isAnyElementActive = false;
            foreach (GameObject element in bottomElements)
            {
                if (element && element.activeSelf)
                {
                    isAnyElementActive = true;
                    break;
                }
            }

            UIManager.Instance.EnableBottomBanner(isAnyElementActive);
        }
        

    }

}

