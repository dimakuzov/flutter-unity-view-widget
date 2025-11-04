// using System;
// using System.Collections.Generic;
// using Niantic.ARDK.AR;
// using Niantic.ARDK.AR.ARSessionEventArgs;
// using Niantic.Lightship.AR.PersistentAnchors;
// using Niantic.ARDK.Extensions;
// using Niantic.ARDK.LocationService;
// using Niantic.ARDK.Utilities;
// using UnityEngine;
//
// namespace SocialBeeARDK
// {
//     public class WayspotAnchorManagerV2 : BaseSingletonClass<WayspotAnchorManagerV2>
//     {
//         private IARSession _arSession;
//         private WayspotAnchorService _wayspotAnchorService;
//         private readonly HashSet<WayspotAnchorTracker> _wayspotAnchorTrackers =
//             new HashSet<WayspotAnchorTracker>();
//         public GameObject AnchorObjectPrefabDEBUG;
//         
//         [Tooltip("Camera used to place the anchors via raycasting")]
//         [SerializeField]
//         private Camera _camera;
//         private void Awake()
//         {
//             print("Awake");
//         }
//
//         private void OnEnable()
//         {
//             print("OnEnable");
//             ARSessionFactory.SessionInitialized += HandleSessionInitialized;
//         }
//
//         private void HandleSessionInitialized(AnyARSessionInitializedArgs args)
//         {
//             print("HandleSessionInitialized");
//             _arSession = args.Session;
//             _arSession.Ran += HandleSessionRan;
//         }
//
//         private void HandleSessionRan(ARSessionRanArgs args)
//         {
//             print("HandleSessionRan");
//             _arSession.Ran -= HandleSessionRan;
//             _wayspotAnchorService = CreateWayspotAnchorService();
//             
//             _wayspotAnchorService.LocalizationStateUpdated += OnLocalizationStateUpdated; // for debugging only
//         }
//
//         /// <summary>
//         /// This is for debugging only and will not be used in production.
//         /// </summary>
//         /// <param name="args"></param>
//         /// <exception cref="NotImplementedException"></exception>
//         private void OnLocalizationStateUpdated(LocalizationStateUpdatedArgs args)
//         {
//             print($"Localization status: {args.State} | Reason:{args.FailureReason}");
//         }
//
//         public void PlaceAnchor(Matrix4x4 touchPosition)
//         {
//             if (_wayspotAnchorService == null) return;
//             print($"SpawnAnchorObject > touchPosition={touchPosition.ToString()} #touchposition");
//            
//             var anchors = _wayspotAnchorService.CreateWayspotAnchors(touchPosition);
//             if (anchors.Length == 0)
//             {
//                 print($"SpawnAnchorObject > failed creating wayspot anchors.");
//                 return;
//             }
//
//             //CreateAnchorGameObject(anchors[0], touchPosition.ToPosition(), touchPosition.ToRotation(), null);
//             TestCreateWayspotAnchorGameObject(anchors[0],  touchPosition.ToPosition(), touchPosition.ToRotation(), true);
//         }
//         
//         private GameObject TestCreateWayspotAnchorGameObject
//         (
//             IWayspotAnchor anchor,
//             Vector3 position,
//             Quaternion rotation,
//             bool startActive
//         )
//         {
//             var go = Instantiate(AnchorObjectPrefabDEBUG, position, rotation);
//             
//             var tracker = go.GetComponent<WayspotAnchorTracker>();
//             if (tracker == null)
//             {
//                 Debug.Log("Anchor prefab was missing WayspotAnchorTracker, so one will be added.");
//                 tracker = go.AddComponent<WayspotAnchorTracker>();
//             }
//
//             tracker.gameObject.SetActive(startActive);
//             tracker.AttachAnchor(anchor);
//             _wayspotAnchorTrackers.Add(tracker);
//
//             return go;
//         }
//
//         private WayspotAnchorService CreateWayspotAnchorService()
//         {
//             // print($"2 - WayspotAnchorService > wayspotAnchorsConfiguration is null? {wayspotAnchorsConfiguration==null}");
//             var locationService = LocationServiceFactory.Create(_arSession.RuntimeEnvironment);
//             // print($"3 - WayspotAnchorService > locationService is null? {locationService==null}");
//             locationService?.Start();
//             
//             // print($"1 - WayspotAnchorService");
//             var wayspotAnchorsConfiguration = WayspotAnchorsConfigurationFactory.Create();
//             wayspotAnchorsConfiguration.LocalizationTimeout = -1;
//
//             // print($"4 - WayspotAnchorService > LocationService.Status={VpsCoverageManager.Instance.LocationService.Status}");
//             var wayspotAnchorService = new WayspotAnchorService(
//                 _arSession, 
//                 locationService, //VpsCoverageManager.Instance.LocationService, 
//                 wayspotAnchorsConfiguration);
//             return wayspotAnchorService;
//         }
//        
//     }
// }