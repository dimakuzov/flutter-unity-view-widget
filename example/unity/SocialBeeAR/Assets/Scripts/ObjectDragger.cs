using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SocialBeeAR
{

    /// <summary>
    /// Enable users to drag an object on multiple detected planes.
    /// </summary>
    public class ObjectDragger : MonoBehaviour
    {
        [SerializeField] private float minDistance = 0.1f;
        [SerializeField] private float maxDistance = 10.0f;

        [SerializeField] private GameObject targetObj;

        private float distance = 0f;

        private ARRaycastManager raycastManager;
        static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        private AnchorController anchorController;
        private Vector3 prevPlaneNormal = Vector3.zero;
        
        
        private void Start()
        {
            GameObject arSessionOriginObj = Camera.main.transform.parent.gameObject;
            this.raycastManager = arSessionOriginObj.GetComponent<ARRaycastManager>();
            if(this.raycastManager == null)
            {
                print("Cannot find ARRaycastManager!");
            }

            anchorController = GetComponentInParent<AnchorController>();
        }


        void Update()
        {
            if (Input.touchCount > 0)
            {
                switch (Input.GetTouch(0).phase)
                {
                    case TouchPhase.Began:
                        OnTapped(Input.GetTouch(0).position);
                        break;

                    case TouchPhase.Moved:
                        OnDragged(Input.GetTouch(0).position);
                        break;

                    case TouchPhase.Ended:
                        OnDragingEnded();
                        break;

                    default:
                        // nothing
                        break;
                }
            }
        }


        private Vector3 lastHitPos = Vector3.zero;
        void OnTapped(Vector2 touchPosition)
        {
            ////disable spawning
            //ContentManager.Instance.EnableCreatingContent(false);

            distance = 0f;
            lastHitPos = Vector3.zero;
            Camera mainCamera = Camera.main;
            if (!mainCamera) return;

            //send a raycast to hit the object
            Ray ray = mainCamera.ScreenPointToRay(touchPosition);
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, maxDistance, 1 << gameObject.layer)
                && raycastHit.collider.gameObject == gameObject
                && raycastHit.distance >= minDistance)
            {
                distance = raycastHit.distance;

                //send another raycast to hit the environment plane
                if (this.raycastManager != null)
                {
                    if (this.raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                    {
                        var hitPose = hits[0].pose;
                        lastHitPos = hitPose.position;
                    }
                }
            }
        }
        

        private void OnDragged(Vector2 touchPosition)
        {
            Camera mainCamera = Camera.main;
            if (!mainCamera) return;
            if (lastHitPos == Vector3.zero) return;
            if (distance <= 0f) return;
            Vector3 planeNormalRounded = Vector3.zero;

            if (!anchorController)
                anchorController = GetComponentInParent<AnchorController>();

            if (this.raycastManager != null)
            {
                if (this.raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    ARRaycastHit firstHit = hits[0];
 
                    var hitPose = firstHit.pose;
                    Vector3 moving = hitPose.position - lastHitPos;
                    Vector3 newAnchorPos = targetObj.transform.position + moving;

                    //set anchor rotation and bottom plate pose
                    ARPlane plane = PlaneManager.Instance.FindTrackable(firstHit.trackableId);
                    planeNormalRounded = Utilities.RoundVector(plane.normal, 1);
                    if (planeNormalRounded != prevPlaneNormal) //on plane normal changed
                    {
                        //print(string.Format("Anchor's Plane changed! normal=\'{0}\'", planeNormalRounded));
                        UpdateAnchorRotation(plane);
                        prevPlaneNormal = planeNormalRounded;
                    }
                    
                    //set anchor position
                    UpdateAnchorPosition(newAnchorPos, plane);
                    
                    lastHitPos = hitPose.position;
                }
            }
        }
        

        public void UpdateAnchorRotation(ARPlane plane)
        {
            
            print("exiting UpdateAnchorRotation...");
            return;
            // #lightship-REMOVE_TEMPORARILY
            if (!anchorController)
                anchorController = GetComponentInParent<AnchorController>();
            
            Vector3 planeNormalRounded = Utilities.RoundVector(plane.normal, 1);
            if (!IsPlaneHorizontal(planeNormalRounded)) //when user touches on vertical plane
            {
                //disable watching player
                anchorController.EnableWatchingPlayer(false);
                                
                //attach anchor body to the plane
                anchorController.AttachToPlane(plane.normal);
            }
            else //when user touches on horizontal plane
            {
                //set bottom plate rotation: stand on the ground
                anchorController.ResetBottomPlate();

                //enable watching player
                anchorController.EnableWatchingPlayer(true);
            }
        }


        private bool IsPlaneHorizontal(Vector3 roundedNormal)
        {
            if (roundedNormal.y >= 0.8 && roundedNormal.y <= 1
                && roundedNormal.x >= -0.3 && roundedNormal.x <= 0.3
                    && roundedNormal.z >= -0.3 && roundedNormal.z <= 0.3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        


        public void UpdateAnchorPosition(Vector3 anchorNewPos, ARPlane plane)
        {
            Vector3 planeNormalRounded = Utilities.RoundVector(plane.normal, 1);
            if (planeNormalRounded == Vector3.up) //when user touches on horizontal plane
            {
                //make sure anchor object is standing on the ground instead of below the ground
                float groundY = plane.transform.position.y;
                float adjustY = groundY - anchorController.lockerPlate.transform.position.y;
                anchorNewPos = new Vector3(anchorNewPos.x, anchorNewPos.y + adjustY, anchorNewPos.z);    
            }

            targetObj.transform.position = anchorNewPos;
        }
        

        private void OnDragingEnded()
        {
            //do nothing
        }
        
    }
}


