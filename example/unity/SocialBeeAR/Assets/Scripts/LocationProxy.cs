// using System.Runtime.InteropServices;
//
// public class LocationProxy  
// {
// #if UNITY_IOS || UNITY_ANDROID
//     [DllImport("__Internal")]
//     private static extern bool _startUpdatingLocation(string callback);
//     [DllImport("__Internal")]
//     private static extern void _stopUpdatingLocation();
// #endif
//
//     public static bool StartUpdatingLocation(string callback)
//     {
// #if !UNITY_EDITOR
//         return _startUpdatingLocation(callback);
// #else
//         return false;
// #endif
//     }
//
//     public static void StopUpdatingLocation()
//     {
// #if !UNITY_EDITOR
//         _stopUpdatingLocation();
// #endif
//     }
//
// }
