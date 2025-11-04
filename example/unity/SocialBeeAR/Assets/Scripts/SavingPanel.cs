using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavingPanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Texture2D []frames;
    private int framesPerSecond = 10;
 
    // private void OnGUI() {
    //     int index = (int) (Time.time * framesPerSecond) % frames.Length;
    //     GetComponent<Renderer>().material.mainTexture = frames[index];
    // }
    
    
}
