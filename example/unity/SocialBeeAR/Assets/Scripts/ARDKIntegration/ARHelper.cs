using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Niantic.Lightship.AR.Utilities;

namespace SocialBeeARDK
{
    public class ARHelper : BaseSingletonClass<ARHelper>

    {
        /// <summary>
        /// Returns true if the AR session has started initializing.
        /// True, doesn't necessarily mean that the AR session is already running.
        /// </summary>
        public bool HasARSessionInitialized;
        // Reference to the active AR session.
        public ARSession arSession;
        [SerializeField] private ARPlaneManager _arPlaneManager;
        public ARSessionState SessionState => ARSession.state;
        public Action<ARSessionState> OnTrackingStarted;
        public Action OnTrackingStopped;
        public Action OnTrackingLost;
        public Action OnTrackingResumed;
        public event Action<ARSessionState> OnSessionStateChanged;
        
        // Start is called before the first frame update
        void Start()
        {
            ARSession.stateChanged += OnARSessionStateChanged;
        }
        
        /// <summary>
        /// Set the current user as the logged-in user in ARDK.
        /// </summary>
        /// <param name="userId"></param>
        public void SetARUser(string userId)
        {
            // ArdkGlobalConfig.SetUserIdOnLogin(userId);
        }

        /// <summary>
        /// Logs out the current user from the AR session.
        /// </summary>
        /// <param name="userId"></param>
        public void ClearARUser(string userId)
        {
            // ArdkGlobalConfig.ClearUserIdOnLogout();
        } 
        
        #region AR session methods
        public void InitializeARSession()
        {
            // print($"InitializeARSession #arsession-init > HasARSessionInitialized={HasARSessionInitialized}");
            if (HasARSessionInitialized)
            {
                //print("AR Session is already initialized #arsession");
                return;
            }
            arSession.enabled = true;
            ResumePlaneDetection();
            HasARSessionInitialized = true;
            print($"InitializeARSession > HasARSessionInitialized = {HasARSessionInitialized} #arsession");

        }
        
        public void DeinitializeARSession()
        {
            //print("DeinitializeARSession > HasARSessionInitialized = false");
            HasARSessionInitialized = false;
            arSession.enabled = false;
            StopPlaneDetection();
            print($"DeinitializeARSession > HasARSessionInitialized = {HasARSessionInitialized} #arsession");

        }

        private void ResumePlaneDetection()
        {
            if (_arPlaneManager != null && !_arPlaneManager.enabled)
            {
                _arPlaneManager.enabled = true;
            }
            print("RunPlaneManager()");
        }

        public void StopPlaneDetection()
        {
            if (_arPlaneManager != null)
            {
                _arPlaneManager.enabled = false;
            }
            print("StopPlaneManage()");
        }
        
        /// <summary>
        /// Toggle visibility of all detected planes
        /// </summary>
        public void TogglePlaneVisibility(bool visible)
        {
            foreach (var plane in _arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
            
            Debug.Log($"Planes visibility: {visible}");
        }
        
        private void OnDestroy()
        {
            // Instance.DeinitializeARSession();
            //print("ARSession Deinitialize #arsession");
        }
        
        #endregion

        #region Session Event Handlers
        
        private ARSessionState _previousState = ARSessionState.None;
        
        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            Debug.Log($"AR Session state changed: {_previousState} -> {args.state}");
            
            OnSessionStateChanged?.Invoke(args.state);
            
            switch (args.state)
            {
                case ARSessionState.Ready:
                    Debug.Log("AR Session is ready");
                    break;
                    
                case ARSessionState.SessionInitializing:
                    Debug.Log("AR Session is initializing...");
                    break;
                    
                case ARSessionState.SessionTracking:
                    // Tracking started
                    if (_previousState != ARSessionState.SessionTracking)
                    {
                        Debug.Log("✓ AR Tracking started");
                        OnTrackingStarted?.Invoke(args.state);
                    }
                    break;
                    
                case ARSessionState.None:
                case ARSessionState.Unsupported:
                    // Tracking stopped
                    if (_previousState == ARSessionState.SessionTracking)
                    {
                        Debug.LogWarning("✗ AR Tracking stopped");
                        OnTrackingStopped?.Invoke();
                    }
                    break;
            }
            
            if (_previousState == ARSessionState.SessionTracking && 
                args.state != ARSessionState.SessionTracking)
            {
                Debug.LogWarning("⚠ AR Tracking lost");
                OnTrackingLost?.Invoke();
            }
            
            if (_previousState != ARSessionState.SessionTracking && 
                args.state == ARSessionState.SessionTracking)
            {
                Debug.Log("✓ AR Tracking resumed");
                OnTrackingResumed?.Invoke();
            }
            
            _previousState = args.state;
        }
        
        #endregion
    }
} 