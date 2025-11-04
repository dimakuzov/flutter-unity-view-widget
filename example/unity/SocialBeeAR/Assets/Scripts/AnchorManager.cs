using System;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.PersistentAnchors;
using Niantic.Lightship.AR.Subsystems.PersistentAnchor;
using SocialBeeARDK;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;


namespace SocialBeeAR
{

    /// <summary>
    /// This class manages the instance of all the anchor('Activity') objects
    /// </summary>
    public class AnchorManager : BaseAnchorManager<AnchorManager>, IAnchorManager  
    {
        // public ARAnchorManager arRefPointManager;
        // [SerializeField] ARRaycastManager raycastManager;
        
        //Anchor object list
        private Dictionary<TrackableId, ARPersistentAnchor> _addedAnchors = new Dictionary<TrackableId, ARPersistentAnchor>();
        private List<GameObject> anchorObjList = new List<GameObject>();


        private void Update()
        {
            if (SBContextManager.Instance.context is not { isCreatingGPSOnlyAnchors: true }) return;
            
            //checking interaction
            HandleAnchorInteraction();

            // print($"isEditing={SBContextManager.Instance.context.isEditing} | spawnedAnchors={spawnedAnchors} | anchorsToSpawn={anchorsToSpawn}");
            if (SBContextManager.Instance.context.isEditing && spawnedAnchors < anchorsToSpawn)
            {
                return;
            }
            //check anchor engagement
            CheckDistance(null);
        }

        
        public void PlaceAnchor(Vector3 touchPosition)
        {
            if (SBContextManager.Instance.context!=null && SBContextManager.Instance.context.plannedLocation != null)
            {
                print($"The thumbail for the Google Map Location panel: {SBContextManager.Instance.context.plannedLocation.Thumbnail}");                
            }            
        
            // 0.0 Add anchor to the AR session.
            // var anchor = ARHelper.Instance.Session.AddAnchor(Matrix4x4.TRS(touchPosition, Quaternion.identity, Vector3.one));
            // _addedAnchors.Add(anchor.Identifier, anchor);
            
            //1. Instantiate new anchor prefab and set transform.
            var anchorObj = Instantiate(AnchorObjectPrefab);
            
            //2. creating an initial anchorInfo object
            var actInfoList = new List<IActivityInfo>();
            var anchorId = Utilities.GenerateAnchorId();
            var anchorPose = new Pose(touchPosition, Quaternion.identity);
            var initialAnchorInfo = new AnchorInfo //prepare an empty anchorInfo
            {
                id = anchorId,
                //context = SBContextManager.Instance.context,
                postInfo = new PostActivityInfo(), // PostInfo(),
                pose = anchorPose,
                activityInfoList = actInfoList
            };
            if (SBContextManager.Instance.context is { isPlanning: true })
                initialAnchorInfo.postInfo.MapLocation = SBContextManager.Instance.context.plannedLocation;
        
            //3. init anchor
            var anchorController = anchorObj.GetComponent<AnchorController>();
            var index = anchorObjList.Count;
            anchorController.Born(index, initialAnchorInfo);
        
            //4. add to the anchor list
            anchorObjList.Add(anchorObj);
            print($"New anchor spawned, index: '{index}'");
        
            SpawnAnchorFinalAction(index, anchorController);
        }
        
        //-------------------------------- Spawn & Reborn AnchorObject-------------------------
         
        public void YaxisCorrection()
        {
            //Y-axis correction
            MessageManager.Instance.DebugMessage("Y-axis correction: Start up correction...");
            Vector3 normalFromPlaneDetection = PlaneManager.Instance.GetGroundNormal();
            
            Vector3 normalOfFloor = _arSpace.transform.up;

            float angle = Vector3.Angle(normalOfFloor, normalFromPlaneDetection);
            MessageManager.Instance.DebugMessage($"Y-axis correction: Normal error angle = {angle}");

            //Correcting rotation by rotating the whole AR content space (stick floor's Y to plane's Y)            
            Quaternion rotation = Quaternion.FromToRotation(normalOfFloor, normalFromPlaneDetection);
            _arSpace.transform.rotation = rotation * _arSpace.transform.rotation;

            //Correcting the error(position change) caused by the last step (correcting the rotation).
            Transform originT = _arSpace.transform.Find("Origin");
            Vector3 offsetBecauseOfRotation = originT.position * -1;
            _arSpace.transform.position += offsetBecauseOfRotation;
        }


        public void AddARAnchors()
        {
            for (int i = 0; i < anchorObjList.Count; i++)
            {
                AnchorController controller = anchorObjList[i].GetComponent<AnchorController>();
                controller.AddARAnchor();
            }
        }

        #region Override methods

        public override int AnchorCount => anchorObjList.Count;

        public override IEnumerable<string> GetActivityIds()
        {
            print($"AnchorManager.GetActivities: count={AnchorCount}");
            return anchorObjList.SelectMany(x =>
            {
                var info = x.GetComponent<AnchorController>().GetAnchorInfo();
                var ids = info.activityInfoList.Select(a => a.Id);

                return ids;
            });

        }

        /// <summary>
        /// Load anchors from the SBContext.
        /// </summary>
        public override void LoadAnchors(AnchorInfoList anchorList)
        {
            print("AnchorManager > LoadAnchors started");
            ClearAnchors();

            if (anchorList.mapContent == null)
            {
                print("No anchors created!");
                return;
            }

            spawnedAnchors = 0;
            anchorsToSpawn = anchorList.mapContent.Length;
            //reborn anchor objects
            foreach (var anchorInfo in anchorList.mapContent)
            {
                print($"LoadAnchors: post desc = {anchorInfo.postInfo.Description}");
                RebornAnchorObject(anchorInfo);
                spawnedAnchors++;
            }
        }
        
        /// <summary>
        /// Re-create an anchor object when edit/consume
        /// </summary>
        /// <param name="info"></param>
        /// <param name="initialIndex"></param>
        private void RebornAnchorObject(AnchorInfo info, int? initialIndex = null)
        {
            var anchorObj = Instantiate(AnchorObjectPrefab);
            anchorObj.name = info.postInfo.Title;

            //init anchor
            var anchorController = anchorObj.GetComponent<AnchorController>();
            //var index = initialIndex ?? (AnchorCount - 1);
            // anchorController.Reborn(index, info, arSpace, ()=>
            // {
            //     if(SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
            //         anchorObj.SetActive(false);
            // }
            print($"initialIndex={initialIndex} | AnchorCount={AnchorCount} #radebug ");
            anchorController.Reborn(initialIndex ?? (AnchorCount - 1), info, ()=>
            {
                // We should not control the visibility of an anchor here.
                // if(SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
                //     anchorObj.SetActive(false);
            });

            //add to the anchor list
            AddAnchorObjectToList(anchorObj);
        }

        protected override GameObject GetAnchorObjectAt(int index)
        {
            print($"#debuglockanchor anchors={AnchorCount} | index={index}");
            return index >= AnchorCount ? null : anchorObjList[index];
        }

        protected override void RemoveAnchorObjectAt(int index)
        {
            anchorObjList.RemoveAt(index);
        }

        protected override void AddAnchorObjectToList(GameObject anchor)
        {
            //add to the anchor list
            anchorObjList.Add(anchor);
        }

        public override List<GameObject> GetAnchorObjectList()
        {
            return anchorObjList;
        }
        
        public override void ClearAnchors()
        {
            foreach (var obj in this.anchorObjList)
                Destroy(obj);
            
            this.anchorObjList.Clear();
            
            base.ClearAnchors();
            
        }

        public override bool IsReadyToSaveMap()
        {
            //there must have at least one anchor is completed (config completed)
            return AnchorCount > 0 && anchorObjList.Any(t => t.GetComponent<AnchorController>().isConfigCompleted);
        }

        #endregion
        
        //--------------- Register current selected anchor object -----------------
        
        
        //------------------------  Converting to JSON --------------------------

        // /// <summary>
        // /// Converting AnchorInfo list to StartAREditingHelper: mode=JSON data
        // /// </summary>
        // /// <returns></returns>
        // public JObject AnchorInfoListToJSON()
        // {
        //     //prepare anchor info list
        //     List<AnchorInfo> anchorInfoList = new List<AnchorInfo>();
        //     foreach (GameObject anchorObj in anchorObjList)
        //     {
        //         AnchorInfo info = anchorObj.GetComponent<AnchorController>().GetAnchorInfo();
        //         anchorInfoList.Add(info);
        //     }
        //     
        //     //prepare an array for converting to JSON
        //     AnchorInfoList tempAnchorList = new AnchorInfoList
        //     {
        //         mapContent = new AnchorInfo[anchorInfoList.Count]
        //     };
        //     
        //     for (int i = 0; i < anchorInfoList.Count; ++i)
        //     {
        //         tempAnchorList.mapContent[i] = anchorInfoList[i];
        //     }
        //
        //     //convert to JSON
        //     JObject jObject = JObject.FromObject(tempAnchorList);
        //     print(jObject.ToString());			
        //     return jObject;
        // }


        // /// <summary>
        // /// Parsing JSON data to have a AnchorInfo list
        // /// </summary>
        // /// <param name="mapMetadata"></param>
        // public void AnchorInfoListFromJSON(JToken mapMetadata)
        // {
        //     ClearAnchors();
        //
        //     if (mapMetadata is JObject && mapMetadata[Const.ANCHOR_DATA_JSON_ROOT] is JObject)
        //     {
        //         AnchorInfoList anchorList = mapMetadata[Const.ANCHOR_DATA_JSON_ROOT].ToObject<AnchorInfoList>();
        //         LoadAnchors(anchorList);                 
        //     }
        //     
        // }
        

        
        
        //-------------------------------- Others -------------------------------

        
        // public void StartEditingCurrentAnchor()
        // {
        //     this.currAnchorObj.GetComponent<AnchorController>().EditActivityNameForCurrentAnchorObj();
        // }
        
         
        // public void RotateAllAnchors() {
        //     foreach (var anchorController in sortedAnchorControllerArr) {
        //         anchorController.StopRotate(false);
        //     }
        // }
        

        public void HideActivities(string activeAnchorId = "") 
        {
            foreach (GameObject anchorObj in this.anchorObjList)
            {
                AnchorController anchorController = anchorObj.GetComponent<AnchorController>();
                if(anchorController.GetAnchorInfo().id != activeAnchorId) 
                {
                    anchorController.HideActivities();
                }
            }
        }

        // public Guid GetKeyOfEngagedAnchor()
        // {
        //      
        // }

        public void CorrectAllAnchorHeight(float groundHeight)
        {
            foreach (GameObject anchorObj in this.anchorObjList)
            {
                anchorObj.transform.position = new Vector3(anchorObj.transform.position.x,
                    groundHeight, anchorObj.transform.position.z);
            }
            MessageManager.Instance.DebugMessage($"Ground correction done for all anchors! Anchor height = {groundHeight}");
        }


        public bool IsAnyAnchorFlying()
        {
            foreach (GameObject anchorObj in anchorObjList)
            {
                AnchorController controller = anchorObj.GetComponent<AnchorController>();
                if (controller.flyingLock)
                    return true;
            }

            return false;
        }

    }
    
}

