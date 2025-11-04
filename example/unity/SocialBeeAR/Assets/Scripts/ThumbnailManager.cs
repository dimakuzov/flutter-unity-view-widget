using UnityEngine;
using UnityEngine.UI;

namespace SocialBeeAR
{

    public class ThumbnailManager : BaseSingletonClass<ThumbnailManager>
    {
        private Texture2D pnCaptured;
        private Texture2D sbCaptured;
        private Texture2D finalCaptured;

        [SerializeField] private GameObject thumbnailPreviewParent;
        [SerializeField] private RawImage pnThumbnailImage;
        [SerializeField] private RawImage finalThumbnailImage;

        [SerializeField] private bool enableDebug;


        private void Start()
        {
            // #placenote2lightship 
            // PNThumbnailSelector.Instance.ReportTexture += OnGetPNThumbnail;
            SBThumbnailSelector.Instance.ReportTexture += OnGetSBThumbnail;
        }


        private void OnDestroy()
        {
            // #placenote2lightship  
            // PNThumbnailSelector.Instance.ReportTexture -= OnGetPNThumbnail;
            SBThumbnailSelector.Instance.ReportTexture -= OnGetSBThumbnail;
        }


        public void StartCapturing()
        {
            pnCaptured = null;
            sbCaptured = null;
            finalCaptured = null;

            pnThumbnailImage.texture = null;
            finalThumbnailImage.texture = null;

            if(enableDebug)
                thumbnailPreviewParent.SetActive(true);
            SBThumbnailSelector.Instance.Reset();
        }


        public string EndCapturing(string anchorId)
        {
            finalCaptured = pnCaptured != null ? pnCaptured : sbCaptured;
            string fromWhere = pnCaptured != null ? "PN" : "SB";

            //update thumbnail debug UI
            Utilities.UpdateDebugThumbnail(finalThumbnailImage, finalCaptured);

            // Write to PNG file, use anchorId as the file name
            string savedFilePath = Utilities.Texture2DToPNGFile(finalCaptured, anchorId);
            MessageManager.Instance.DebugMessage($"Thumbnail from {fromWhere} for anchor {anchorId} saved to {savedFilePath}");

            return savedFilePath;
            //return null;
        }


        public void Reset()
        {
            if(enableDebug)
                thumbnailPreviewParent.SetActive(false);
        }


        private void OnGetPNThumbnail(Texture2D thumbnailTexture)
        {            
            pnCaptured = thumbnailTexture;

            ShowThumbnailHelper(thumbnailTexture);            
        }


        private void OnGetSBThumbnail(Texture2D thumbnailTexture)
        {            
            sbCaptured = thumbnailTexture;

            ShowThumbnailHelper(thumbnailTexture);
        }

        void ShowThumbnailHelper(Texture2D thumbnailTexture)
        {            
            //update thumbnail UI
            if (ActivityUIFacade.Instance.thumbnailImage != null && thumbnailTexture != null)
            {
                ActivityUIFacade.Instance.UpdateThumbnail(thumbnailTexture);
            }
            
            //update thumbnail debug UI
            if(enableDebug)
                Utilities.UpdateDebugThumbnail(pnThumbnailImage, pnCaptured);
        }
    }

}