using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace SocialBeeAR
{
    public class PulseController : MonoBehaviour
    {
        //the pulse has 2 waves
        [SerializeField] private GameObject[] waves;
        [SerializeField] private GameObject[] waveEdges;

        private Image[] waveImages;
        private Vector3[] waveOriginScales;
        private Color[] waveOriginColors;
        
        private Image[] waveEdgeImages;
        private Vector3[] waveEdgeOriginScales;
        private Color[] waveEdgeOriginColors;

        [SerializeField] private Text distanceValueText;
        [SerializeField] private Text distanceUnitText;

        //parameters
        public float targetScale = 0.5f;
        public float transformationTime = 1.5f;

        private Coroutine pulseThread;
        private bool animationStarted;

        public bool isInteractable = true;
        
        //the related anchor
        private AnchorController anchorController;
        public void SetAnchorController(AnchorController controller)
        {
            this.anchorController = controller;
        }

        
        private void Awake()
        {
            //wave array
            waveImages = new Image[waves.Length];
            waveOriginScales = new Vector3[waves.Length];
            waveOriginColors = new Color[waves.Length];
            for (int i = 0; i < waves.Length; i++)
            {
                waveImages[i] = waves[i].GetComponent<Image>();

                waveOriginScales[i] = waves[i].transform.localScale;
                waveOriginColors[i] = waveImages[i].color;
            }
            
            //wave edges array
            waveEdgeImages = new Image[waveEdges.Length];
            waveEdgeOriginScales = new Vector3[waveEdges.Length];
            waveEdgeOriginColors = new Color[waveEdges.Length];
            for (int i = 0; i < waveEdges.Length; i++)
            {
                waveEdgeImages[i] = waveEdges[i].GetComponent<Image>();

                waveEdgeOriginScales[i] = waveEdges[i].transform.localScale;
                waveEdgeOriginColors[i] = waveEdgeImages[i].color;
            }
        }


        private void Start()
        {
            //SetVisible(false);
        }


        private void SetVisible(bool isVisible)
        {
            //wave array
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i].SetActive(isVisible);
            }
            
            //wave edges array
            for (int i = 0; i < waveEdges.Length; i++)
            {
                waveEdges[i].SetActive(isVisible);
            }
        }


        private void ResetPulse()
        {
            //wave array
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i].transform.localScale = waveOriginScales[i];
                waveImages[i].color = waveOriginColors[i];
            }
            
            //wave edges array
            for (int i = 0; i < waveEdges.Length; i++)
            {
                waveEdges[i].transform.localScale = waveEdgeOriginScales[i];
                waveEdgeImages[i].color = waveEdgeOriginColors[i];
            }
        }


        private void OnEnable()
        {
            // if (isInteractable)
            // {
            //     StartPulseAnimation();
            // }
        }
        
        private void OnDisable()
        {
            // if (isInteractable)
            // {
                StopPulseAnimation();
            // }
        }


        //------------------------------- pulse animation control ---------------------------------

        public void StartPulseAnimation()
        {
            if (animationStarted)
                return;

            StopPulseAnimation();
            
            SetVisible(true); ResetPulse();
            animationStarted = true;
            pulseThread = StartCoroutine(PulseAnimation());
        }


        public void StopPulseAnimation()
        {
            if (!animationStarted)
                return;
            
            animationStarted = false;
            if(pulseThread != null)
                StopCoroutine(pulseThread);

            ResetPulse();
            SetVisible(false);
        }
        
        
        IEnumerator PulseAnimation() 
        {
            MessageManager.Instance.DebugMessage("Start pulse animation...");
            while (animationStarted) 
            {
                for (float i = 0; i < 1; i += Time.deltaTime / transformationTime) 
                {
                    //wave1
                    float fScale = i * targetScale;
                    waves[0].transform.localScale = waveOriginScales[0] + new Vector3(fScale, fScale, fScale);
                    waveImages[0].color = new Color(waveImages[0].color.r, waveImages[0].color.g, waveImages[0].color.b, 1 - i);
                    
                    //wave edge1
                    if (waveEdges.Length >= 1 && waveEdges[0] != null)
                    {
                        waveEdges[0].transform.localScale = waveEdgeOriginScales[0] + new Vector3(fScale, fScale, fScale);
                        waveEdgeImages[0].color = new Color(waveEdgeImages[0].color.r, waveEdgeImages[0].color.g, waveEdgeImages[0].color.b, 1 - i);    
                    }

                    //wave2
                    if (waves.Length >= 2 && waves[1] != null)
                    {
                        fScale = fScale / targetScale + 0.5f;
                        if (fScale >= 1) //reset
                            fScale -= 1.0f;
                        fScale *= targetScale;
                        
                        waves[1].transform.localScale = waveOriginScales[1] + new Vector3(fScale, fScale, fScale);
                        waveImages[1].color = new Color(waveImages[1].color.r, waveImages[1].color.g, waveImages[1].color.b, 1 - fScale / targetScale);    
                    }

                    //wave edge2
                    if (waveEdges.Length >= 2 && waveEdges[1] != null)
                    {
                        waveEdges[1].transform.localScale = waveEdgeOriginScales[1] + new Vector3(fScale, fScale, fScale);
                        waveEdgeImages[1].color = new Color(waveEdgeImages[1].color.r, waveEdgeImages[1].color.g, waveEdgeImages[1].color.b, 1 - fScale / targetScale);    
                    }

                    yield return null;
                }
            }
            MessageManager.Instance.DebugMessage("End pulse animation...");
        }


        public void UpdateAlphaAccordingToDistance(float newAlpha)
        {
            CanvasGroup[] cgArr = gameObject.GetComponentsInChildren<CanvasGroup>();
            for (int i = 0; i < cgArr.Length; i++)
            {
                cgArr[i].alpha = newAlpha;
            }
        }
        
        
        //------------------------------- set distance value ---------------------------------
        
        /// <summary>
        /// Struct for displaying the distance number and unit.
        /// </summary>
        private Distance4Display distance4Display = new Distance4Display();
        
        
        public void SetDistanceValue(float value)
        {
            Utilities.TrimDistanceNumber4DisplayYardMiles(value, distance4Display);
            this.distanceValueText.text = this.distance4Display.value;
            this.distanceUnitText.text = this.distance4Display.unit;
        }

        //------------------------------- tapping the icon ---------------------------------

        public void CallAnchor()
        {
            /////////////////////////// according to Martin, we don't make any interaction for pulsing indicator
            MessageManager.Instance.DebugMessage("Tap the pulse indicator");
            
            if (isInteractable)
            {
                if (this.anchorController != null)
                {
                    print("OnCall...");
                    // this.anchorController.SwapToMarker(false, false, () =>
                    // {
                    //     this.anchorController.OnCall();    
                    // });
                    this.anchorController.gameObject.SetActive(true);
                    this.anchorController.OnCall();
                }    
            }
            else
            {
                //play audio
                AudioManager.Instance.PlayAudio(AudioManager.AudioOption.Wrong);
            }
        }
        
        //--------------------------- enable color update for consumed anchors ---------------------------


        [SerializeField] private Image[] imageArr;
        [SerializeField] private Color completedColor;
        public bool isCompleted = false;

        public void SetCompletedStyle()
        {
            for (int i = 0; i < imageArr.Length; i++)
            {
                if (imageArr[i] != null)
                    imageArr[i].color = completedColor;
            }

            isCompleted = true;
        }

    }
}


