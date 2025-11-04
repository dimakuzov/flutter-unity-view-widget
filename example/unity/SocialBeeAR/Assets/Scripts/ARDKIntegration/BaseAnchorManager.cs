using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SocialBeeARDK
{
    public interface IAnchorManager
    {
        public int AnchorCount { get; }
        public int CalledAnchorIndex { get; set; }
        public List<GameObject> GetAnchorObjectList();
        public bool isAlerted { get; set; }
        public bool IsReadyToSaveMap();
        public GameObject GetCurrentAnchorObject();
        public GameObject GetAnchorObjectById(string id);
        public void HideActivities(string activeAnchorId = "");
        public void LoadAnchors(AnchorInfoList anchorList);
        public AnchorInfo GetEngagedAnchorInfo();
        // public abstract Guid GetKeyOfEngagedAnchor();
        public void RegisterClosestEngagedAnchor();
        public void RegisterEngagedAnchor(int anchorIndex);
        // public void CompleteAllAnchors(Action postAction);
        public void ClearCurrentAnchor();
        public void ClearClosetAnchor();
        public void ClearAnchors();
        public void InitPulseIndicator();
        
        public void SortAnchorObjByDistance();
        public AnchorController GetNeededConsumeAnchorController();
        public bool IsAllAnchorsComplete();
        public bool IsTheClosestAnchor(int anchorIndex);
        public void SetInteractable(bool interactable, int ignoreIndex = -1);
        public bool IsAnyAnchorFlying();
        public GameObject GetCalledAnchor();
        public void DeleteAnchorObject(int index);
    }
    public abstract class BaseAnchorManager<T> : BaseSingletonClass<T> where T : Component, IAnchorManager
    {
        //Anchor prefab
        public GameObject AnchorObjectPrefab;
        public GameObject AnchorObjectPrefabDEBUG;
        
        [Tooltip("Camera used to place the anchors via raycasting")]
        [SerializeField]
        private Camera _camera;
        [SerializeField] protected GameObject _arSpace;
        
        //when anchors are too far away, show an indicator
        [SerializeField] protected GameObject pulseIndicatorPrefab;
        protected List<GameObject> pulseIndicatorList = new List<GameObject>();
        protected List<PulseController> pulseControllerList = new List<PulseController>();
        
        //current anchor
        public int engagedAnchorIndex = -1;
        protected GameObject engagedAnchorObject;
        public int closestAnchorIndex = -1;
        
        protected AnchorController[] sortedAnchorControllers; //sorted by the distance to the user
        protected float[] sortedAnchorDistances; //sorted by the distance to the user
        
        //the latest engaged anchor object
        public bool isAlerted { get; set; }

        public abstract int AnchorCount { get; }
        protected int spawnedAnchors = 0; 
        // We cannot rely on the "Anchors" var inside the Update method.
        // So we need to have a separate var that holds the count of all anchors to be spawned/respawned.
        protected int anchorsToSpawn = 0;
        
        public int CalledAnchorIndex { get; set; }
        
        /// <summary>
        /// Gets a list of IDs of all activities in all the anchors.
        /// </summary>
        /// <returns></returns>

        public abstract IEnumerable<string> GetActivityIds();
        
        public GameObject GetAnchorObjectById(string id)
        {
            for (var i = 0; i < AnchorCount; i++)
            {
                var anchorObj = GetAnchorObjectAt(i);
                if(anchorObj == null)
                    continue;

                var aController = anchorObj.GetComponent<AnchorController>();
                if (id == aController.GetAnchorInfo().id)
                {
                    return anchorObj;
                }
            }

            return null;
        }
        public void HideActivities(string activeAnchorId = "") 
        {
            foreach (var anchorObj in GetAnchorObjectList())
            {
                var anchorController = anchorObj.GetComponent<AnchorController>();
                if(anchorController.GetAnchorInfo().id != activeAnchorId) 
                {
                    anchorController.HideActivities();
                }
            }
        }
        
        protected void SpawnAnchorFinalAction(int anchorIndex, AnchorController anchorController)
        {
            // register this anchor as the focused anchor
            print($"#debuglockanchor > anchorIndex={anchorIndex} #RegisterEngagedAnchor");
            RegisterEngagedAnchor(anchorIndex);
            
            // set target for Off-Screen indicator
            OffScreenIndicatorManager.Instance.SetTarget(anchorController.bodyCenter.transform);
            OffScreenIndicatorManager.Instance.ShowArrow();
        }

        /// <summary>
        /// Load anchors from the SBContext.
        /// </summary>
        public abstract void LoadAnchors(AnchorInfoList anchorList);

        protected abstract GameObject GetAnchorObjectAt(int index);
        protected abstract void RemoveAnchorObjectAt(int index);
        protected abstract void AddAnchorObjectToList(GameObject anchor);

        public GameObject GetCurrentAnchorObject()
        {
            return engagedAnchorObject;
        }
        
        public abstract List<GameObject> GetAnchorObjectList();

        protected void HandleAnchorInteraction()
        {
            //print("updateError 1");
            if (Input.touchCount <= 0) return;

            //print("updateError 2");
            var touch = Input.GetTouch(0);
            //print("updateError 3");
            if (touch.phase != TouchPhase.Ended) return;

            //print("updateError 4");
            if (ReferenceEquals(EventSystem.current.currentSelectedGameObject, null))
            {
                var hitX = new RaycastHit();
                //print("updateError 5");
                var rayX = Camera.main.ScreenPointToRay(touch.position);

                //print("updateError 6");
                if (Physics.Raycast(rayX, out hitX)) //if it hits a game object
                {
                    // //get the hit anchor object
                    // GameObject hitObj = hit.transform.gameObject;
                    // AnchorController hitAnchorControler = hitObj.GetComponentInParent<AnchorController>();
                    // if (hitAnchorControler != null)
                    // {
                    //     this.currAnchorObj = hitAnchorControler.gameObject;
                    //     if (hitAnchorControler.GetUIMode() != AnchorController.UIMode.Consuming) //if it's creator scenario
                    //     {
                    //         // if (hitObj.name.StartsWith(Const.ANCHOR_OBJ_PREFIX_COVER)) //if a cover is clicked
                    //         // {
                    //         //     hitAnchorControler.ToggleEditButtons();
                    //         //     RegisterCurrentAnchor(hitAnchorControler.index);
                    //         // }
                    //     }
                    //     else //if it's consumer scenario
                    //     {
                    //         // if(hitObj.name == Const.ANCHOR_OBJ_COMPLETION_OBJ_NAME) //if 'check-in' object is clicked
                    //         // {
                    //         //     hitAnchorControler.OnCheckIn();
                    //         // }
                    //     }
                    // }
                    // else //when touching non-anchor object
                    // {
                    //     //do nothing, to-be-extended
                    // }
                }
                else //if it doesn't hit any game object
                {
                    //do nothing, to-be-extended
                }

                return;
            }

            //print("updateError 8");
            //when touch on the UI (overlay 2D UI or 2D UI on 3D objects)
            AudioManager.Instance.PlayAudio(AudioManager.AudioOption.Tap);
            //print("updateError 9");
            RaycastHit hit = new RaycastHit();
            //print("updateError 10");
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            //print("updateError 11");
            if (!Physics.Raycast(ray, out hit)) return;
            //print("updateError 12");
            GameObject hitObj = hit.transform.gameObject;
            //print("updateError 13");
            AnchorController hitAnchorControler = hitObj.GetComponentInParent<AnchorController>();
            //print("updateError 14");
            if (hitAnchorControler == null) return;

            //register as focus
            print($"#debuglockanchor > Tapped some UI on anchor index={hitAnchorControler.index} #RegisterEngagedAnchor");
            //print("updateError 15");
            RegisterEngagedAnchor(hitAnchorControler.index);
            //print("updateError 16");
            //toggle 'ears'
            if (hitAnchorControler.GetUIMode() == AnchorController.UIMode.Consuming) return;
            //print("updateError 17");
            if (hitObj.name.StartsWith(Const.ANCHOR_OBJ_PREFIX_COVER))
                hitAnchorControler.ToggleEditButtons();
            //print("updateError END");
        }

        public AnchorInfo GetEngagedAnchorInfo()
        {
            return engagedAnchorObject == null ? null : engagedAnchorObject.GetComponent<AnchorController>().GetAnchorInfo();
        }

        public void RegisterClosestEngagedAnchor()
        {
            print($"closestAnchorIndex={closestAnchorIndex} #engageanchordebug");
            if (closestAnchorIndex >= 0) return;
            print("#engageanchordebug Z");
            SBContextManager.Instance.context.hasAnchorDistanceChecked = false;
            this.StartThrowingCoroutine (CheckDistance(afterCheck), ex =>
            {
                print($"{ex.Message} #engageanchordebug ERROR");
            });
        }

        private void afterCheck()
        {
            print("afterCheck #RegisterEngagedAnchor");
            RegisterEngagedAnchor(closestAnchorIndex);
        }

        /// <summary>
        /// Light-weight registration of current anchor, in this case only report the index number
        /// </summary>
        /// <param name="anchorIndex"></param>
        public void RegisterEngagedAnchor(int anchorIndex)
        {
            if (anchorIndex < 0)
            {
                print($"Invalid anchor index: {anchorIndex}");
                return;
            }
            print($"engagedAnchorIndex={engagedAnchorIndex}, anchorIndex={anchorIndex} #engageanchordebug");
            //escaping situation
            if (engagedAnchorIndex == anchorIndex) {
                if(!engagedAnchorObject) {
                    engagedAnchorObject = GetAnchorObjectAt(anchorIndex);
                }
                return;
            }
            if (anchorIndex >= AnchorCount)
            {
                string errorMsg = $"Error: Current anchor index out of bound! anchorIndex={anchorIndex} | AnchorCount={AnchorCount}";
                print(errorMsg);
                Debug.LogError(errorMsg);
                return;
            }

            //when the index is 'valid'
            engagedAnchorIndex = anchorIndex;

            if (engagedAnchorObject) //close ears of the previous focus anchor, if there is.
            {
                var currentAnchorObjController = engagedAnchorObject.GetComponent<AnchorController>();
                
                if (currentAnchorObjController.index != anchorIndex && currentAnchorObjController.isScanningCompletedOrSkipped)
                {
                    currentAnchorObjController.TurnOffButtons();
                }
            }
            
            engagedAnchorObject = GetAnchorObjectAt(anchorIndex);
            //MessageManager.Instance.UpdateCurrentAnchorIndex(currAnchorIndex, currAnchorObj);
        }
        
        // public void CompleteAllAnchors(Action postAction) {
        //     
        //     AnchorController notReadyController = null;
        //     for (int i = 0; i < AnchorCount; i++)
        //     {
        //         var controller = GetAnchorObjectAt(i).GetComponent<AnchorController>();
        //         if (controller.IsReadyToCompleteConfig()) //automatically 'complete' its config only when any activity is saved.
        //         {
        //             controller.OnAnchorConfigCompleted(true);    
        //         }
        //         else {
        //             notReadyController = controller;
        //         }
        //     }
        //
        //     // when we break scanning second or next anchor
        //     if (notReadyController != null) {
        //         notReadyController.DoDeleteAnchor(postAction);
        //     }
        //     else {
        //         postAction.Invoke();
        //     }
        // }
        
        public void DeleteAnchorObject(int index)
        {
            // print("DeleteAnchorObj");
            if (index < 0) return;
            
            //reset the flags first, to avoid exception in Update() in AnchorManager
            if (index == engagedAnchorIndex)
                ClearCurrentAnchor();
            if(index == closestAnchorIndex)
                ClearClosetAnchor();
                
            //delete object from obj list
            var toBeDeletedAnchorObject = GetAnchorObjectAt(index);    
            RemoveAnchorObjectAt(index);

            //refresh anchor index
            for (int i = 0; i < AnchorCount; ++i)
            {
                var anchor = GetAnchorObjectAt(i);
                if (anchor == null) continue;
                anchor.GetComponentInChildren<AnchorController>().index = i;
            }
                
            //destroy game object
            Destroy(toBeDeletedAnchorObject);
                
            //delete object (pulse indicator), valid only during edit/consume
            if (!SBContextManager.Instance.context.IsConsuming() &&
                !SBContextManager.Instance.context.isEditing) return;
                
            var toBeDeletedAnchorIndicator = pulseIndicatorList[index];
            toBeDeletedAnchorIndicator.GetComponent<PulseController>().SetAnchorController(null);
            pulseIndicatorList.RemoveAt(index);
            pulseControllerList.RemoveAt(index);
                    
            Destroy(toBeDeletedAnchorIndicator);
        }
        
        public void ClearCurrentAnchor()
        {
            engagedAnchorIndex = -1;
            engagedAnchorObject = null;
        }
        
        public void ClearClosetAnchor()
        {
            closestAnchorIndex = -1;
        }
        
        public virtual void ClearAnchors()
        {
            foreach (var obj in pulseIndicatorList)
                Destroy(obj);
            foreach (var obj in pulseControllerList)
                Destroy(obj);

            pulseIndicatorList.Clear();
            pulseControllerList.Clear();
            
            //reset flags
            ClearCurrentAnchor();
            ClearClosetAnchor();
        }
        
        protected void CheckAnchorEngagement(float minDistance)
        {
            if (closestAnchorIndex == -1 || closestAnchorIndex >= AnchorCount)
                return;
            
            //if it's close enough
            var closestAnchorObj = GetAnchorObjectAt(closestAnchorIndex);
            var aController = closestAnchorObj.GetComponent<AnchorController>();

            if (minDistance > Const.DISTANCE_TO_SWAP_MARKER_AND_ANCHOR || minDistance > Const.DISTANCE_TO_ENGAGE_ANCHOR)
            {
                //aController.SwapToMarker(true);
                SetNoAnchorEngaged();
                return;
            }
             
            //set it engaged
            if (!aController.IsReadyToEngage() || aController.isEngaged) return;
            
            //un-engage the previous engaged one
            if (engagedAnchorIndex != -1 && engagedAnchorIndex != closestAnchorIndex)
                GetAnchorObjectAt(engagedAnchorIndex).GetComponent<AnchorController>().OnUnengaged();
            else if (engagedAnchorIndex != -1 && engagedAnchorIndex == closestAnchorIndex)
                return;

            //engage it
            engagedAnchorIndex = closestAnchorIndex;
            aController.OnEngaged();
            MessageManager.Instance.DebugMessage($"Anchor \'{closestAnchorIndex}\' is engaged!");
        }
        
        protected void SetNoAnchorEngaged()
        {
            if (engagedAnchorIndex == -1) return;
            
            GetAnchorObjectAt(engagedAnchorIndex).GetComponent<AnchorController>().OnUnengaged();
            engagedAnchorIndex = -1;
        }

        public abstract bool IsReadyToSaveMap();
        
        public bool IsTheClosestAnchor(int anchorIndex)
        {
            return anchorIndex == closestAnchorIndex;
        }
        
        protected void CheckIfUserIsTooFarAway(bool isAnyAnchorIsCloseToUser)
        {
            if (!isAnyAnchorIsCloseToUser 
                && SBContextManager.Instance 
                && SBContextManager.Instance.context != null 
                && !SBContextManager.Instance.context.IsConsuming())
            {
                if (isAlerted) return;
                
                ActivityUIFacade.Instance.FireUIEvent(ActivityUIFacade.UIEvent.UserTooFarAway); //when user walk out of the 'focus zone'
                isAlerted = true;
            }
            else if (isAlerted && isAnyAnchorIsCloseToUser)
            {
                BottomPanelManager.Instance.HideCurrentPanel();
                isAlerted = false;
            }
        }
        
        protected IEnumerator CheckDistance(Action callback)
        {
            print($"hasAnchorDistanceChecked={SBContextManager.Instance.context.hasAnchorDistanceChecked} #engageanchordebug");
            if (!SBContextManager.Instance.context.hasAnchorDistanceChecked)
            {
                SBContextManager.Instance.context.hasAnchorDistanceChecked = true;
                yield return new WaitForSeconds(1f);
            }
            var isAnyAnchorIsCloseToUser = false;
            var minDistance = float.MaxValue;
            for (var i = 0; i < AnchorCount; i++)
            {
                var anchorObj = GetAnchorObjectAt(i);
                var aController = anchorObj.GetComponent<AnchorController>();
                var anchorBodyCenter = anchorObj.GetComponent<AnchorController>().bodyCenter;
                var distance = Vector3.Distance(_camera.transform.position, anchorBodyCenter.transform.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestAnchorIndex = i;
                    print($"closestAnchorIndex={closestAnchorIndex} #engageanchordebug");
                }

                // print($"anchor distance={distance} | isAnyAnchorIsCloseToUser={isAnyAnchorIsCloseToUser} | isEditing={SBContextManager.Instance.context.isEditing} | spawnedAnchors={spawnedAnchors} | anchorsToSpawn={anchorsToSpawn} " +
                //       $"| DistToAlertUser={Const.DISTANCE_TO_ALERT_USER} | distanceForMarker={SBContextManager.Instance.context.distanceForMarker} " +
                //       $"| distanceForIndicator={SBContextManager.Instance.context.distanceForIndicator}" +
                //       $"| flyingLock={!aController.flyingLock}  #engageanchordebug 1");
                if (distance < Const.DISTANCE_TO_ALERT_USER)
                {
                    isAnyAnchorIsCloseToUser = true;
                }

                if (!SBContextManager.Instance.context.IsConsuming() &&
                    (!SBContextManager.Instance.context.isEditing)) continue;
                
                //switch between marker and anchor object
                if (!aController.flyingLock)
                {
                    if (distance <= SBContextManager.Instance.context.distanceForMarker)
                        //if (distance <= Const.DISTANCE_4_TOO_FAR_INDICATOR)
                    {
                        aController.SetAnchorMode(AnchorController.AnchorMode.Anchor, true, () =>
                        {
                            aController.isModeSettled = true;
                        });
                    }
                    else if (distance > SBContextManager.Instance.context.distanceForMarker && distance < SBContextManager.Instance.context.distanceForIndicator)
                        //else if (distance > Const.DISTANCE_4_TOO_FAR_INDICATOR && distance < Const.DISTANCE_4_SUPER_FAR_INDICATOR)
                    {
                        aController.SetAnchorMode(AnchorController.AnchorMode.Marker, true, () =>
                        {
                            aController.isModeSettled = true;
                        });
                    }
                    else if(distance >= SBContextManager.Instance.context.distanceForIndicator)
                        //else if(distance >= Const.DISTANCE_4_SUPER_FAR_INDICATOR)
                    {
                        aController.SetAnchorMode(AnchorController.AnchorMode.Indicator, true, () =>
                        {
                            aController.isModeSettled = true;
                        });
                    }
                    // print($"isModeSettled={aController.isModeSettled} #debugconsume");
                }

                //pulse indicator (not for create)
                if (pulseIndicatorList.Count != AnchorCount ||
                    pulseControllerList.Count != AnchorCount) continue;
                
                var pulseIndicator = this.pulseIndicatorList[i];
                var pulseController = this.pulseControllerList[i];
                if (distance > Const.DISTANCE_4_SUPER_FAR_INDICATOR)
                {
                    var screenPos = _camera.WorldToScreenPoint(anchorBodyCenter.transform.position);
                    if (!(screenPos.z > 0)) continue;
                    
                    pulseIndicator.SetActive(true);
                    pulseIndicator.transform.position = screenPos;

                    pulseController.SetDistanceValue(distance);
                    pulseController.StartPulseAnimation();
                    pulseController.isInteractable = true;
                                
                    //check if it's completed, change color if it's.
                    if (!pulseController.isCompleted && aController.AllActivitiesCompleted())
                        pulseController.SetCompletedStyle();

                    //check if we need to enable user interaction
                    // if (distance <= Const.DISTANCE_4_SUPER_FAR_INDICATOR)
                    // {
                    //     pulseController.StartPulseAnimation();
                    //     pulseController.isInteractable = true;
                    // }
                    // else
                    // {
                    //     pulseController.StopPulseAnimation();
                    //     pulseController.isInteractable = false;
                    // }
                                
                    //adjust the size
                    // float ratio = (distance - Const.DISTANCE_4_TOO_FAR_INDICATOR) /
                    //               (Const.DISTANCE_4_SUPER_FAR_INDICATOR -
                    //                Const.DISTANCE_4_TOO_FAR_INDICATOR);
                    // Vector3 size = Vector3.Lerp(Const.PULSE_INDICATOR_SIZE_MIN, Const.PULSE_INDICATOR_SIZE_MAX, ratio);
                    // pulseIndicator.transform.localScale = size;

                    //test adjust the alpha
                    // MessageManager.Instance.DebugMessage($">>>>> screenPos={screenPos}");
                    //  if (i > 0)
                    //  {
                    //      pulseController.UpdateAlphaAccordingToDistance(0.5f);
                    //  }
                }
                else
                {
                    pulseIndicator.SetActive(false);
                }
            }

            if (closestAnchorIndex == -1) yield break;
            
            //check anchor's engagement
            CheckAnchorEngagement(minDistance);

            //alert if user goes too far away
            CheckIfUserIsTooFarAway(isAnyAnchorIsCloseToUser);
            
            callback?.Invoke();
        }
        
        public void SetInteractable(bool interactable, int ignoreIndex = -1)
        {
            for (var i = 0; i < AnchorCount; i++)
            {
                if (ignoreIndex != -1)
                {
                    if (i == ignoreIndex)
                        continue;
                }

                GetAnchorObjectAt(i).GetComponent<AnchorController>().SetInteractable(interactable);
            }
        }
        
        public bool IsAnyAnchorFlying()
        {
            foreach (var anchorObj in GetAnchorObjectList())
            {
                var controller = anchorObj.GetComponent<AnchorController>();
                if (controller.flyingLock)
                    return true;
            }

            return false;
        }

        public GameObject GetCalledAnchor()
        {
            return GetAnchorObjectAt(CalledAnchorIndex);
        }
        
        
        #region Pulse
        
        /// <summary>
        /// Create pulse indicator for anchors according to distance.
        /// </summary>
        public void InitPulseIndicator()
        {
            var tempPulseIndicatorDict = new Dictionary<int, GameObject>();

            print($"sortedAnchors={sortedAnchorControllers.Length}");
            for (var i = this.sortedAnchorControllers.Length - 1; i >= 0 ; i--) //dsc order, far objects render first
            {
                var aController = sortedAnchorControllers[i];
                print($"anchor index={aController.index}");
                var pulseIndicator = Instantiate(pulseIndicatorPrefab, UIManager.Instance.gameObject.transform);
                tempPulseIndicatorDict.Add(aController.index, pulseIndicator);
                var controller = pulseIndicator.GetComponent<PulseController>();
                controller.SetAnchorController(aController);
                
                pulseIndicator.SetActive(false);
            }

            Debug.Log($">>>>>>>before sorting>>>>>>>>>>>>");
            foreach (var item in tempPulseIndicatorDict)
            {
                Debug.Log($"item.Key={item.Key}");
            }
            
            Debug.Log($">>>>>>>after sorting>>>>>>>>>>>>");
            
            var sortedDict = from entry in tempPulseIndicatorDict orderby entry.Key select entry;
            foreach (var item in sortedDict)
            {
                Debug.Log($"item.Key={item.Key}");
                var pulseIndicator = item.Value;
                var controller = pulseIndicator.GetComponent<PulseController>();
                pulseIndicatorList.Add(pulseIndicator);
                pulseControllerList.Add(controller);
            }
        }
        
        #endregion
        
        #region Sorting
        
        /// <summary>
        /// Sort the anchor objects according to the distance to user
        /// </summary>
        public void SortAnchorObjByDistance() 
        {
            print("SortAnchorObjByDistance 1");
            var aControllerDict = new Dictionary<AnchorController, float>();
            print($"SortAnchorObjByDistance 2 -> AnchorCount={AnchorCount} #spawnanchor");
            for (var i = 0; i < AnchorCount; i++)
            {
                print("SortAnchorObjByDistance 3a");
                var anchorObj = GetAnchorObjectAt(i);
                print("SortAnchorObjByDistance 3b");
                var aController = anchorObj.GetComponent<AnchorController>();
                print("SortAnchorObjByDistance 3c");
                var dist = (Vector3.Distance(aController.bodyCenter.transform.position, Camera.main.transform.position));
                print("SortAnchorObjByDistance 3d");
                aControllerDict.Add(aController, dist);
            }
            print("SortAnchorObjByDistance 4");
            var sortedDict = from entry in aControllerDict 
                orderby entry.Value ascending select entry;
            print("SortAnchorObjByDistance 5");
            sortedAnchorControllers = new AnchorController[aControllerDict.Count];
            print("SortAnchorObjByDistance 6");
            sortedAnchorDistances = new float[aControllerDict.Count];
            print($"SortAnchorObjByDistance 7 -> anchorControllers={aControllerDict.Count} #spawnanchor");
            for (var i = 0; i < aControllerDict.Count; i++) 
            {
                print("SortAnchorObjByDistance 8a");
                sortedDict.ElementAt(i).Key.sortOrder = i; //set the sort order into AnchorController
                print("SortAnchorObjByDistance 8b");
                sortedAnchorControllers[i] = sortedDict.ElementAt(i).Key;
                print("SortAnchorObjByDistance 8c");
                sortedAnchorDistances[i] = sortedDict.ElementAt(i).Value;
            }

            closestAnchorIndex = 0;
        }

        
        /// <summary>
        /// This will return the anchor that the off-screen indicator will use.
        /// </summary>
        /// <returns></returns>
        /// <remarks>If return null, we complete all anchors</remarks> 
        public AnchorController GetNeededConsumeAnchorController() {
            // We need to have a status per anchor.
            if (sortedAnchorControllers == null) return null;
            
            // We have the status for all anchors - we know if the AR session is completed. <-- have this
            // check each activities
            if (sortedAnchorControllers.Length == 0) {
                return null;
            }

            // return anchorControllers.OrderBy(x => x.sortOrder).First();
            try {
                return sortedAnchorControllers.OrderBy(x => x.sortOrder).FirstOrDefault(x => !x.AllActivitiesCompleted());
            }
            catch (Exception e) {
                print($"AnchorManager GetNeededConsumeAnchorController Exception = {e.Message}");
                return null;
            }
            
            //anchorControllers[0].AllActivitiesCompleted()

            
            //return null;
        }

        public bool IsAllAnchorsComplete() {
            print("AnchorManager IsAllAnchorsComplete()");
            foreach (var anchorController in sortedAnchorControllers) {
                if (!anchorController.AllActivitiesCompleted()) {
                    return false;
                }
            }
            
            return true;
        }
        
        

        #endregion
    }
}