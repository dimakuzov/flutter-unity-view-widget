using SocialBeeARDK;
using UnityEngine;


namespace SocialBeeAR
{
    public class PhonePoseManager : BaseSingletonClass<PhonePoseManager>
    {
        [SerializeField] private GameObject phonePoseWarningPanel;
        [SerializeField] private GameObject trackingLostAlertPanel;

        private bool isEnabled;

        
        // Start is called before the first frame update
        private void Start()
        {
            phonePoseWarningPanel.SetActive(false);
            trackingLostAlertPanel.SetActive(false);

            //AR tracking alert is always on, not like phone pose detection.
            // ARStatusManager.Instance.OnTrackingLostAlert += OnARTrackingLostAlert;
            // ARStatusManager.Instance.OnTrackingRecovered += OnARTrackingRecovered;
            ARHelper.Instance.OnTrackingLost += OnARTrackingLost;
            ARHelper.Instance.OnTrackingResumed += OnARTrackingResumed;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!isEnabled)
                return;
            
            //Input.gyro.gravity.y is a value between -1 - 0. it's -1 when the phone is vertical,
            //0 when it's horizontal (e.g. when phone is on table)
            if (Input.gyro.gravity.y >= -1 * Const.PhonePoseAngle / 100)
            {
                // if(trackingLostAlertPanel.activeSelf)
                //     trackingLostAlertPanel.SetActive(false);
                phonePoseWarningPanel.SetActive(true);
            }
            else
            {
                phonePoseWarningPanel.SetActive(false);
            }
        }


        private void OnDestroy()
        {
            // ARStatusManager.Instance.OnTrackingLostAlert -= OnARTrackingLostAlert;
            // ARStatusManager.Instance.OnTrackingRecovered -= OnARTrackingRecovered;
            ARHelper.Instance.OnTrackingLost -= OnARTrackingLost;
            ARHelper.Instance.OnTrackingResumed -= OnARTrackingResumed;
        }


        // private void OnARTrackingLostAlert(string trackingLostReason, string trackingLostReasonDesc)
        // {          
        //     trackingLostAlertPanel.SetActive(true);
        //     
        //     CenterPanel cp = trackingLostAlertPanel.GetComponent<CenterPanel>();
        //     cp.title.text = trackingLostReason;
        //     cp.desc.text = trackingLostReasonDesc;
        // }
        //
        //
        // private void OnARTrackingRecovered()
        // {
        //     CenterPanel cp = trackingLostAlertPanel.GetComponent<CenterPanel>();
        //     cp.title.text = "";
        //     cp.desc.text = "";
        //     
        //     trackingLostAlertPanel.SetActive(false);
        // }
        
        private void OnARTrackingResumed()
        {
            CenterPanel cp = trackingLostAlertPanel.GetComponent<CenterPanel>();
            cp.title.text = "";
            cp.desc.text = "";
            
            trackingLostAlertPanel.SetActive(false);
        }

        private void OnARTrackingLost()
        {
            trackingLostAlertPanel.SetActive(true);
            
            CenterPanel cp = trackingLostAlertPanel.GetComponent<CenterPanel>();
            cp.title.text = "Tracking is lost"; //trackingLostReason;
            cp.desc.text = "Please wait while your device recovers from the lost tracking."; //trackingLostReasonDesc;
        }


        public void EnablePoseWarning(bool enabled)
        {
            if (isEnabled == enabled)
                return;

            isEnabled = enabled;

            if (!enabled)
            {
                phonePoseWarningPanel.SetActive(false);
            }
        }

        public void ResetAlertPanels()
        {
            phonePoseWarningPanel.SetActive(false);
            trackingLostAlertPanel.SetActive(false);
        }
    }

}
