using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{
    
    /// <summary>
    /// Facade class for MappingPanel
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class MappingFacade : UIFacade
    {

        public enum UIMode
        {
            UnDefined,
            Mapping_PreScanning,
            Mapping_Scanning,
            Mapping_PostScanning
        }
        private UIMode currentUIMode = UIMode.UnDefined;

        
        //parts
        [SerializeField] GameObject cancelMappingButton; //+

        [SerializeField] GameObject preMappingInfo; //+
        [SerializeField] GameObject preMappingButton; //+
        
        [SerializeField] GameObject mappingInfo; //+
        [SerializeField] GameObject mappingProgressBar; //+; //+
        
        [SerializeField] GameObject postMappingInfo; //+
        [SerializeField] GameObject postMappingButton; //

        //elements for progress
        [SerializeField] Text mappingProgressNum;
        [SerializeField] Image mappingProgressFillImage;

        //the animation for assistance
        [SerializeField] Animator moveDeviceAnimation;
        
        [SerializeField] GameObject arCamera;
        
        private static MappingFacade _instance;
        public static MappingFacade Instance
        {
            get
            {
                return _instance;
            }
        }


        private void Awake()
        {
            _instance = this;
            Categorize();
        }


        private void Categorize()
        {
            mutualElements.Add(cancelMappingButton);
            mutualElements.Add(preMappingInfo);
            mutualElements.Add(preMappingButton);
            mutualElements.Add(mappingInfo);
            mutualElements.Add(mappingProgressBar);
            mutualElements.Add(postMappingInfo);
            mutualElements.Add(postMappingButton);

            //any button or button parent must be added into the list.
            bottomElements.Add(cancelMappingButton);
            bottomElements.Add(preMappingButton);
            bottomElements.Add(postMappingButton);
        }


        public UIMode GetUIMode()
        {
            return this.currentUIMode;
        }


        public void SetUIMode(UIMode uiMode)
        {
            if (this.currentUIMode != uiMode)
            {
                this.currentUIMode = uiMode;
                switch (uiMode)
                {
                    case UIMode.Mapping_PreScanning:
                        DeactiveAll();
                        this.preMappingInfo.SetActive(true);
                        this.cancelMappingButton.SetActive(true);
                        this.preMappingButton.SetActive(true);
                        //StartCoroutine(CheckIfUserIsCloseEnoughToTheAnchorObj());
                        break;

                    case UIMode.Mapping_Scanning:
                        DeactiveAll();
                        this.mappingInfo.SetActive(true);
                        this.mappingProgressBar.SetActive(true);
                        this.cancelMappingButton.SetActive(true);
                        this.EnableMoveAnimation(true);
                        break;
                    
                    case UIMode.Mapping_PostScanning:
                        DeactiveAll();
                        this.postMappingInfo.SetActive(true);
                        this.cancelMappingButton.SetActive(true);
                        this.postMappingButton.SetActive(true);
                        this.EnableMoveAnimation(false);
                        break;

                    default:
                        DeactiveAll();
                        print("MappingFacade: unexpected UI mode.");
                        break;
                }
            }
            UpdateBottomBanner();
        }


        // private Vector2 anchorObjectPos2D = Vector2.zero;
        // private Vector2 playerPos2D = Vector2.zero;
        // private float maxDistance = 1.5f;
        // private IEnumerator CheckIfUserIsCloseEnoughToTheAnchorObj()
        // {
        //     GameObject anchorObj = AnchorManager.Instance.GetCurrentAnchorObject();
        //     
        //     anchorObjectPos2D.Set(anchorObj.transform.position.x, anchorObj.transform.position.z);
        //     playerPos2D.Set(arCamera.transform.position.x, arCamera.transform.position.z);
        //     float distance = Vector2.Distance(anchorObjectPos2D, playerPos2D);
        //
        //     while (distance > maxDistance)
        //     {
        //         this.preMappingInfo.SetActive(false);
        //         this.preMappingButton.SetActive(false);
        //
        //         anchorObjectPos2D.Set(anchorObj.transform.position.x, anchorObj.transform.position.z);
        //         playerPos2D.Set(arCamera.transform.position.x, arCamera.transform.position.z);
        //         distance = Vector2.Distance(anchorObjectPos2D, playerPos2D);
        //         
        //         yield return null; //go to the next frame
        //     }
        //
        //     this.preMappingInfo.SetActive(true);
        //     this.preMappingButton.SetActive(true);
        // }


        public void ResetMappingProgress()
        {
            mappingProgressFillImage.gameObject.GetComponent<Image>().fillAmount = 0;
            mappingProgressNum.text = "";
        }


        public void SetMappingProgress(float percentageUnderOne)
        {
            float percentage = Mathf.Max(percentageUnderOne, 0f);
            mappingProgressFillImage.gameObject.GetComponent<Image>().fillAmount = percentage;
            mappingProgressNum.text = (percentage * 100).ToString();
        }
        
        
        public void EnableMoveAnimation(bool enable)
        {
            moveDeviceAnimation.SetTrigger(enable ? Const.ANIMATION_FADE_ON : Const.ANIMATION_FADE_OFF);
        }

    }

}