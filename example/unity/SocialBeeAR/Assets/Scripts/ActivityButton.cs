using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ActivityButton : MonoBehaviour
{
    
    public GameObject enabledObj;
    public GameObject disabledObj;
    public GameObject existingObj;
    
    private UIMode currentModel = UIMode.Undefined;

    public enum UIMode
    {
        Undefined,
        Disabled,
        Enabled
    }

    public void Start()
    {
        SetEnabled(true);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            currentModel = UIMode.Enabled;
            enabledObj.SetActive(true);
            disabledObj.SetActive(false);
        }
        else
        {
            currentModel = UIMode.Disabled;
            enabledObj.SetActive(false);
            disabledObj.SetActive(true);
        }
    }

    public void SetExisting(bool isExist)
    {
        if (!existingObj) return;
        print($"-=- ActivityButton SetExisting isExist = {isExist}");
        existingObj.SetActive(isExist);
    }

}
