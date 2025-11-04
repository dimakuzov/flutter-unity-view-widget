using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIContentClamper : MonoBehaviour
{
    private float minX;
    private float maxX;

    private void LateUpdate()
    {
        Vector3 pos = GetComponent<RectTransform>().localPosition;

        if (pos.x < -maxX)
        {
            GetComponent<RectTransform>().localPosition = new Vector3(-maxX, pos.y, pos.z);
        }
        else if (pos.x > minX)
        {
            GetComponent<RectTransform>().localPosition = new Vector3(minX, pos.y, pos.z);
        }
    }

    public void Initialize(float elementWidth, float size)
    {
        minX = elementWidth * 0.5f;
        maxX = size - minX + 2 * elementWidth;
        Vector3 pos = GetComponent<RectTransform>().localPosition;
        pos.x = minX;
        GetComponent<RectTransform>().localPosition = pos;
    }
}
