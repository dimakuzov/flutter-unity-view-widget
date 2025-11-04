// using Niantic.ARDK.AR;
// using Niantic.ARDK.AR.ARSessionEventArgs;
// using UnityEngine;
//
// namespace SocialBeeARDK
// {
//     public class PlaneManager
//     {
//         /// The object we will place to represent the cursor!
//         public GameObject CursorObject;
//         
//         // Reference to the active AR session.
//         IARSession _session;
//         
//         
//         void Start()
//         {
//             ARSessionFactory.SessionInitialized += OnAnyARSessionDidInitialize;
//         }
//
//         void OnDestroy()
//         {
//             ARSessionFactory.SessionInitialized -= OnAnyARSessionDidInitialize;
//             _session = null;
//             ARHelper.Instance.DeinitializeARSession();
//         }
//      
//         void OnAnyARSessionDidInitialize(AnyARSessionInitializedArgs args)
//         {
//             _session = args.Session;
//             _session.Deinitialized += OnSessionDeinitialized;
//         }
//         
//         void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
//         {
//             // destroy spawned objects here.
//         }
//     }
// }