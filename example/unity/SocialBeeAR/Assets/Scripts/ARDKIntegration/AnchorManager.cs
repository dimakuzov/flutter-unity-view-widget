// using System;
// using System.Collections.Generic;
// using Niantic.Lightship.AR.Subsystems.PersistentAnchor;
// using Niantic.ARDK.AR.ARSessionEventArgs;
// using Niantic.ARDKExamples.Common.Helpers;
// using SocialBeeAR;
// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
//
// namespace SocialBeeARDK
// {
//     public class AnchorManager : BaseSingletonClass<AnchorManager>
//     {
//         [SerializeField] ARRaycastManager raycastManager;
//         [SerializeField] private Camera mainCam;
//
//         [SerializeField] private GameObject arSpace;
//
//         //Anchor prefab
//         public GameObject anchorObjPrefab;
//
//         //Anchor object list
//         private Dictionary<Guid, IARAnchor> _addedAnchors = new();
//         private List<GameObject> _anchorObjList = new();
//         private Dictionary<Guid, GameObject> _placedObjects = new();
//         
//         void Start()
//         {
//             ARHelper.Instance.Session.AnchorsAdded += OnAnchorsAddedToSession;
//             ARHelper.Instance.Session.AnchorsRemoved += OnAnchorsRemovedFromSession;
//         }
//         
//         private void OnAnchorsRemovedFromSession(AnchorsArgs args)
//         {
//             // throw new NotImplementedException();
//         }
//
//         private void OnAnchorsAddedToSession(AnchorsArgs args)
//         {
//             foreach (var anchor in args.Anchors)
//             {
//                 if (!_addedAnchors.ContainsKey(anchor.Identifier))
//                 {
//                     // Plane and image detection are both disabled in this scene, so the only anchors getting
//                     // surfaced through this callback are the anchors added in HitTestToPlaceAnchor.
//                     Debug.LogWarningFormat
//                     (
//                         "Found anchor (id: {0}) not added by this class. This should not happen.",
//                         anchor.Identifier
//                     );
//
//                     continue;
//                 }
//
//                 PlaceAnchorObject(anchor);
//             }
//         }
//         
//         public void SpawnAnchorObj(Vector3 touchPosition)
//         {
//             if (SBContextManager.Instance.context!=null && SBContextManager.Instance.context.plannedLocation != null)
//             {
//                 print($"The thumbail for the Google Map Location panel: {SBContextManager.Instance.context.plannedLocation.Thumbnail}");                
//             }            
//
//             // 0.0 Add anchor to the AR session.
//             var anchor = ARHelper.Instance.Session.AddAnchor(Matrix4x4.TRS(touchPosition, Quaternion.identity, Vector3.one));
//             _addedAnchors.Add(anchor.Identifier, anchor);
//         }
//
//         private void PlaceAnchorObject(IARAnchor anchor)
//         {
//             //1. Instantiate new anchor prefab and set transform.
//             //GameObject anchorObj = Instantiate(anchorObjPrefab);
//             
//             // Create the cube object and add a component that will keep it attached to the new anchor.
//             var cube =
//                 Instantiate
//                 (
//                     anchorObjPrefab,
//                     new Vector3(0, 0, 0),
//                     Quaternion.identity
//                 );
//
//             var attachment = cube.AddComponent<ARAnchorAttachment>();
//             attachment.AttachedAnchor = anchor;
//             var cubeYOffset = anchorObjPrefab.transform.localScale.y / 2;
//             attachment.Offset = Matrix4x4.Translate(new Vector3(0, cubeYOffset, 0));
//  
//             // Keep track of the anchor objects
//             _placedObjects.Add(anchor.Identifier, cube);
//             
//             //2. creating an initial anchorInfo object
//             // var actInfoList = new List<IActivityInfo>();
//             // string anchorId = Utilities.GenerateAnchorId();
//             // Pose anchorPose = new Pose(touchPosition, Quaternion.identity);
//             AnchorInfo initialAnchorInfo = new AnchorInfo //prepare an empty anchorInfo
//             {
//                 id = anchor.Identifier.ToString(),
//                 postInfo = new PostActivityInfo(),
//                 // pose = anchorPose,
//                 activityInfoList = new List<IActivityInfo>()
//             };
//             if (SBContextManager.Instance.context is { isPlanning: true })
//                 initialAnchorInfo.postInfo.MapLocation = SBContextManager.Instance.context.plannedLocation;
//
//             //3. init anchor
//             AnchorController anchorController = anchorObj.GetComponent<AnchorController>();
//             var index = _placedObjects.Count; // anchorObjList.Count;
//             anchorController.Born(index, initialAnchorInfo);
//
//             //4. add to the anchor list
//             anchorObjList.Add(anchorObj);
//             print($"New anchor spawned, index: '{index}'");
//
//             //5. register this anchor as the focused anchor
//             RegisterEngagedAnchor(index);
//             
//             //6. set target for Off-Screen indicator
//             OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform);
//             OffScreenIndicatorManager.Instance.ShowArrow();
//         } 
//     }
// }