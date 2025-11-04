using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhotoFilters
{
    public class PostEffectController : MonoBehaviour
    {
        public static PostEffectController Instance { get; private set; }

        [SerializeField] private Material combineMaterial;
        [SerializeField] private Camera cameraUI;

        private bool shouldTakeScreenshot;
        private Material postProcessMaterial;
        private bool isOriginal;
        RenderTexture tempTexture;

        private void Awake()
        {
            Instance = this;
            RenderTexture uiTexture = new RenderTexture(Screen.width, Screen.height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
            tempTexture = new RenderTexture(uiTexture);
            cameraUI.targetTexture = uiTexture;
            isOriginal = true;
            shouldTakeScreenshot = false;
        }

        public void TakeScreenshot()
        {
            shouldTakeScreenshot = true;
        }

        public void SetMaterial(Material material)
        {
            this.postProcessMaterial = material;
            isOriginal = false;
        }

        public void ShowOriginal(bool isOriginal)
        {
            this.isOriginal = isOriginal;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            combineMaterial.SetTexture("_OriginalTexture", source);
            combineMaterial.SetTexture("_UITexture", cameraUI.targetTexture);

            if (!isOriginal)
            {
                Graphics.Blit(source, tempTexture, postProcessMaterial);
                SaveTexture(tempTexture);
                Graphics.Blit(tempTexture, destination, combineMaterial);
            }
            else
            {
                SaveTexture(source);
                Graphics.Blit(source, destination, combineMaterial);
            }
        }

        public void ShowComprasion(bool show)
        {
            combineMaterial.SetFloat("_EnableComparison", show ? 1 : 0);
        }


        public void SaveTexture(RenderTexture renderTexture)
        {
            if (!shouldTakeScreenshot)
            {
                return;
            }

            shouldTakeScreenshot = false;

            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.Destroy(tex);
            File.WriteAllBytes(Application.dataPath + "/../" + postProcessMaterial.name + ".png", bytes);
        }
    }
}
