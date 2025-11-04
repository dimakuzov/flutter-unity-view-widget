using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;


namespace SocialBeeAR
{
    
    public class GPSAnchorController : MonoBehaviour
    {
        
        //UI elements
        [SerializeField] private Renderer Outer;
        [SerializeField] private Renderer Middle;
        [SerializeField] private Text distanceText;
        [SerializeField] private GameObject IxPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descText;
        [SerializeField] private GameObject focusEffect;
        [SerializeField] private GameObject checkpointUp;
        [SerializeField] private GameObject checkpointDown;

        //Sprites for activity icons
        [SerializeField] private Sprite spritePost;
        [SerializeField] private Sprite spriteTrivia;
        [SerializeField] private Sprite spritePhotoVideo;
        [SerializeField] private Sprite spriteAudio;
        private List<Sprite> actSpriteList = new List<Sprite>();
        private List<Image> actImageList = new List<Image>();
        [SerializeField] private GameObject iconRowParent;
        
        private Camera ARCamera;
        private Vector3 lookAtTargetPos = Vector3.zero;
        
        private Vector3 ixPanelLocalPosOrigin = new Vector3(0, 0, 0);
        private Vector3 ixPanelLocalPosEngaged = new Vector3(0, 0.33f, 0);
        
        private string mapId;
        
        //scale control
        private float standardCpHeight = 120f;
        private float standardDist = 19.48864f;
        private float initialScale = 5;
        private float standardCpHeightObj = -1;
        private Vector3 finalScaleFactorVector3 = Vector3.zero;
        
        //flag indicating if this anchor (body plus activities) is interactable
        private bool isInteractable;
        
        private UIMode currMode = UIMode.Undefined;
        

        public enum UIMode //difference appearance under different GPS signal strength
        {
            Undefined,
            Good,
            Medium,
            Bad
        }
    
    
        void Start()
        {
            // IxPanel?.SetActive(false);
            // focusEffect?.SetActive(false);
        }
        
        
        private void Update()
        {
            //check distance and engage/un-engage
            CheckDistance();
        }


        public void Init(Camera camera, AnchorDto dto)
        {
            ARCamera = camera;

            //parsing anchor and activity information from AnchorDTO
            if (dto != null)
            {
                this.mapId = dto.mapID;
            
                actSpriteList.Clear();
                string title = "";
                string desc = "";
                foreach (var activity in dto.activities)
                {
                    var typeEnum = (ActivityType) activity.type;
                    switch (typeEnum)
                    {
                        case ActivityType.CheckIn:
                        case ActivityType.Post:
                            actSpriteList.Add(spritePost);
                            title = activity.text;
                            desc = activity.description;
                            break;
                        case ActivityType.Trivia:
                            actSpriteList.Add(spriteTrivia);
                            break;
                        case ActivityType.PhotoVideo:
                            actSpriteList.Add(spritePhotoVideo);
                            break;
                        case ActivityType.Audio:
                            actSpriteList.Add(spriteAudio);
                            break;
                    }
                }
            
                //set title/desc
                print(string.Format(
                    "Preparing GPS anchor: title='{0}', desc='{1}', mapId='{2}'", title, desc, dto.mapID));
                titleText.text = title;
                descText.text = desc;

                //set activity icons
                actImageList.Clear();
                if (actSpriteList.Count == 1)
                {
                    iconRowParent.transform.GetChild(0).gameObject.SetActive(true);
                    iconRowParent.transform.GetChild(1).gameObject.SetActive(false);
                    iconRowParent.transform.GetChild(2).gameObject.SetActive(false);
                    actImageList.AddRange(iconRowParent.transform.GetChild(0).GetComponentsInChildren<Image>());
                }
                else if(actSpriteList.Count == 2)
                {
                    iconRowParent.transform.GetChild(0).gameObject.SetActive(false);
                    iconRowParent.transform.GetChild(1).gameObject.SetActive(true);
                    iconRowParent.transform.GetChild(2).gameObject.SetActive(false);
                    actImageList.AddRange(iconRowParent.transform.GetChild(1).GetComponentsInChildren<Image>());
                }
                else if (actSpriteList.Count == 3)
                {
                    iconRowParent.transform.GetChild(0).gameObject.SetActive(false);
                    iconRowParent.transform.GetChild(1).gameObject.SetActive(false);
                    iconRowParent.transform.GetChild(2).gameObject.SetActive(true);
                    actImageList.AddRange(iconRowParent.transform.GetChild(2).GetComponentsInChildren<Image>());
                }
                
                for (int i = 0; i < actImageList.Count; i++)
                {
                    actImageList[i].sprite = actSpriteList[i];
                }
            }

            //Set UI mode
            SetUIMode(UIMode.Undefined);
        }


        private void CheckDistance()
        {
            // print("GPSAnchorController.CheckDistance");
            if (ARCamera == null)
                return;
            
            //set engaged/unengaged according to the distance
            float nowDist = Vector3.Distance(transform.position, ARCamera.transform.position);
            float distanceFeet = nowDist * Const.meter2feetFactor;
            Int64 distanceInt = Convert.ToInt64(distanceFeet); 
            distanceText.text = distanceInt.ToString();
            
            //set watching at the player
            lookAtTargetPos.Set(ARCamera.transform.position.x, transform.position.y, ARCamera.transform.position.z);
            transform.LookAt(lookAtTargetPos, Vector3.up);
            
            //scale control: scale if it's too far away
            ScaleControl(nowDist);

            //handle engagement
            if (nowDist <= Const.DISTANCE_TO_ENGAGE_GPS_ANCHOR)
            {
                OnEngaged();
            }
            else
            {
                OnUnengaged();
            }
        }
        

        private void ScaleControl(float distance)
        {
            Vector3 cpUpScreenPos = ARCamera.WorldToScreenPoint(checkpointUp.transform.position);
            Vector3 cpDownScreenPos = ARCamera.WorldToScreenPoint(checkpointDown.transform.position);
            float cpDistOnScreen = Vector3.Distance(cpUpScreenPos, cpDownScreenPos);
            
            if (cpDistOnScreen < standardCpHeight) //when it's too far away
            {
                float nowCpHeightObj = (distance * standardCpHeightObj) / standardDist;
                float scaleFactor = nowCpHeightObj / standardCpHeightObj;
            
                if (scaleFactor > 1)
                {
                    float finalScaleFactor = initialScale * scaleFactor;
                    finalScaleFactorVector3.Set(finalScaleFactor, finalScaleFactor, finalScaleFactor);
                    transform.localScale = finalScaleFactorVector3;
                }
            }
        }
        

        private bool isEngaged = false;
        private void OnEngaged()
        {
            print("GPSAnchorController.OnEngaged");
            if (isEngaged)
                return;
            else
            {
                isEngaged = true;
                //print("GPS anchor Engaged!");
            }
            
            //show interaction panel
            IxPanel.SetActive(true);
            IxPanel.transform.localPosition = ixPanelLocalPosOrigin;
            IxPanel.transform.DOLocalMove(ixPanelLocalPosEngaged, 1f).SetEase(Ease.InOutBack);

            //show focus effect
            focusEffect?.SetActive(true); 
            
            //register this anchor as the focused anchor
            GPSAnchorManager.Instance.RegisterCurrentAnchor(gameObject);
        }


        public void OnUnengaged()
        {
            if (!isEngaged)
                return;
            else
            {
                isEngaged = false;
                //print("GPS anchor Un-engaged!");
            }
            
            IxPanel.transform.localPosition = ixPanelLocalPosEngaged;
            IxPanel.transform.DOLocalMove(ixPanelLocalPosOrigin, 1f).SetEase(Ease.OutQuint).OnComplete(() =>
            {
                IxPanel.SetActive(false);
            });
            
            //hide focus effect
            focusEffect?.SetActive(false);
            
            //clear the focused anchor
            GPSAnchorManager.Instance.ClearCurrentAnchor();
        }

        
        public void SetUIMode(UIMode mode)
        {
            if (mode == currMode)
                return;
            
            currMode = mode;
            
            switch (mode)
            {
                case UIMode.Good:
                    Outer.material.color = Const.OUTER_GOOD;
                    Middle.material.color = Const.MIDDLE_GOOD;
                    break;
                case UIMode.Medium:
                    Outer.material.color = Const.OUTER_MEDIUM;
                    Middle.material.color = Const.MIDDLE_MEDIUM;
                    break;
                case UIMode.Bad:
                    Outer.material.color = Const.OUTER_BAD;
                    Middle.material.color = Const.MIDDLE_BAD;
                    break;
                case UIMode.Undefined:
                    Outer.material.color = Const.OUTER_DEFAULT;
                    Middle.material.color = Const.MIDDLE_DEFAULT;
                    break;
            }
        }


        public void LoadMap()
        {
            //if(!string.IsNullOrEmpty(mapId))
            InteractionManager.Instance.LoadNewMap(mapId);
        }
        
        
        public void SetInteractable(bool interactable)
        {
            if (interactable != isInteractable)
            {
                isInteractable = interactable;
                Utilities.SetCanvasGroupInteractable(gameObject, interactable);
            }
        }

    }
    
}

