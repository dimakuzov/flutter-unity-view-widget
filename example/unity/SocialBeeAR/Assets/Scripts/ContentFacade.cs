using System.Collections;
using UnityEngine;


namespace SocialBeeAR
{
    /// <summary>
    /// Parent of all Facade class for 3D content of an anchor object
    /// (Facade class is for managing interaction for a set of UI components)
    /// </summary>
    public class ContentFacade : MonoBehaviour
    {
        
        /// <summary>
        /// It represents the role of the user and at which stage the user is.
        /// </summary>
        public enum UIMode
        {
            Creator_PreSetting, //when an anchor object is born
            Creator_Setting, //when user tap it and start setting its activity type & detail
            Creator_PostSetting, //after the anchor scanned (but still in creation process)
            Creator_PreScanning, //equal to 'MappingFacade.UIMode.Mapping_PreScanning', showing info like 'ready to scan'
            Creator_Scanning, //equal to 'MappingFacade.UIMode.Mapping_Scanning', during the scanning
            Creator_PostScanning, //equal to 'MappingFacade.UIMode.Mapping_PostScanning', after scanning
            Consumer //during the consumer process
        }
        
        [SerializeField] private GameObject completionObj;
        [SerializeField] private GameObject completionEffectPrefab;
        
        [SerializeField] protected GameObject editButton;
        
        private bool isCompleted;
        private float loopTimeLimit = 2.0f;
        
        protected void Init(UIMode uiMode)
        {
            //init basic components
            if (uiMode != UIMode.Consumer) //for creator
            {
                completionObj.SetActive(true);
                editButton.SetActive(true);
            }
            else //for consumer
            {
                completionObj.SetActive(!isCompleted);
                editButton.SetActive(false);
            }
        }
        
        //--------------------------- completion effect -----------------------------

        public void OnComplete()
        {
            isCompleted = true;
            PlayCheckinEffect();
            completionObj.SetActive(false);
        }
        
        public void PlayCheckinEffect()
        {
            StartCoroutine(EffectLoop());
        }


        IEnumerator EffectLoop()
        {
            GameObject effectPlayer = (GameObject)Instantiate(
                completionEffectPrefab, completionObj.transform.position, completionObj.transform.rotation);
            yield return new WaitForSeconds(loopTimeLimit);

            Destroy(effectPlayer);

            //loopping
            //PlayCheckinEffect();  
        }
    }
}


