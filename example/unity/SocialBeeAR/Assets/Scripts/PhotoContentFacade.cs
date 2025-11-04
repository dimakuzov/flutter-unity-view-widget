using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{

    /// <summary>
    /// Facade class for Photo taking content of an anchor object
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class PhotoContentFacade : ContentFacade
    {

        enum ContentMode
        {
            Editing,
            Viewing
        }
        
        
        [SerializeField] private GameObject photoSetting;
        [SerializeField] private GameObject photoPlay;

        public void InitContentMode(UIMode uiMode)
        {
            //init basic components
            base.Init(uiMode);
            
            //init activity specific components
            if (uiMode != UIMode.Consumer) //for creator
            {
                this.photoSetting.SetActive(true);
                this.photoPlay.SetActive(false);
            }
            else //for consumer
            {
                this.photoSetting.SetActive(false);
                this.photoPlay.SetActive(true);
            }
        }
        
        
        public void EditPhoto()
        {
            print("Start taking photo.");
            
            UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
            PhotoFacade.Instance.SetUIMode(PhotoFacade.UIMode.PhotoTaking);
            
            //call native UI for photo taking here...
        }
        

        private void OnEditPhotoDone(string photoPath)
        {
            print("Taking photo done");
            
            //update photo image
            
        }


        public void TestLoadPhoto()
        {
            
        }


        public void OnPhotoLoaded(byte[] photo)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(photo);
            
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            GetComponent<Image>().sprite = sprite;
        }


    }

}
