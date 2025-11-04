using UnityEngine;

public class SingletonForAnimatonEvents : MonoBehaviour {
    [SerializeField] private RectTransform botPosition;
    public GameObject background;

    private void OnEnable() {
        if (background != null) {
            background.SetActive(true);
        }
    }
    
    private void OnDisable() {
        if (background != null) {
            background.SetActive(false);
        }
    }

    public Vector3 GetBotPosition() {
        if(botPosition != null) {
            return new Vector3(botPosition.position.x, botPosition.position.y, 0);
        }
        else {
            Debug.Log("-=- botPosition = null");
            return Vector3.zero;
        }
    }
    
    public void ZoomOutAnimationEnd()
    {
        gameObject.SetActive(false);
        //transform.GetComponent<Animator>().SetBool("PanelZoomOut", true);
    }
}
