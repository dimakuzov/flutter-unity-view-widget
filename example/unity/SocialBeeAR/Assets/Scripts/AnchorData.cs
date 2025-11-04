using System.Collections.Generic;
using Niantic.Lightship.AR.PersistentAnchors;
using Unity.VisualScripting;
using UnityEngine;

namespace SocialBeeAR
{

    //------------------------------- Anchor data (which to be saved in cloud) --------------------------------


    [System.Serializable]
    public class AnchorInfoList
    {
        // List of all anchors stored in the current Place.
        public AnchorInfo[] mapContent;
    }


    /// <summary>
    /// Class 'AnchorInfo' presents all the data that we should save to the storage for an anchor. The data structure
    /// inside is aligned with the JSON-format configuration file.
    /// </summary>
    [System.Serializable]
    public class AnchorInfo
    {
        /// <summary>
        /// The Id of the anchor in the AR session. 
        /// </summary>
        public string id;

        //public SBContext context;

        // name and description are the basic 'PoI' info of an anchor object
        public PostActivityInfo postInfo;

        //position and rotation
        public Pose pose;

        //this is the location when user 'complete' an anchor
        public ARLocation.Location locationInfo;

        // The thumbnail's local path.
        public string thumbnail;
        
        public List<IActivityInfo> activityInfoList;
        
        /// <summary>
        /// The anchor information specific to Niantic to which this post belongs to.
        /// </summary>
        public string anchorPayload { get; set; }
        public void SetAnchorPayload(ARPersistentAnchorPayload payload)
        {
            anchorPayload = payload.Serialize().ToString();
        }
        
        public AnchorInfo()
        {
            activityInfoList = new List<IActivityInfo>();
        }
    }


    //-------------------------------- Activity data (during anchor config) -------------------------------


    public class AnchorActivityInfo
    {
        public PostActivityInfo postInfo;
        public List<IActivityInfo> activityInfoList = new List<IActivityInfo>();        
    }


    public class PostInfo
    {
        public string id = "";
        public string name = "";
        public string description = "";
    }     
}