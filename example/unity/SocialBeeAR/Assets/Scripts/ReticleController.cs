using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace SocialBeeAR
{

    /// <summary>
    /// This is modified from 'ReticleController' in 'HellowWorld' sample.
    /// </summary>
    public class ReticleController : BaseSingletonClass<ReticleController>
    {
        [SerializeField] GameObject reticle;
        [SerializeField] GameObject onBoardingReticle;

        static List<ARRaycastHit> hits = new List<ARRaycastHit>();
        public ARRaycastManager raycastManager;
        
        private IEnumerator continuousHitTest;
        private IEnumerator continuousHitOnBoard;
        private ARPlane currentTouchedPlane;
        
        private bool isReticleStarted;
        public bool IsReticleStarted
        {
            get => isReticleStarted;
        }
        
        private Vector3 prevPlaneNormal = Vector3.zero;

        
        void Start()
        {
            reticle.SetActive(false);
            continuousHitTest = ContinuousHittest();
            continuousHitOnBoard = ContinuousHitOnBoard();
        }
        

        // starts the cursor
        public void StartReticle()
        {
            PhonePoseManager.Instance.EnablePoseWarning(false);

            isReticleStarted = true;
            reticle.SetActive(false);
            StartCoroutine(continuousHitTest);
        }
        
        // starts OnBoarding's cursor
        public void StartOnBoardingReticle()
        {
            PhonePoseManager.Instance.EnablePoseWarning(false);
            isReticleStarted = true;
            onBoardingReticle.SetActive(false);
            StartCoroutine(continuousHitOnBoard);
        }

        public void StopReticle()
        {
            if (isReticleStarted)
            {
                StopCoroutine(continuousHitTest);
                reticle.SetActive(false);
            }
            isReticleStarted = false;
        }

        public void StopOnBoardReticle() {
            if (isReticleStarted)
            {
                StopCoroutine(continuousHitOnBoard);
                onBoardingReticle.SetActive(false);
            }
            isReticleStarted = false;
        }


        private IEnumerator ContinuousHittest()
        {
            Vector3 planeNormalRounded = Vector3.zero;
            
            while (true)
            {
                //getting screen point
                var screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

                //world Hit Test
                if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinBounds))
                {
                    //raycast hits are sorted by distance, so get the closest hit.
                    ARRaycastHit firstHit = hits[0];
                    Pose hitPose = firstHit.pose;
                    
                    //1. set reticle position to the hit position
                    reticle.transform.position = hitPose.position;
                    reticle.SetActive(true);
                    
                    //2. find the plane, for updating reticle rotation
                    currentTouchedPlane = PlaneManager.Instance.FindTrackable(firstHit.trackableId);
                    planeNormalRounded = Utilities.RoundVector(currentTouchedPlane.normal, 1);

                    if (planeNormalRounded != prevPlaneNormal) //on plane normal changed, update rotation
                    {
                        AttachReticleToPlane(reticle, currentTouchedPlane.normal);
                        prevPlaneNormal = planeNormalRounded;
                    }
                }
                else
                {
                    reticle.SetActive(false);
                }

                // go to next frame
                yield return null;
            }
        }
        
        private IEnumerator ContinuousHitOnBoard()
        {
            Vector3 planeNormalRounded = Vector3.zero;
            
            while (true)
            {
                //getting screen point
                var screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);

                //world Hit Test
                if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinBounds))
                {
                    //raycast hits are sorted by distance, so get the closest hit.
                    ARRaycastHit firstHit = hits[0];
                    Pose hitPose = firstHit.pose;
                    
                    //1. set reticle position to the hit position
                    onBoardingReticle.transform.position = hitPose.position;
                    onBoardingReticle.SetActive(true);
                    
                    //2. find the plane, for updating reticle rotation
                    currentTouchedPlane = PlaneManager.Instance.FindTrackable(firstHit.trackableId);
                    planeNormalRounded = Utilities.RoundVector(currentTouchedPlane.normal, 1);

                    if (planeNormalRounded != prevPlaneNormal) //on plane normal changed, update rotation
                    {
                        AttachReticleToPlane(onBoardingReticle, currentTouchedPlane.normal);
                        prevPlaneNormal = planeNormalRounded;
                    }
                }
                else
                {
                    onBoardingReticle.SetActive(false);
                }

                // go to next frame
                yield return null;
            }
        }


        private void AttachReticleToPlane(GameObject retigleObj, Vector3 planeNormal)
        {
            if (transform.forward != planeNormal)
            {
                //rotate anchor body and bottom plate
                Quaternion rotation = Quaternion.identity;
                rotation.SetLookRotation(planeNormal, Vector3.up);

                retigleObj.transform.rotation = rotation;
            }
        }
        
        
        public void OnReticleTapped()
        {
            print(string.Format("Reticle tapped, about to spawn anchor object"));
            StopReticle();

            if (SBContextManager.Instance.context.isEditing && SBContextManager.Instance.context.isPlanning)
            {
                //place the anchor object in the reticle position
                InteractionManager.Instance.OnSelectedLocationForEditPlanning(reticle.transform.position);
            }
            else
            {
                //place the anchor object in its own position
                //AnchorManager.Instance.SpawnAnchorObj(reticle.transform.position, currentTouchedPlane);
                AnchorManager.Instance.PlaceAnchor(reticle.transform.position);
            }
            
            //update UI mode
            ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.ReticleTapped);
            ActivityUIFacade.Instance.ShowAnchorMoveAndLockGuidance();

            //prepare anchor completion effect
            AnchorConfigCompletionEffectController.Instance.PrepareEffect();
            
        }
        
   
    }
    
    
}