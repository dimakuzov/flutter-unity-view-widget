using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SocialBeeAR
{
    public class AnchorConfigCompletionEffectController : MonoBehaviour
    {

        [SerializeField] private GameObject anchorConfigCompletionEffect;

        private static AnchorConfigCompletionEffectController _instance;
        public static AnchorConfigCompletionEffectController Instance
        {
            get
            {
                return _instance;
            }
        }


        //------------------------Monobehaviour methods-------------------------


        void Awake()
        {
            _instance = this;
        }

        /// <summary>
        /// This method to be called every time an anchor is spawned. 
        /// </summary>
        public void PrepareEffect()
        {
            anchorConfigCompletionEffect.SetActive(false);
        }

        /// <summary>
        /// This method to be called every time when an anchor configuration is set to 'completed'
        /// </summary>
        public void RunEffect(Vector3 anchorBottomPosition)
        {
            anchorConfigCompletionEffect.transform.position = anchorBottomPosition;
            anchorConfigCompletionEffect.SetActive(true);
        }
    

    }

}

