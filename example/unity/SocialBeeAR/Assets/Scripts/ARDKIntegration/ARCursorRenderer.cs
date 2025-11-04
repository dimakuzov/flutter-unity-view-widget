using System.Collections.Generic;
using Niantic.Lightship.AR;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SocialBeeARDK
{ 
    ///! Helper script that spawns a cursor on a plane if it finds one
    /// <summary>
    /// A sample class that can be added to a scene to demonstrate basic plane finding and hit
    ///   testing usage. On each updated frame, a hit test will be applied from the middle of the
    ///   screen and spawn a cursor if it finds a plane.
    /// </summary>
    /// <remarks>
    /// Original code taken from ARDK sample.
    /// </remarks>
    public class ARCursorRenderer : BaseSingletonClass<ARCursorRenderer>
    {
        /// The camera used to render the scene. Used to get the center of the screen.
        [SerializeField] private Camera _camera;

        /// The object we will place to represent the cursor!
        public GameObject CursorObject;
        /// The object we will place to represent the cursor during On-Boarding.
        public GameObject OnboardingCursorObject;         
        /// <summary>
        /// The active cursor to use.
        /// </summary>
        private GameObject _cursorToUse;
        
        /// A reference to the spawned cursor in the center of the screen.
        private GameObject _spawnedCursorObject;
        /// <summary>
        /// Same position as <see cref="_spawnedCursorObject"/>.
        /// </summary>
        private Pose _cursorLocalPose;

        private ARSession _session;
        private ARRaycastManager _arRaycastManager;
        private bool _isCursorStarted;
        Vector3 _prevPlaneNormal = Vector3.zero;
        [SerializeField] private float _smoothSpeed = 10f;

        private void Start() {
            _session = FindFirstObjectByType<ARSession>();
            _arRaycastManager = FindObjectOfType<ARRaycastManager>();
            
            CursorObject.SetActive(false);
            OnboardingCursorObject.SetActive(false);
        }

        private void Update()
        {
            // // if there is a touch call our function
            // if (PlatformAgnosticInput.touchCount <= 0) return;
            //
            // // print($"Update #cursortapped > touchCount={PlatformAgnosticInput.touchCount}");
            // var touch = PlatformAgnosticInput.GetTouch(0);
            // if (touch.phase == TouchPhase.Began)
            // {
            //     TouchBegan(touch);
            // }
            
            if (!_isCursorStarted)
            {
                // print("OnFrameUpdated > isCursorStarted=False");
                return;
            }
            
            if (_camera == null  || _arRaycastManager == null)
                return;
            
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
            if (_arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                ARRaycastHit hit = hits[0];
            
                if (_spawnedCursorObject == null)
                {
                    _spawnedCursorObject = Instantiate(_cursorToUse);
                    _spawnedCursorObject.SetActive(true);
                    _spawnedCursorObject.name = "AR Cursor";
                }
            
                if (!_spawnedCursorObject.activeSelf)
                {
                    _spawnedCursorObject.SetActive(true);
                }
            
                _cursorLocalPose = hit.pose;
            
                // Плавное движение к целевой позиции
                Vector3 targetPosition = hit.pose.position;
                _spawnedCursorObject.transform.position = Vector3.Lerp(
                    _spawnedCursorObject.transform.position,
                    targetPosition,
                    Time.deltaTime * _smoothSpeed
                );
            
                RotateCursorTowardsCamera();
            }
        }
        
        private void RotateCursorTowardsCamera()
        {
            Vector3 lookAtPosition = new Vector3(
                _camera.transform.position.x,
                _spawnedCursorObject.transform.position.y,
                _camera.transform.position.z
            );
        
            Vector3 direction = lookAtPosition - _spawnedCursorObject.transform.position;
        
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                _spawnedCursorObject.transform.rotation = Quaternion.Lerp(
                    _spawnedCursorObject.transform.rotation,
                    targetRotation,
                    Time.deltaTime * _smoothSpeed
                );
            }
        }

        private void OnDestroy()
        {
            DestroySpawnedCursor();
        }

        private void DestroySpawnedCursor()
        {
            if (_spawnedCursorObject == null)
              return;

            Destroy(_spawnedCursorObject);
            _spawnedCursorObject = null;
        }


        // void RotateCursorParallelToTheGround(IARHitTestResult plane)
        // {
        //     if (plane == null || plane.Anchor == null) return;
        //     if (plane.Anchor.AnchorType != AnchorType.Plane) return;
        //
        //     
        //     var anchorPosition = plane.Anchor.Transform.ToPosition();
        //     print($"plane position={plane.WorldTransform.ToPosition()} | anchorPosition={anchorPosition} | transform.forward={transform.forward} | LocalTransform={plane.LocalTransform.ToPosition()}");
        //     var planeNormalRounded = Utilities.RoundVector(anchorPosition, 1);
        //     print($"planeNormalRounded={planeNormalRounded} | _prevPlaneNormal={_prevPlaneNormal}");
        //     if (planeNormalRounded == _prevPlaneNormal) return;
        //     
        //     if (transform.forward != anchorPosition)
        //     {
        //         //rotate anchor body and bottom plate
        //         Quaternion rotation = Quaternion.identity;
        //         rotation.SetLookRotation(anchorPosition, Vector3.up);
        //         print($"rotation before={_spawnedCursorObject.transform.rotation} | after={rotation}");
        //         _spawnedCursorObject.transform.rotation = rotation;
        //     }
        //     _prevPlaneNormal = planeNormalRounded;
        // }
         
        public void OnCursorTapped()
        {
            StopCursor();

            print($"1 - cursor position: {_spawnedCursorObject.transform.position} | isEditing={SBContextManager.Instance.context.isEditing} " +
                  $"| isPlanning={SBContextManager.Instance.context.isPlanning} | isCreatingGPSOnlyAnchors={SBContextManager.Instance.context.isCreatingGPSOnlyAnchors} #anchorposition");
            if (SBContextManager.Instance.context.isEditing && SBContextManager.Instance.context.isPlanning)
            {
                //place the anchor object in the reticle position
                InteractionManager.Instance.OnSelectedLocationForEditPlanning(_spawnedCursorObject.transform.position);
            }
            else if (SBContextManager.Instance.context.isCreatingGPSOnlyAnchors)
            {
                //place the anchor object in its own position
                AnchorManager.Instance.PlaceAnchor(_spawnedCursorObject.transform.position);
            }
            else // marker-based with Lightship VPS
            {
                WayspotAnchorManager.Instance.PlaceAnchor(_cursorLocalPose);
                //WayspotAnchorManagerV2.Instance.PlaceAnchor(_cursorLocalPose);
            }
 
            //update UI mode
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ReticleTapped);
            ActivityUIFacade.Instance.ShowAnchorMoveAndLockGuidance();
            //prepare anchor completion effect
            AnchorConfigCompletionEffectController.Instance.PrepareEffect();
        }

        // starts the cursor
        public void StartCursor()
        {
            // PhonePoseManager.Instance.EnablePoseWarning(false);
            _isCursorStarted = true;
            CursorObject.SetActive(false);
            _cursorToUse = CursorObject;
        }
        
        // starts the cursor
        public void StartOnboardingCursor()
        {   
            //PhonePoseManager.Instance.EnablePoseWarning(false);
            _isCursorStarted = true;
            OnboardingCursorObject.SetActive(false);
            _cursorToUse = OnboardingCursorObject;
        }
        
        public void StopCursor()
        {
            if (_isCursorStarted)
            {
                CursorObject.SetActive(false);
            }
            _isCursorStarted = false;
            Destroy(_spawnedCursorObject); 
        }

        public void StopOnboardingCursor() {
            if (_isCursorStarted)
            {
                OnboardingCursorObject.SetActive(false);
            }
            _isCursorStarted = false;
        }
    }
}