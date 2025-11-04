using System;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Serialization;

public class OffScreenIndicatorManager : BaseSingletonClass<OffScreenIndicatorManager>
{
    [SerializeField] private GameObject targetDetector;
    [FormerlySerializedAs("arrowUI")] [SerializeField] private GameObject indicatorUI;

    private Transform target;
    private bool toShow;
    

    private void Update() 
    {
        if (toShow) 
        {
            if(!BottomPanelManager.Instance.isActivePanel ||
               BottomPanelManager.Instance.isOffScreenPanel) 
            {
                ShowIndicator();
            }
        }
    }

    private void ShowIndicator() 
    {
        bool isNear = false;
        targetDetector.transform.LookAt(target); // align by z axis
        float degX = targetDetector.transform.localRotation.eulerAngles.x;
        float degY = targetDetector.transform.localRotation.eulerAngles.y;

        if (Math.Abs(degX) < 35 || Math.Abs(degX) > 325) {
            if (Math.Abs(degY) < 21 || Math.Abs(degY) > 339) {
                isNear = true;
                if (indicatorUI.activeSelf) {
                    indicatorUI.SetActive(false);
                    BottomPanelManager.Instance.HideCurrentPanel(() => { CheckButtonPanel(); });
                }
            }
        }
        if(!isNear) {
            if (!indicatorUI.activeSelf) {
                indicatorUI.SetActive(true);
                BottomPanelManager.Instance.ShowMessagePanel("Move your device", false);
                BottomPanelManager.Instance.isOffScreenPanel = true;
            }

            float x = (float) Math.Sin(degX * Mathf.Deg2Rad);
            float y = (float) Math.Sin(degY * Mathf.Deg2Rad);
            float sign = (0 < x) ? 1.0f : -1.0f;
            float zRotateAngle = Vector2.Angle(new Vector2(x, y), Vector2.down) * sign;
            indicatorUI.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, zRotateAngle));

            //set indicator's position
            Vector3 indicatorPos = CalculateIndicatorPosition(zRotateAngle);
            indicatorUI.transform.localPosition = indicatorPos;
        }
    }


    private Vector3 CalculateIndicatorPosition(float zRotateAngle) 
    {
        // Get this value from CanvasScaler
        // P.s. CanvasScaler get 0 when we get value from Start method.
        // int screenHeight = 2436; 
        // int screenWidth = 1125;
        int screenHeight = Screen.height;
        int screenWidth = Screen.width;

        zRotateAngle = zRotateAngle * -1;
        double ZRotateAngleRad = zRotateAngle * Mathf.Deg2Rad;
        double tanAngle = Math.Tan(ZRotateAngleRad);

        double tanDiagonal = (screenHeight * 0.5) / (screenWidth * 0.5);
        double diagonalAngle = Math.Atan(tanDiagonal) * Mathf.Rad2Deg;

        double xOnScreen = 0;
        double yOnScreen = 0;
        float backOffset = 100; //100, //to make the indicator fully visible (instead of 50% visible)
        float extraBackOffForTop = 250; //this is top banner's height

        float yOnScreenMax = screenHeight / 2.0f - (backOffset + extraBackOffForTop);
        float yOnScreenMin = -1 * screenHeight / 2.0f + backOffset;

        if (zRotateAngle > -1 * diagonalAngle && zRotateAngle < diagonalAngle)
        {
            //if it's in the left zone
            //MessageManager.Instance.DebugMessage(string.Format("currAngle=\'{0}\', diagonalAngle=\'{1}\', >>>>>>>>LEFT", zRotateAngle, diagonalAngle));

            xOnScreen = -1 * screenWidth / 2.0f + backOffset;
            yOnScreen = Mathf.Clamp((float)(0.5 * (float)(screenWidth * tanAngle)), yOnScreenMin, yOnScreenMax);
        }
        else if (zRotateAngle > (180 - diagonalAngle) || zRotateAngle < -1 * (180 - diagonalAngle))
        {
            //if it's in the right zone
            //MessageManager.Instance.DebugMessage(string.Format("currAngle=\'{0}\', diagonalAngle=\'{1}\', >>>>>>>>RIGHT", zRotateAngle, diagonalAngle));

            xOnScreen = screenWidth / 2.0f - backOffset;
            yOnScreen = Mathf.Clamp((float)(0.5 * screenWidth * tanAngle * -1), yOnScreenMin, yOnScreenMax) ;
        }
        else if (zRotateAngle >= diagonalAngle && zRotateAngle <= (180 - diagonalAngle))
        {
            //if it's in the top zone.
            //MessageManager.Instance.DebugMessage(string.Format("currAngle=\'{0}\', diagonalAngle=\'{1}\', >>>>>>>>TOP", zRotateAngle, diagonalAngle));

            xOnScreen = screenHeight / 2.0f / tanAngle * -1;
            yOnScreen = yOnScreenMax;
        }
        else if (zRotateAngle <= -1 * diagonalAngle && zRotateAngle >= -1 * (180 - diagonalAngle))
        {
            //if it's in the bottom zone.
            //MessageManager.Instance.DebugMessage(string.Format("currAngle=\'{0}\', diagonalAngle=\'{1}\', >>>>>>>>BOTTOM", zRotateAngle, diagonalAngle));

            xOnScreen = screenHeight / 2.0f / tanAngle;
            yOnScreen = yOnScreenMin;
        }

        return new Vector3((float)xOnScreen, (float)yOnScreen, 0);
    }


    void CheckButtonPanel() {
        float degX = Mathf.Asin(targetDetector.transform.localRotation.x) * Mathf.Rad2Deg * 2;
        float degY = Mathf.Asin(targetDetector.transform.localRotation.y) * Mathf.Rad2Deg * 2;

        if (Math.Abs(degX) > 35 || Math.Abs(degY) > 21) {
            BottomPanelManager.Instance.ShowMessagePanel("Move your device", false);
            BottomPanelManager.Instance.isOffScreenPanel = true;
        }
    }

    public void ShowArrow()
    {
        if(target != null) 
        {
            print($"-=- ArrowForMoveDeviceManager ShowArrow()");
            toShow = true;
            // indicatorUI.SetActive(true);
        }
        else {
            toShow = false;
            indicatorUI.SetActive(false);
            print($"-=- ArrowForMoveDeviceManager Need to Set Target");
        }
    }

    public void HideArrow() 
    {
        if(toShow) 
        {
            print($"-=- ArrowForMoveDeviceManager HideArrow()");
            toShow = false;
            indicatorUI.SetActive(false);
            BottomPanelManager.Instance.HideCurrentPanel(); //todo !!!
        }
    }

    public void SetTarget(Transform tr) 
    {
        if (tr != null) 
        {
            print($"-=- ArrowForMoveDeviceManager SetTarget()");
            target = tr;
        }
    }
}