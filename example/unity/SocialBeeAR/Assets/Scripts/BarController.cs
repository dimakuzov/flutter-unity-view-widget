using SocialBeeAR;
using UnityEngine;


public class BarController : MonoBehaviour 
{

    public int band;
    public float startScale, scaleMultiplier;

    // Material material;
    private RectTransform tr;
    // float x;
    // float y;
    // float width;

    void Start() {
        // material = GetComponent<MeshRenderer>().materials[0];
        tr = GetComponent<RectTransform>();
        // x = tr.rect.x;
        // y = tr.rect.y;
        // width = tr.rect.width;
    }

    // private Vector3 newScale = Vector3.one;
	
    // Update is called once per frame
    void Update ()
    {
        if (AudioVisualization.Instance.isWorking && AudioVisualization._audioBandBuffer.Length >= band + 1)
        {
            float localScaleY = (AudioVisualization._audioBandBuffer[band]) * scaleMultiplier + startScale;
            if (!float.IsNaN(localScaleY)) {
                if (Mathf.Abs(localScaleY) > scaleMultiplier) {
                    tr.localScale = new Vector3(1, scaleMultiplier,1);
                }
                else {
                    tr.localScale = new Vector3(1, localScaleY,1);
                }
                // tr.rect.Set(x,y,width,localScaleY);
                
                // newScale.Set(tr.localScale.x, localScaleY, tr.localScale.z);

                // transform.localScale = newScale;

                // Color color = new Color(AudioVisualization._audioBandBuffer[band], AudioVisualization._audioBandBuffer[band], AudioVisualization._audioBandBuffer[band]);
                // material.SetColor("_EmissionColor",color);    
            }
        }
        else if(tr.localScale != Vector3.one){
            tr.localScale = Vector3.one;;
        }
    }
}