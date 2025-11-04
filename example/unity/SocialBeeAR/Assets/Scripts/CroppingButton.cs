using System;
using System.Collections;
using System.Collections.Generic;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CroppingButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {

    [SerializeField] private RectTransform selectedSquare;
    [SerializeField] private ButtonsPoses buttonPos;
    

    private bool isChanging;
    private bool isMoving;
    private Vector2 currPos;
    private Vector2 previousPos;
    private float minSize;
    private Vector2 maxSize;
    private RectTransform rectOfButton;
    private float uiSizeToScreenSize;
    private bool pressed;
    
    enum ButtonsPoses {
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
        Center
    }
    
    void Start() {
        minSize = RecordManager.Instance.croppingMinSize.x;
        maxSize = new Vector2(Screen.width, Screen.height - 75);
        rectOfButton = gameObject.GetComponent<RectTransform>();
        print($"-=- maxSize = {maxSize}");
        // uiSizeToScreenSize = Screen.width / FindObjectOfType<CanvasScaler>().referenceResolution.x;
        uiSizeToScreenSize = Screen.width / 1125.0f;// 1125 - is CanvasScaler.referenceResolution.x 
    }

    private void Update() {
        if (isChanging) {
            Touch touch = Input.GetTouch(0);
            currPos = touch.position;
            if(currPos != previousPos) {
                if (isMoving) {
                    MoveSelectedArea();
                }
                else {
                    ChangeSelectedSquare();
                }
                previousPos = currPos;
            }
        }
    }

    private UnityEvent onTouchUp;
    
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
        // Start change selected square
        print("-=- CroppingButton IPointerDownHandler");
        isChanging = true;
        Touch touch = Input.GetTouch(0);
        previousPos = touch.position;
        if(Input.touchCount == 1) {
            SetAnchorInSelectedRectTransform();
        }

        StartCoroutine(FinishCropping());
    }
    
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
        // Finish change selected square
        print("-=- CroppingButton IPointerUpHandler");
        isChanging = false;
        pressed = false;
    }
    
    
    // ------------ changing selected area functions ------------

    void SetAnchorInSelectedRectTransform() {
        Vector2 newPivot = Vector2.zero;
        if (buttonPos == ButtonsPoses.UpLeft) {
            newPivot = new Vector2(1,0);
        }
        else if (buttonPos == ButtonsPoses.UpRight) {
            newPivot = new Vector2(0,0);
        }
        else if (buttonPos == ButtonsPoses.DownLeft) {
            newPivot = new Vector2(1,1);
        }
        else if (buttonPos == ButtonsPoses.DownRight) {
            newPivot = new Vector2(0,1);
        }
        else if (buttonPos == ButtonsPoses.Center) {
            newPivot = new Vector2(0.5f,0.5f);
            isMoving = true;
        }
        
        // --- apply new pivot
        Vector3 deltaPosition = selectedSquare.pivot - newPivot;    // get change in pivot
        deltaPosition.Scale(selectedSquare.rect.size);           // apply sizing
        deltaPosition.Scale(selectedSquare.localScale);          // apply scaling
        deltaPosition = selectedSquare.rotation * deltaPosition; // apply rotation
     
        selectedSquare.pivot = newPivot;                            // change the pivot
        selectedSquare.localPosition -= deltaPosition;           // reverse the position change
    }

    void ChangeSelectedSquare() {
        Vector2 changing = previousPos - currPos;
        
        // --- set direction to scale
        if (buttonPos == ButtonsPoses.UpLeft) {
            if (rectOfButton.position.x > 0 && rectOfButton.position.y < maxSize.y) {
                AddSize(new Vector2(changing.x, -changing.y), false);
            }
            else {
                AddSize(new Vector2(changing.x, -changing.y), true);
            }
        }
        
        else if (buttonPos == ButtonsPoses.UpRight) {
            if (rectOfButton.position.x < maxSize.x && rectOfButton.position.y < maxSize.y) {
                AddSize(new Vector2(-changing.x, -changing.y), false);
            }
            else {
                AddSize(new Vector2(-changing.x, -changing.y), true);
            }
        }
        
        else if (buttonPos == ButtonsPoses.DownLeft) {
            if (rectOfButton.position.x > 0 && rectOfButton.position.y > 0) {
                AddSize(new Vector2(changing.x, changing.y), false);
            }
            else {
                AddSize(new Vector2(changing.x, changing.y), true);
            }
        }
        
        else if (buttonPos == ButtonsPoses.DownRight) {
            if (rectOfButton.position.x < maxSize.x && rectOfButton.position.y > 0) {
                AddSize(new Vector2(-changing.x, changing.y), false);
            }
            else {
                AddSize(new Vector2(-changing.x, changing.y), true);
            }
        }
    }

    void AddSize(Vector2 newSquare, bool isBorder) {
        print($"-=- AddSize original newSquare = {newSquare}");
        newSquare *= new Vector2(1.4f, 1.4f);
        newSquare += selectedSquare.sizeDelta;
        
        // --- make a square
        if (newSquare.x > newSquare.y && Math.Abs(selectedSquare.sizeDelta.y) < Math.Abs(selectedSquare.sizeDelta.x)/2) {
            newSquare.y = newSquare.x;
        }     
        else if (Math.Abs(selectedSquare.sizeDelta.x) < Math.Abs(selectedSquare.sizeDelta.y) / 2) {
            newSquare.x = newSquare.y;
        }
        else if(newSquare.x < newSquare.y) {
            newSquare.y = newSquare.x;
        }
        else {
            newSquare.x = newSquare.y;
        }
        
        // --- check if selected area less then minimum
        if (newSquare.x < minSize) {
            newSquare.x = minSize;
            newSquare.y = minSize;
        }
        
        // --- will change size only if it is smaller then was
        if (isBorder && newSquare.x > selectedSquare.sizeDelta.x) {
            AlignSquare();
            onTouchUp?.Invoke();
        }
        else {
            // --- apply size
            selectedSquare.sizeDelta = newSquare;
        }
        
    }

    void MoveSelectedArea() {
        Vector2 changing = currPos - previousPos;
        float size = (uiSizeToScreenSize * rectOfButton.sizeDelta.x)  / 2;
        
        if (rectOfButton.position.x + size > maxSize.x && changing.x > 0) {
            changing.x = 0;
        }
        if (rectOfButton.position.x - size < 0 && changing.x < 0) {
            changing.x = 0;
        }
        if (rectOfButton.position.y + size > maxSize.y && changing.y > 0) {
            changing.y = 0;
        }
        if (rectOfButton.position.y - size < 0 && changing.y < 0) {
            changing.y = 0;
        }

        if (changing.x == 0 && changing.y == 0) {
            return;
        }
        
        selectedSquare.Translate(new Vector3(changing.x, changing.y, 0));
        AlignSquare();
    }

    void AlignSquare() {
        float size = (uiSizeToScreenSize * selectedSquare.sizeDelta.x) / 2;
        Vector2 changingSize = Vector2.zero;

        if (selectedSquare.pivot.x != 0.5f) {
            Vector2 newPivot = Vector2.zero;
            newPivot = new Vector2(0.5f, 0.5f);

            // --- apply new pivot
            Vector3 deltaPosition = selectedSquare.pivot - newPivot; // get change in pivot
            deltaPosition.Scale(selectedSquare.rect.size); // apply sizing
            deltaPosition.Scale(selectedSquare.localScale); // apply scaling
            deltaPosition = selectedSquare.rotation * deltaPosition; // apply rotation

            selectedSquare.pivot = newPivot; // change the pivot
            selectedSquare.localPosition -= deltaPosition; // reverse the position change}
        }
        
        // --- moving selected area
        if (selectedSquare.position.x + size > maxSize.x) {
            selectedSquare.Translate(new Vector3(maxSize.x - (selectedSquare.position.x + size), 0, 0));
        }
        if (selectedSquare.position.x - size < 0) {
            selectedSquare.Translate(new Vector3(Mathf.Abs(selectedSquare.position.x - size), 0, 0));
        }
        if (selectedSquare.position.y + size > maxSize.y) {
            selectedSquare.Translate(new Vector3(0, maxSize.y - (selectedSquare.position.y + size), 0));
        }
        if (selectedSquare.position.y - size < 0) {
            selectedSquare.Translate(new Vector3(0, Mathf.Abs(selectedSquare.position.y - size), 0));
        }

        // --- Scaling selected area if x more then screen width 
        if (selectedSquare.position.x + size > maxSize.x) {
            changingSize.x = (maxSize.x - (selectedSquare.position.x + size)) * 2;
        }
        if (selectedSquare.position.x - size < 0) {
            changingSize.x = (selectedSquare.position.x - size) * 2;
        }

        // --- make a square
        if (changingSize.x < changingSize.y) {
            changingSize.y = changingSize.x;
        }
        else {
            changingSize.x = changingSize.y;
        }
        print($"-=- AlignSquare() changingSize = {changingSize}");
        changingSize += selectedSquare.sizeDelta;
        selectedSquare.sizeDelta = changingSize;
    }

    IEnumerator FinishCropping() {
        pressed = true;
        yield return new WaitForSeconds(0.2f);
        if (!pressed) {
            RecordManager.Instance.HideCroppingPage();
            yield break;
        }
        yield return null;
    }
    
    
    // ---------------- Data for AR Panel ---------------
    
    public Vector3 CroppingData() {
        Vector2 newPivot = new Vector2(0.5f,0.5f);
        // --- apply new pivot
        Vector3 deltaPosition = selectedSquare.pivot - newPivot;    // get change in pivot
        deltaPosition.Scale(selectedSquare.rect.size);           // apply sizing
        deltaPosition.Scale(selectedSquare.localScale);          // apply scaling
        deltaPosition = selectedSquare.rotation * deltaPosition; // apply rotation
     
        selectedSquare.pivot = newPivot;                            // change the pivot
        selectedSquare.localPosition -= deltaPosition;           // reverse the position change
        print($"rectOfButton.sizeDelta.x = {rectOfButton.sizeDelta.x}");
        print($"Screen.width = {Screen.width}");
        print($"maxSize.x = {maxSize.x}");
        print($"uiSizeToScreenSize = {uiSizeToScreenSize}");
        float scale = maxSize.x / (uiSizeToScreenSize * rectOfButton.sizeDelta.x);
        float xNormPos = selectedSquare.position.x / Screen.width;
        float yNormPos = selectedSquare.position.y / Screen.height;
        return new Vector3(xNormPos, yNormPos, scale);
    }

}
