using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;


namespace SocialBeeAR
{
    
    public class ObjectShaker : MonoBehaviour
    {
    
        //how much it shakes
        [SerializeField] private float shakeAmount = 1f;
    
        //how long it lasts
        [SerializeField] private float shakeLasting = 0.25f;
    
        private bool isShaking = false;
    
        private void Update() 
        {
            if(isShaking)
            {
                Vector3 newLocalPos = Random.insideUnitSphere * (Time.deltaTime * shakeAmount);
                newLocalPos.y = transform.localPosition.y;
                newLocalPos.z = transform.localPosition.z;

                transform.localPosition = newLocalPos;
            }
        }
    

        public void Shake(Action postAction)
        {
            StartCoroutine(StartShaking(postAction));
        }
    

        IEnumerator StartShaking(Action postAction)
        {
            Vector3 originalPos = transform.position;
            if(!isShaking)
            {
                isShaking = true;
            }

            yield return new WaitForSeconds(shakeLasting);

            isShaking = false;
            transform.position = originalPos;
            
            postAction.Invoke();
        }
    }
    
}

