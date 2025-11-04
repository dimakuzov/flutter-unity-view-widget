using UnityEngine;
using UnityEngine.Video;

namespace SocialBeeAR
{

    /// <summary>
    /// Facade class for Video content of an anchor object.
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class VideoContentFacade : ContentFacade
    {

        public enum ContentMode
        {
            PrePostEdit, //this means before or after the editing(which happens on native UI)
            Editing, //at this moment, the iOS UI should pop-up and cover the whole screen
            Viewing_PrePostPlaying, //when view but not playing the video
            Viewing_Playing, //when playing the video
            Viewing_Pause //when pausing the video
        }
        
        //the board
        [SerializeField] private GameObject videoBoard;

        //the 'monitor'
        [SerializeField] private GameObject monitorCover;
        
        //buttons on video player
        [SerializeField] private GameObject playerButtonBoard;
        [SerializeField] private GameObject playButton;
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private GameObject replayButton;
        
        private string videoPath;

        public void InitUIMode(UIMode uiMode)
        {
            //init the basic components. Note: for 'edit' button, the setting will be covered by more detail settings with ContentMode
            base.Init(uiMode);
            
            //init activity specific components
            videoBoard.SetActive(true);

            if (uiMode != UIMode.Consumer) //for creator
                SetUIMode(ContentMode.PrePostEdit); //this is the default mode for creator
            else //for consumer
                SetUIMode(ContentMode.Viewing_PrePostPlaying); //this is the default mode for consumer
        }

        public void SetUIMode(ContentMode mode)
        {
            switch (mode)
            {
                case ContentMode.PrePostEdit:
                    this.editButton.SetActive(true);
                    
                    this.playerButtonBoard.SetActive(false);
                    this.playButton.SetActive(false);
                    this.pauseButton.SetActive(false);
                    this.replayButton.SetActive(false);
                    
                    this.monitorCover.SetActive(true);
                    break;
                
                case ContentMode.Editing: 
                    this.editButton.SetActive(false);
                    
                    this.playerButtonBoard.SetActive(false);
                    this.playButton.SetActive(false);
                    this.pauseButton.SetActive(false);
                    this.replayButton.SetActive(false);
                    
                    this.monitorCover.SetActive(true);
                    break;
                
                case ContentMode.Viewing_PrePostPlaying:
                    this.editButton.SetActive(false);
                    
                    this.playerButtonBoard.SetActive(true);
                    this.playButton.SetActive(true);
                    this.pauseButton.SetActive(false);
                    this.replayButton.SetActive(false);
                    
                    this.monitorCover.SetActive(true);
                    break;
                
                case ContentMode.Viewing_Playing:
                    this.editButton.SetActive(false);
                    
                    this.playerButtonBoard.SetActive(true);
                    this.playButton.SetActive(false);
                    this.pauseButton.SetActive(true);
                    this.replayButton.SetActive(true);
                    
                    this.monitorCover.SetActive(false);
                    break;
                
                case ContentMode.Viewing_Pause:
                    this.editButton.SetActive(false);
                    
                    this.playerButtonBoard.SetActive(true);
                    this.playButton.SetActive(true);
                    this.pauseButton.SetActive(false);
                    this.replayButton.SetActive(true);
                    
                    this.monitorCover.SetActive(false);
                    break;
            }
        }
        
        
        public void EditVideo()
        {
            print("Start taking video.");
            
            //update content mode
            this.SetUIMode(ContentMode.Editing);
            
            //update UI mode
            UIManager.Instance.SetUIMode(UIManager.UIMode.PhotoVideo);
            PhotoFacade.Instance.SetUIMode(PhotoFacade.UIMode.VideoTaking);
            
            //call native UI for video taking here...
        }
        
        private void OnEditVideoDone(string videoPath)
        {
            print("Taking video done");
            
            //set the path of the video to be played
            // string filePath = "file://" + Application.streamingAssetsPath + "/" + "SeattleMuseum.mp4";
            // this.videoPath = filePath;
            
            //update content mode
            this.SetUIMode(ContentMode.PrePostEdit);
        }
        
        public void OnPlayButtonClicked()
        {
            print("Start playing video.");
            VideoPlayer player = gameObject.GetComponentInChildren<VideoPlayer>();
            player.Play();
            
            //update content mode
            this.SetUIMode(ContentMode.Viewing_Playing);
        }
        
        public void OnPauseButtonClicked()
        {
            print("Pausing video.");
            VideoPlayer player = gameObject.GetComponentInChildren<VideoPlayer>();
            player.Pause();
            
            //update content mode
            this.SetUIMode(ContentMode.Viewing_Pause);
        }
        
        public void OnReplayButtonClicked()
        {
            print("Replaying video.");
            VideoPlayer player = gameObject.GetComponentInChildren<VideoPlayer>();
            player.Stop();
            player.Play();
            
            //update content mode
            this.SetUIMode(ContentMode.Viewing_Playing);
        }

    }

}
