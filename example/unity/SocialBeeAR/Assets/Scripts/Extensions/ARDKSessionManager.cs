#if ARGPS_USE_ARDK
using System;
using SocialBeeARDK;
using UnityEngine.XR.ARFoundation;

namespace ARLocation.Session
{
    public class ARDKSessionManager : IARSessionManager
    {
        private Action onAfterReset;
        private string sessionInfoString;
        private bool trackingStarted;
        private Action trackingStartedCallback;
        private Action trackingRestoredCallback;
        private Action trackingLostCallback;
        
        public bool DebugMode { get; set; }

        public ARDKSessionManager()
        {
            sessionInfoString = $"OnVuforiaStarted";
            ARHelper.Instance.OnTrackingResumed += OnSessionTrackingResumed;
            ARHelper.Instance.OnTrackingStarted += OnSessionTrackingStarted;
            ARHelper.Instance.OnTrackingLost += OnSessionTrackingLost;
        }

        private void OnSessionTrackingLost()
        {
            if (!trackingStarted) return;
            trackingStarted = false;
            trackingLostCallback?.Invoke();
        }

        private void OnSessionTrackingStarted(ARSessionState obj)
        {
            if (trackingStarted) return;
            trackingStarted = true;
            trackingStartedCallback?.Invoke();
        }

        void OnSessionTrackingResumed()
        {
            if (onAfterReset != null)
            {
                // Logger.LogFromMethod("ARDKSessionManager", "OnTrackingResumed", "Emitting 'OnAfterReset' event.", DebugMode);
                onAfterReset.Invoke();
                onAfterReset = null;
            }
        }

        public string GetSessionInfoString()
        {
            return sessionInfoString;
        }

        public string GetProviderString()
        {
            return "ARDK3.0";
        }
        
        public void Reset(Action callback)
        {
            onAfterReset += callback;
        }

        public void OnARTrackingStarted(Action callback)
        {
            if (trackingStarted)
            {
                callback?.Invoke();
                return;
            }

            trackingStartedCallback += callback;
        }

        public void OnARTrackingRestored(Action callback)
        {
            trackingRestoredCallback += callback;
        }

        public void OnARTrackingLost(Action callback)
        {
            trackingLostCallback += callback;
        }
    }
}
#endif