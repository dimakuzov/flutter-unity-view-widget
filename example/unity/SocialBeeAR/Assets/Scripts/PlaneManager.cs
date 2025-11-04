using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace SocialBeeAR
{

    /// <summary>
    /// This class is the manager for all virtual objects (except anchors). This can be extended with different sub-managers for different type of objects.
    /// </summary>
    public class PlaneManager : BaseSingletonClass<PlaneManager>
    {

        [SerializeField] ARPlaneManager arPlaneManager;
        [SerializeField] private GameObject hexagonVisualizationPrefab;
        [SerializeField] private GameObject occlusionVisualizationPrefab;
        
        private PlaneVisualizationType planeVisType;
        private PlaneDetectionMode planeDetectionMode;

        private PlaneVisualizationType lastVisualizationType;
        private PlaneDetectionMode lastDetectionMode;

        
        public enum PlaneVisualizationType
        {
            None, Hexagon, Occlusion
        }


        public enum PlaneDetectionMode
        {
            Undefined, Horizontal, Vertical, All, None
        }
         
        //--------------------------- set detection mode -----------------------
        

        public void SetPlaneDetectionMode(PlaneDetectionMode mode)
        {
            if (planeDetectionMode == mode)
                return;

            planeDetectionMode = mode;

            SetPlaneDetectionMode();
        }
        
        
        private void SetPlaneDetectionMode()
        {
            switch (planeDetectionMode)
            {
                case PlaneDetectionMode.Horizontal:
                    arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
                    break;
                
                case PlaneDetectionMode.Vertical:
                    arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
                    break;
                
                case PlaneDetectionMode.All:
                    arPlaneManager.detectionMode = (UnityEngine.XR.ARSubsystems.PlaneDetectionMode)(-1);
                    break;
                
                case PlaneDetectionMode.Undefined:
                    arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
                    break;
                
                case PlaneDetectionMode.None:
                    arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.None;
                    break;
            }
            
            //'reboot' the plane detection
            arPlaneManager.enabled = false;
            arPlaneManager.enabled = true;
        }
        

        //--------------------------- set visualization mode -----------------------
        
        
        public void SetPlaneVisualizationType(PlaneVisualizationType type)
        {
            if (type == planeVisType)
                return;
            
            planeVisType = type;

            SetVisualization();
        }
        

        private void SetVisualization()
        {
            switch (planeVisType)
            {
                case PlaneVisualizationType.Hexagon:
                    if (hexagonVisualizationPrefab)
                        arPlaneManager.planePrefab = hexagonVisualizationPrefab;
                    break;
                
                case PlaneVisualizationType.Occlusion:
                    if (occlusionVisualizationPrefab)
                        arPlaneManager.planePrefab = occlusionVisualizationPrefab;
                    break;
                
                case PlaneVisualizationType.None:
                    arPlaneManager.planePrefab = null;
                    break;
            }
        }
        
        
        //--------------------------- set visibility for planes -----------------------
        
        
        public void SetARPlanesVisible(bool visible)
        {
            if (!arPlaneManager)
                return;
            
            // if(!visible)
            // {
            //     lastVisualizationType = planeVisType;
            //     lastDetectionMode = planeDetectionMode;
            //     arPlaneManager.detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.None;
            // }
 
            //control the visibility of all the created planes
            foreach (ARPlane plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
        
            //enable/disable plane manager
            //arPlaneManager.enabled = visible;
            
            // if(visible)
            // {
            //     SetPlaneVisualizationType(lastVisualizationType);
            //     SetPlaneDetectionMode(lastDetectionMode);
            // }
        }
        
        
        public void EnableARPlaneManager(bool enabled)
        {
            arPlaneManager.enabled = enabled;
        }


        public ARPlane FindTrackable(TrackableId id)
        {
            foreach (ARPlane plane in arPlaneManager.trackables)
            {
                if(plane.trackableId == id)
                    return plane;
            }
            return null;
        }


        public Vector3 currGroundNormal { get; set; }

        
        /// <summary>
        /// Retrieve the ground normal from AR plane detection
        /// </summary>
        /// <returns></returns>
        public Vector3 GetGroundNormal()
        {
            MessageManager.Instance.DebugMessage($"Iterating \'{arPlaneManager.trackables.count}\' planes");
            Vector3 finalGoundNormal = Vector3.zero;

            if (arPlaneManager.trackables.count == 0)
            {
                MessageManager.Instance.DebugMessage("ERROR: no plane for checking normal!");
                return Vector3.up;
            }

            int i = 0;
            float minAngle = 180f;
            foreach (ARPlane plane in arPlaneManager.trackables)
            {
                if (!plane)
                    continue;

                float angle = Vector3.Angle(plane.normal, Vector3.up);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    finalGoundNormal = plane.normal;
                    MessageManager.Instance.DebugMessage($"Plane {i}: normal: [{plane.normal.x},{plane.normal.y},{plane.normal.z}], angle to up: '{angle}', closer to Vector3.up!");
                }
                else
                    MessageManager.Instance.DebugMessage($"Plane {i}: normal: [{plane.normal.x},{plane.normal.y},{plane.normal.z}], angle to up: '{angle}', NOT closer to Vector3.up!");

                i++;
            }

            if (finalGoundNormal == Vector3.zero)
            {
                MessageManager.Instance.DebugMessage("ERROR: Didn't find any ground normal!");
                return Vector3.up;
            }
            else
            {
                currGroundNormal = finalGoundNormal;
                return finalGoundNormal;
            }
        }


        public float GetGroundHeight()
        {
            //MessageManager.Instance.DebugMessage($"[GetGroundHeight]: Iterating \'{arPlaneManager.trackables.count}\' planes");
            Vector3 finalGoundNormal = Vector3.zero;

            if (arPlaneManager.trackables.count == 0)
            {
                //MessageManager.Instance.DebugMessage("[GetGroundHeight]: ERROR: no plane for checking normal!");
                return float.NegativeInfinity;
            }

            int i = 0;
            float lowest = Camera.main.transform.position.y;
            ARPlane lowestPlane = null;
            List<float> heightList = new List<float>();
            foreach (ARPlane plane in arPlaneManager.trackables)
            {
                if (!plane)
                    continue;
                
                if (plane.alignment != PlaneAlignment.Vertical)
                {
                    float planeHeight = plane.transform.position.y;
                    if (planeHeight < lowest)
                    {
                        lowest = planeHeight;
                        lowestPlane = plane;
                    }
                }
                i ++;
            }

            if (lowest < 0)
            {
                //MessageManager.Instance.DebugMessage($"[GetGroundHeight]: Found ground plane, height={lowest}");
                return lowest;
            }
            else
            {
                //MessageManager.Instance.DebugMessage($"[GetGroundHeight]: Didn't find ground plane");
                return float.NegativeInfinity;
            }
        }

    }

}
