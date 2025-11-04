using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace SocialBeeAR
{

    /// <summary>
    /// MessageManager is the class for all kinds of messages, including notification message or debug message.
    /// </summary>
    public class MessageManager : BaseSingletonClass<MessageManager>
    {
        //The debug panel
        [SerializeField] private GameObject debugPanel;
        private DebugPanelController debugPanelController;
        [SerializeField] private bool enableOnScreenDebug = true;

        //The notification in top for end users
        [SerializeField] Text messageText;
        
        //other debug info elements
        [SerializeField] private Text infoMapSizeText;
        [SerializeField] private GameObject infoMappingQualityTextParent;
        private Text infoMappingQualityText;

        [SerializeField] private GameObject infoStatusParent;
        private Text infoMode;
        private Text infoStatus;

        [SerializeField] private Text infoAnchorSizeText;

        [SerializeField] private Text arTrackingStateText;
        [SerializeField] private Text arNotTrackingReasonText;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private Text cameraPosText;

        [SerializeField] private Text engagedAnchorIndex;
        [SerializeField] private Text closestAnchorIndex;
        [SerializeField] private Text currentAnchorObjIdx;

        private NativeCall nativeCall;


        private void Awake()
        {
            if (debugPanel != null)
                debugPanelController = debugPanel.GetComponent<DebugPanelController>();

            if (infoMappingQualityTextParent != null)
                infoMappingQualityText = infoMappingQualityTextParent.transform.GetChild(1).GetComponent<Text>();

            if (infoStatusParent != null)
            {
                infoMode = infoStatusParent.transform.GetChild(1).GetComponent<Text>();
                infoStatus = infoStatusParent.transform.GetChild(3).GetComponent<Text>();
            }
        }


        private void Start()
        {
            nativeCall = NativeCall.Instance;
        }


        private void Update()
        {
            //UpdateCurrentAnchorInfo();
        }
        
        
        //------------------------Notification for users------------------------

        public void ShowMessage(string message)
        {
            if (messageText)
            {
                // MainThreadTaskQueue.InvokeOnMainThread(() =>
                // {
                //     messageText.text = message;
                // });
                messageText.text = message;
            }
        }

        //------------------------Debug panel-----------------------------------

        public void DebugMessage(string msgLine, bool showInConsoleOnly = true)
        {
            
#if !DEBUG && !DEVELOPMENT_BUILD
return;
#endif
            if (showInConsoleOnly)
            {
                nativeCall.DebugMessage(msgLine);
                return;
            }

            if (debugPanelController == null || !enableOnScreenDebug) return;
            
            string[] msgLineArr = msgLine.Split(Environment.NewLine.ToCharArray());
            if (msgLineArr != null && msgLineArr.Length > 0)
            {
                foreach (string line in msgLineArr)
                {
                    // MainThreadTaskQueue.InvokeOnMainThread(() =>
                    // {
                    //     debugPanelController.PushMessage(line);
                    // });   
                    debugPanelController.PushMessage(line);
                }
            }
            else
            {
                // MainThreadTaskQueue.InvokeOnMainThread(() =>
                // {
                //     debugPanelController.PushMessage(msgLine);
                // });    
                debugPanelController.PushMessage(msgLine);
            }
        }
        
        public void ClearDebug()
        {
            if (debugPanelController != null)
            {
                debugPanelController.Clear();
            }
        }

        //------------------------Other debug info------------------------------

        public void UpdateMapSize(int mapLength)
        {
            if (this.infoMapSizeText)
            {
                this.infoMapSizeText.text = mapLength.ToString();
            }
        }

        public void UpdateCurrentAnchorSize(int currentAnchorSize)
        {
            if(infoAnchorSizeText)
            {
                infoAnchorSizeText.text = currentAnchorSize.ToString();
            }   
        }


        public void UpdateMappingQuality(string mappingQuality)
        {
            if (this.infoMappingQualityText)
            {
                this.infoMappingQualityText.text = mappingQuality;
            }
        }


        public void EnableMappingQualityInfo(bool enabled)
        {
            if(infoMappingQualityTextParent)
            {
                infoMappingQualityTextParent.SetActive(enabled);
            }
        }


        public void UpdateStatus(string mode, string status)
        {
            if (infoMode)
                this.infoMode.text = mode;

            if (infoStatus)
                this.infoStatus.text = status;
        }


        public void UpdateCameraInfo()
        {
            if (mainCamera == null)
                return;
            cameraPosText.text = mainCamera.transform.position.ToString();
        }


        public void UpdateTrackingState(string arTrackingState)
        {
            if (arTrackingStateText)
                arTrackingStateText.text = arTrackingState;
        }
        
        
        public void UpdateNotTrackingReason(string arNotTrackingReason)
        {
            if (arNotTrackingReasonText)
                arNotTrackingReasonText.text = arNotTrackingReason;
        }
        

        // public void UpdateCurrentAnchorInfo()
        // {
        //     engagedAnchorIndex.text = AnchorManager.Instance.engagedAnchorIndex.ToString();
        //     closestAnchorIndex.text = AnchorManager.Instance.closestAnchorIndex.ToString();
        //     
        //     GameObject currentAnchorObj = AnchorManager.Instance.GetCurrentAnchorObject();
        //     if (currentAnchorObj)
        //         this.currentAnchorObjIdx.text = currentAnchorObj.GetComponent<AnchorController>().index.ToString();
        //     else
        //         this.currentAnchorObjIdx.text = "-1";
        // }
        
    }

}


