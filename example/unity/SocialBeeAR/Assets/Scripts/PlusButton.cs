using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlusButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject[] newButtons;
    [SerializeField] private GameObject finishButton;
    [SerializeField] private GameObject cancelButton;
    [SerializeField] private Transform root;
    
    
    private Vector3 invisiblePos { get; set; }
    private Vector3[] visiblePos;
    
    
    void Start() {
        visiblePos = new Vector3[newButtons.Length];
        for (int i = 0; i < newButtons.Length; i++) {
            visiblePos[i] = newButtons[i].transform.position;
        }
        invisiblePos = transform.position;
        
        // --- Set Init Positions
        for (int i = 0; i < newButtons.Length; i++) {
            newButtons[i].transform.position = invisiblePos;
        }
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        SetVisible(true, () => { });
    }
    
    public void SetVisible(bool visible) {
        SetVisible(visible, () => { });
    }
    
    void SetVisible(bool showOptions, Action postAction = null) {
        float rootPos = root.transform.position.y;
        print($"-=- PlusButton SetVisible = {showOptions}, rootPos = {rootPos}");
        
        if (showOptions) {
            for (int i = 0; i < newButtons.Length; i++) {
                newButtons[i].transform.position = invisiblePos + new Vector3(0, rootPos, 0);
                newButtons[i].transform.DOMove(visiblePos[i] + new Vector3(0, rootPos, 0), 0.5f).SetEase(Ease.OutQuint)
                    .OnComplete(() => { postAction?.Invoke(); });
            }

            ShowOptions();
        }
        else {
            for (int i = 0; i < newButtons.Length; i++) {
                newButtons[i].transform.DOMove(invisiblePos + new Vector3(0, rootPos, 0), 0.5f).SetEase(Ease.OutQuint).OnComplete(() => {
                    postAction?.Invoke();
                    HideOptions();
                });
            }
        }
    }

    void ShowOptions() {
        cancelButton.SetActive(true);
        finishButton.SetActive(false);
    }

    void HideOptions() {
        cancelButton.SetActive(false);
        finishButton.SetActive(true);
    }

}
