using System;
using System.Collections.Generic;
using UnityEngine;

public class NavigationPoint : MonoBehaviour
{
    public int id;
    public string type; // "target", "path"
    [HideInInspector]public Vector3 position;
    public List<int> neighborIds;
    [HideInInspector]public int nextPointId;// if point not set: -1
    [HideInInspector]public Vector3 lastPointPos;
    
    // --- This is id of activity in activity collection
    public string activityId;

    public void SetLastPointDirection() {
        float distance = Vector3.Distance(transform.position, lastPointPos);
        
        // --- Calculate target position
        lastPointPos -= transform.position;
        Vector3 rotateAngle = new Vector3(0, Math.Abs(transform.localRotation.eulerAngles.y),0);
        rotateAngle.y = 0 - rotateAngle.y;
        lastPointPos = Quaternion.Euler(rotateAngle) * lastPointPos;
    }
}



