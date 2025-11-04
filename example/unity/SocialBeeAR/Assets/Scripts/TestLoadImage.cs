using System.Collections;
using System.Collections.Generic;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;

public class TestLoadImage : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    private string imagePath = "file:///Users/maolin/workspaces/thumbnail-ChIJQ_oEiK1qkFQRg3bU4l3mmWs";
    
    //private string imagePath = "http://gyanendushekhar.com/wp-content/uploads/2017/07/SampleImage.png";


    private void Start()
    {
        GoLoadImage();
    }
    
    
    public void GoLoadImage()
    {
        if (rawImage == null)
        {
            Debug.Log("error!");
        }

        StartCoroutine(GoLoadImage(rawImage));
    }
    
    IEnumerator GoLoadImage (RawImage r) 
    {
        WWW www = new WWW (imagePath);
        while(!www.isDone)
            yield return null;

        Texture2D t = www.texture;
        r.texture = t;
        float newW = t.height * 180 / 225; //the ratio of the rawimage
        // Texture2D squareT = Utilities.ScaleTextureCutOut(t,(t.width - newW)/2, 0, newW, t.height);

        // r.texture = squareT;
    }

    
}



