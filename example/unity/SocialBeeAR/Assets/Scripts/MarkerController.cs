using System;
using System.Collections;
using Elasticsearch.Net.Specification.MachineLearningApi;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MarkerController : MonoBehaviour
{
    [SerializeField] private GameObject textPanel;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text distanceUnitText;
    [SerializeField] private GameObject pinObj;
    [SerializeField] private GameObject innerObj;

    [SerializeField] private Material transparentMat;
    [SerializeField] private Material opaqueMat;
    [SerializeField] private Animator idleAnimation;
    

    private bool watchingPlayer = true;
    private Vector3 lookAtTargetPos;

    private float fadeTime = 0.75f;
    
    private Distance4Display distance4Display = new Distance4Display();
    
    //the related anchor
    private AnchorController anchorController;
    public void SetAnchorController(AnchorController controller)
    {
        this.anchorController = controller;
    }


    void Update()
    {
        if(watchingPlayer)
        {
            WatchPlayer();
        }

        UpdateDistance();
    }


    public void SetWatchPlayer(bool watchPlayer)
    {
        this.watchingPlayer = watchPlayer;
    }
    
    
    private void WatchPlayer()
    {
        lookAtTargetPos.Set(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);
        transform.LookAt(lookAtTargetPos);
    }

    
    public void UpdateDistance()
    {
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        
        // Utilities.TrimDistanceNumber4DisplayYardMiles(distance, distance4Display);
        this.distanceText.text = this.distance4Display.value;
        this.distanceUnitText.text = this.distance4Display.unit;
    }
    
    
    public void MarkerOff(Action postAction)
    {
        this.innerObj.SetActive(false);
        this.textPanel.SetActive(false);
        this.pinObj.SetActive(false);
        
        GetComponentInChildren<PulseController>().StopPulseAnimation();
        
        postAction.Invoke();
    }

    public void MarkerOn(Action postAction)
    {
        this.innerObj.SetActive(true);
        this.textPanel.SetActive(true);
        this.pinObj.SetActive(true);
        
        pinObj.GetComponent<MeshRenderer>().material = this.opaqueMat;
        GetComponentInChildren<PulseController>().StartPulseAnimation();
        
        postAction.Invoke();
    }
    

    private IEnumerator fadingProcess;
    public void MarkerFadeOff(Action postAction)
    {
        this.innerObj.SetActive(false);
        this.textPanel.SetActive(false);
        //this.pinObj.SetActive(false);

        if (fadingProcess != null)
        {
            StopCoroutine(fadingProcess);
            //return;
        }
        
        //stop pulsing animation on the bottom
        GetComponentInChildren<PulseController>().StopPulseAnimation();
        
        fadingProcess = FadeOffPinCoroutine(postAction);
        StartCoroutine(fadingProcess);
    }
    
    
    public void MarkerFadeOn(Action postAction)
    {
        this.innerObj.SetActive(true);
        this.textPanel.SetActive(true);
        //this.pinObj.SetActive(true);
        
        if (fadingProcess != null)
        {
            StopCoroutine(fadingProcess);
            //return;
        }
        
        fadingProcess = FadeOnPinCoroutine(() =>
        {
            GetComponentInChildren<PulseController>().StartPulseAnimation();
            postAction.Invoke();
        });
        StartCoroutine(fadingProcess);
    }
    

    IEnumerator FadeOffPinCoroutine(Action postAction)
    {
        yield return new WaitForSeconds(0.1f);

        pinObj.GetComponent<MeshRenderer>().material = this.transparentMat;
        
        while (pinObj.GetComponent<MeshRenderer>().material.color.a > 0)
        {
            float newAlpha = pinObj.GetComponent<MeshRenderer>().material.color.a - Time.deltaTime / fadeTime;
            pinObj.GetComponent<MeshRenderer>().material.color = new Color(
                pinObj.GetComponent<MeshRenderer>().material.color.r, 
                pinObj.GetComponent<MeshRenderer>().material.color.g, 
                pinObj.GetComponent<MeshRenderer>().material.color.b, newAlpha);
            yield return null;
        }

        pinObj.SetActive(false);
        fadingProcess = null;
        
        postAction.Invoke();
        yield return null;
    }
    
    
    IEnumerator FadeOnPinCoroutine(Action postAction)
    {
        pinObj.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        
        //start fading on
        while (pinObj.GetComponent<MeshRenderer>().material.color.a < 1)
        {
            float newAlpha = pinObj.GetComponent<MeshRenderer>().material.color.a + Time.deltaTime / fadeTime;
            pinObj.GetComponent<MeshRenderer>().material.color = new Color(
                pinObj.GetComponent<MeshRenderer>().material.color.r, 
                pinObj.GetComponent<MeshRenderer>().material.color.g, 
                pinObj.GetComponent<MeshRenderer>().material.color.b, newAlpha);
            yield return null;
        }

        pinObj.GetComponent<MeshRenderer>().material = this.opaqueMat;
        fadingProcess = null;
        
        postAction.Invoke();
        yield return null;
    }


    public void SetPinVisible(bool visible)
    {
        this.pinObj.SetActive(visible);
    }
    
    
    public void CallAnchor()
    {
        MessageManager.Instance.DebugMessage("Tapped the pulse indicator on Marker");

        print($"marker pos: [{gameObject.transform.position}], anchor pos: [{this.anchorController.gameObject.transform.position}]");
        this.anchorController.gameObject.transform.position = gameObject.transform.position; //put anchor the same position as marker
        
        if (this.anchorController != null)
        {
            MessageManager.Instance.DebugMessage("OnCall...");
            this.anchorController.OnCall();
            this.anchorController.SetAnchorMode(AnchorController.AnchorMode.Anchor, true);
        }
    }


    public void EnableIdleAnimation(bool toEnable)
    {
        if (toEnable)
        {
            idleAnimation.enabled = true;
        }
        else
        {
            idleAnimation.enabled = false;
        }
    }

}
