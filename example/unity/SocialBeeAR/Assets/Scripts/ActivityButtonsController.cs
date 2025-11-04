using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SocialBeeAR
{
    
    public class ActivityButtonsController : MonoBehaviour
    {

        [SerializeField] private GameObject disappearPoint;
        [SerializeField] private List<GameObject> activityButtonList;
        [SerializeField] private List<Vector3> createInConsumePoses;
        
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (checkingVisibility)
            {
                for (int i = 0; i < activityButtonList.Count; i++)
                {
                    if (activityButtonList[i].transform.position.y <= disappearPoint.transform.position.y)
                    {
                        activityButtonList[i].SetActive(false);
                    }
                    else
                    {
                        activityButtonList[i].SetActive(true);
                    }
                }
            }
        }

        private bool checkingVisibility = false;
        public void CheckingVisibility(bool startChecking)
        {
            checkingVisibility = startChecking;
        }

        public void SetCreateInConsumeButtons() {
            // trivia button
            activityButtonList[1].SetActive(false);
            activityButtonList[1] = activityButtonList[0];

            activityButtonList[0].transform.localPosition = createInConsumePoses[0];
            activityButtonList[2].transform.localPosition = createInConsumePoses[1];
            activityButtonList[3].transform.localPosition = createInConsumePoses[2];
        }
    }

}

