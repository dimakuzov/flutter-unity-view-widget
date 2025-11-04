using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ARLocation;
using Newtonsoft.Json.Linq;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;

public class TestPulse : MonoBehaviour
{

    [SerializeField] private GameObject pulsePrefab;
    [SerializeField] private GameObject uiRoot;

    private GameObject pulseIndicator;
    
    // Start is called before the first frame update
    void Start()
    {
        this.pulseIndicator = Instantiate(pulsePrefab, uiRoot.transform);
        this.pulseIndicator.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ShowIndicator()
    {
        this.pulseIndicator.SetActive(true);
    }


    public void HideIndicator()
    {
        this.pulseIndicator.SetActive(false);
    }


    public void StartAnimation()
    {
        this.pulseIndicator.GetComponent<PulseController>().StartPulseAnimation();
        
        //TestDistanceValue();
    }
    
    
    public void StopAnimation()
    {
        // if(this.currPulse)
        //     this.currPulse.SetActive(false);
        
        this.pulseIndicator.GetComponent<PulseController>().StopPulseAnimation();
    }
    

    private void TestDistanceValue()
    {
        // float value = 99.49f;
        // TrimDistanceNumber4Display(value);
        // Debug.Log($"{value}: {this.displayValue.value} {this.displayValue.unit}");
        //
        // value = 8999.49f;
        // TrimDistanceNumber4Display(value);
        // Debug.Log($"{value}: {this.displayValue.value} {this.displayValue.unit}");
        //
        // value = 9999.99f;
        // TrimDistanceNumber4Display(value);
        // Debug.Log($"{value}: {this.displayValue.value} {this.displayValue.unit}");
        //
        // value = 1600000f;
        // TrimDistanceNumber4Display(value);
        // Debug.Log($"{value}: {this.displayValue.value} {this.displayValue.unit}");
        //
        // value = 1609344f;
        // TrimDistanceNumber4Display(value);
        // Debug.Log($"{value}: {this.displayValue.value} {this.displayValue.unit}");
        //
        // value = 999999934f;
        // TrimDistanceNumber4Display(value);
        // Debug.Log($"{value}: {this.displayValue.value} {this.displayValue.unit}");
    }

    
    /// <summary>
    /// Display rules
    /// 1) <1000 yd (>0 && <9144m): Unit 'yd', 1 digit. e.g. "999.9 yd"
    /// 2) >=1000 yd && <1000mi (>= 9144m && <1609344m): Unit 'mi', 1 digit. e.g. "999.9 mi"
    /// 3) >=1000mi (>= 1609344m): Just show ">1000 mi"
    /// </summary>
    /// <param name="valueMeter"></param>
    /// <returns></returns>
    private DisplayValue TrimDistanceNumber4Display(float valueMeter)
    {
        if (valueMeter > 0 && valueMeter < 9144f) //use unit 'yd'
        {
            double valueYard = valueMeter * (1 / 0.9144);
            this.displayValue.value = valueYard.ToString("F1");
            this.displayValue.unit = "yd";
        }
        else if (valueMeter >= 9144f && valueMeter < 1609344f) //use unit 'mi'
        {
            double valueMiles = valueMeter / 1000 * 0.621371;
            this.displayValue.value = valueMiles.ToString("F1");
            this.displayValue.unit = "mi";
        }
        else if (valueMeter >= 1609344f)
        {
            this.displayValue.value = ">1000";
            this.displayValue.unit = "mi";
        }

        return this.displayValue;
    }


    private DisplayValue displayValue = new DisplayValue();
    private struct DisplayValue
    {
        public string value;
        public string unit;
    }


    public void TestChangeColor()
    {
        this.pulseIndicator.GetComponent<PulseController>().SetCompletedStyle();
    }
    

}
