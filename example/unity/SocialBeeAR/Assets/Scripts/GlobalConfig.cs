using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SocialBeeAR
{
    /// <summary>
    /// This class is for preserving cross-scene variables.
    /// </summary>
    public class GlobalConfig : MonoBehaviour 
    {
        public static GlobalConfig Instance;

        void Awake ()   
        {
            if (Instance == null) //if it's init for the first time
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else if (Instance != this) //if it's not the first time
            {
                Destroy (gameObject);
            }
        }
        
    }
}
