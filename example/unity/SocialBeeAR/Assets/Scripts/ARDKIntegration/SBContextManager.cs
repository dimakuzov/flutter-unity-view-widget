// using SocialBeeAR;
//
// namespace SocialBeeARDK
// {
//     /// <summary>
//     /// This class is for storing the state info synchronized from the native socialbee app.
//     /// All other class in AR get/set state info through this class
//     /// </summary>
//     public class SBContextManager : BaseSingletonClass<SBContextManager>
//     {
//         public SBContext context;
//         /// <summary>
//         /// The last known location of the user.
//         /// </summary>
//         public Location lastKnownLocation;
//         
//         public override void Awake()
//         {
//             base.Awake();
//             LocationProxy.StartUpdatingLocation(name);
//         }
//     }
// }