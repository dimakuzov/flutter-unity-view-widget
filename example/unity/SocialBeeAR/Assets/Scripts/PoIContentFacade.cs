using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{

    /// <summary>
    /// Facade class for Point-of-Interest content.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class PoIContentFacade : ContentFacade
    {
        [SerializeField] private GameObject poiBoard;

        public void InitContentMode(UIMode uiMode)
        {
            //init basic components
            base.Init(uiMode);
            
            //init activity specific components
            poiBoard.SetActive(true);
        }

        // public void EditPoIDesc()
        // {
        //     print("Start editing PoI description");
        //
        //     // Activate input field
        //     InputField input = GetComponentInChildren<InputField>();
        //     input.interactable = true;
        //     input.ActivateInputField();
        //
        //     input.onEndEdit.AddListener(delegate { OnEditPoIDescDone(input); });
        // }
        //
        // private void OnEditPoIDescDone(InputField input)
        // {
        //     print("End editing PoI description");
        //
        //     AnchorController myAnchorController = GetComponentInParent<AnchorController>();
        //     myAnchorController.OnEditPoIDescDone(input);
        // }
        
    }

}
