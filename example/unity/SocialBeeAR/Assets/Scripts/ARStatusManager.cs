using System;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace SocialBeeAR
{
    
    public class ARStatusManager : BaseSingletonClass<ARStatusManager>
    {
        //the last time checking tracking lost
        // private DateTime lastCheckTime;
        
        //delegation
        // public Action<string, string> OnTrackingLostAlert;
        // public Action<NotTrackingReason> OnTrackingLost;
        // public Action OnTrackingRecovered;
        // private bool isTrackingLost;
        
        
        private void OnEnable()
        {
            ARSession.stateChanged += OnStateChanged;
        }
        
        
        private void OnDisable()
        {
            ARSession.stateChanged -= OnStateChanged;
        }

        
        private void OnStateChanged(ARSessionStateChangedEventArgs args)
        {
            //print debug info
            UpdateDebugInfo(args);
            
            //handle delegation
            // if (args.state != ARSessionState.SessionTracking)
            // {
            //     isTrackingLost = true;
            //     print(
            //         string.Format("AR tracking lost, reason = \'{0}\'", 
            //             ARSession.notTrackingReason));
            //     OnTrackingLost(ARSession.notTrackingReason); //callback for tracking lost
            //     
            //     if (ARSession.notTrackingReason == NotTrackingReason.InsufficientFeatures ||
            //         ARSession.notTrackingReason == NotTrackingReason.InsufficientLight ||
            //         ARSession.notTrackingReason == NotTrackingReason.ExcessiveMotion)
            //     {
            //         DateTime thisCheckTime = DateTime.UtcNow;
            //         if (DateTime.Compare(thisCheckTime, lastCheckTime) > 0) //if it's a new 'lost tracking time'
            //         {
            //             OnTrackingLostAlert(
            //                 Utilities.ARTrackingLostReasonToString(ARSession.notTrackingReason), 
            //                     Utilities.ARTrackingLostReasonToDescString(ARSession.notTrackingReason)); //callback for tracking lost alert
            //         }
            //     }
            //
            //     // if (ARSession.notTrackingReason == NotTrackingReason.Initializing) {
            //     //     RecordManager.Instance.RestoreAR();
            //     // }
            // }
            //
            // if (args.state == ARSessionState.SessionTracking && isTrackingLost)
            // {
            //     isTrackingLost = false;
            //     OnTrackingRecovered(); //callback for tracking recovery
            // }
        }


        private void UpdateDebugInfo(ARSessionStateChangedEventArgs args)
        {
            MessageManager.Instance.UpdateTrackingState(
                Utilities.ARTrackingStateToString(ARSession.state));
            
            MessageManager.Instance.UpdateNotTrackingReason(
                Utilities.ARTrackingLostReasonToString(ARSession.notTrackingReason));
        }

    }
}

