// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.UI;
// using Newtonsoft.Json.Linq;
//
// namespace SocialBeeAR
// {
//
//     /// <summary>
//     /// This class is for showing geolocation info for an map.
//     /// It is from PlaceNote sample
//     /// </summary>
//     public class SBMapInfoElement : MonoBehaviour
//     {
//         [SerializeField] Text mapIdText;
//         [SerializeField] Text locationText;
//         [SerializeField] Toggle toggle;
//
//         public void Initialize(LibPlacenote.MapInfo mapInfo, ToggleGroup toggleGroup,
//                                RectTransform listParent, UnityAction<bool> onToggleChanged)
//         {
//             mapIdText.text = mapInfo.placeId;
//             if (mapInfo.metadata.name != null && mapInfo.metadata.name.Length > 0)
//             {
//                 mapIdText.text = mapInfo.metadata.name;
//             }
//             toggle.group = toggleGroup;
//             gameObject.transform.SetParent(listParent);
//             toggle.onValueChanged.AddListener(onToggleChanged);
//
//             if (Input.location.status != LocationServiceStatus.Running)
//             {
//                 locationText.text = "Distance Unknown - No user location";
//             }
//             else if (mapInfo.metadata.location != null)
//             {
//                 
//                 var distance = Calc(Input.location.lastData.latitude, Input.location.lastData.longitude,
//                     mapInfo.metadata.location.latitude,
//                     mapInfo.metadata.location.longitude);
//                 locationText.text = "Distance: " + distance.ToString("F3") + "km";
//             }
//             else
//             {
//                 locationText.text = "Distance Unknown - Map does not have location";
//             }
//         }
//
//         public static double Calc(float lat1, float lon1, float lat2, float lon2)
//         {
//             // Very simple spherical calculation, should probably use a better
//             // projection for production
//             var R = 6378.137; // Radius of earth in KM
//             var dLat = lat2 * Mathf.PI / 180 - lat1 * Mathf.PI / 180;
//             var dLon = lon2 * Mathf.PI / 180 - lon1 * Mathf.PI / 180;
//             float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
//                 Mathf.Cos(lat1 * Mathf.PI / 180) * Mathf.Cos(lat2 * Mathf.PI / 180) *
//                 Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
//             var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
//             var d = R * c;
//             return d;
//         }
//     }
//
// }