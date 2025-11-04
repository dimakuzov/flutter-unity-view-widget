using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;



namespace SocialBeeAR
{

    public class SBThumbnailSelector : BaseSingletonClass<SBThumbnailSelector>
    {
        // [SerializeField] private ARCameraManager cameraManager;
        // [SerializeField] private ARCameraBackground arCameraBackground;
        // [SerializeField] private Camera arCamera;
        private RenderTexture renderTexture;

        //The number of thumbnailImageList should equals to the length of scoreList and segStart, segEnd
        //which represents the time of shots to take, which is according to how many thumbnailImageList is
        //defined in Unity inspector
        [SerializeField] private List<RawImage> thumbnailImageList;


        private float headSeg = 0.15f; //the first 15% is definded as the 'head', to be execuluted from the segment list.
        private List<float> segStart = new List<float>();
        private List<float> segEnd = new List<float>();
        private bool headShotDone = false;
        private List<bool> segDone = new List<bool>();

        //captured & final selected
        private List<float> scoreList = new List<float>();
        private List<Texture2D> capturedTextureList = new List<Texture2D>();
        private int bestScoreIndex = -1;


        /// <summary>
        /// Get accessor for an event that returns the latest captured localization thumbnail
        /// </summary>
        /// <value>Event that returns the latest captured localization thumbnail</value>
        public Action<Texture2D> ReportTexture { get => reportTexture; set => reportTexture = value; }
        private Action<Texture2D> reportTexture = (texture) =>
        {
            Debug.Log("Got new thumbnail texture");
        };


        private void Start()
        {
            //prepare the renderTexture(width, height, colorDepth) according to device's resolution. e.g.
            //iPhone 12 pro: 2532 x 1170
            //iPad pro 2020: 2388 x 1668
            //renderTexture = new RenderTexture(Screen.width / 5, Screen.height / 5, 24);
            renderTexture = new RenderTexture(Screen.width / Const.THUMBNAIL_SCALE,
                Screen.height / Const.THUMBNAIL_SCALE, Const.THUMBNAIL_SCALE, RenderTextureFormat.ARGB32);

            //splitting for the shooting 'segments'
            float segLength = (1 - headSeg) / thumbnailImageList.Count;
            for (int i = 0; i < thumbnailImageList.Count; i++)
            {
                segStart.Add(headSeg + i * segLength);
                segEnd.Add(headSeg + Mathf.Max((i + 1) * segLength - 1, segStart[i]));
            }

            //reset flags
            //PrepareFlags();
        }


        //--------------------- Capture image - Approach 1: Retrieving from ARCamerBackground -------------------------

        public void Reset()
        {
            headShotDone = false;
            PrepareFlags();

            for(int i = 0; i < thumbnailImageList.Count; i ++)
            {
                thumbnailImageList[i].texture = null;
                thumbnailImageList[i].GetComponentInChildren<Image>().color = Color.white;
                thumbnailImageList[i].GetComponentInChildren<Text>().text = "";
            }
        }


        private void PrepareFlags()
        {
            //reset lists for what's captured
            updateThumbnailCounter = 0;

            capturedTextureList.Clear();
            scoreList.Clear();
            bestScoreIndex = -1;

            //reset the shot flag list
            segDone.Clear();
            for (int i = 0; i < thumbnailImageList.Count; i++)
            {
                segDone.Add(false);
            }
        }


        public void Capture(float progress)
        {
            if (!headShotDone && progress >= 0f && progress < headSeg) //for the first 10%, take the 1st shot(which is going to be dropped), it doesn't count.
            {
                GetImageAndScore(); //the first image will be dropped

                //reset counting flags
                headShotDone = true;
                MessageManager.Instance.DebugMessage(string.Format("SB thumnnail shot [{0}] done at '{1}%'", "PRE", progress));
            }

            //taking the real shots
            if(headShotDone)
            {
                for (int i = 0; i < thumbnailImageList.Count; i++)
                {
                    if (!segDone[i] && progress >= segStart[i] && progress <= segEnd[i])
                    {
                        GetImageAndScore();
                        segDone[i] = true;
                        MessageManager.Instance.DebugMessage(string.Format("SB thumnnail shot [{0}] done at '{1}%'", i, progress));
                    }
                }
            }
        }


        private void GetImageAndScore()
        {
            // We are not capturing thumbnail with Lightship.
            return; 
            
            // //copy the camera background to a RenderTexture
            // Graphics.Blit(null, renderTexture, arCameraBackground.material);
            //
            // //copy the RenderTexture from GPU to CPU
            // var activeRenderTexture = RenderTexture.active;
            // RenderTexture.active = renderTexture;
            // Texture2D lastCameraTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, true);
            // lastCameraTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            // lastCameraTexture.Apply();
            // RenderTexture.active = activeRenderTexture;
            //
            // //add to list if it's not the first one(which should be dropped)
            // if(headShotDone)
            //     capturedTextureList.Add(lastCameraTexture);
            //
            // // Write to file
            // //Utilities.Texture2DToPNGFile(lastCameraTexture, "camera_texture");
            //
            // //draw thumbnail in the debug panel
            // UpdateThumbnailAndCalScore(lastCameraTexture);
        }


        private int updateThumbnailCounter;
        private void UpdateThumbnailAndCalScore(Texture2D capturedTexture)
        {
            //Skip the first one as it's problematic
            if (!headShotDone)
            {
                return; 
            }

            if (updateThumbnailCounter < thumbnailImageList.Count)
            {
                //1. update thumbnail
                RawImage thumbnailImage = thumbnailImageList[updateThumbnailCounter];
                Utilities.UpdateDebugThumbnail(thumbnailImage, capturedTexture);

                //2. get scrore!
                float score = CalPointCloudScore();
                MessageManager.Instance.DebugMessage(string.Format("Image[{0}]: Score={1}", updateThumbnailCounter, score));
                scoreList.Add(score);
                thumbnailImage.GetComponentInChildren<Text>().text = score.ToString("F2");

                //3. when the last thumbnail is updated, get the best score!
                if (updateThumbnailCounter == thumbnailImageList.Count - 1)
                {
                    //select the winner!
                    bestScoreIndex = -1;
                    float bestScore = 0;
                    for(int i = 0; i < scoreList.Count; i ++)
                    {
                        if(scoreList[i] > bestScore)
                        {
                            bestScore = scoreList[i];
                            bestScoreIndex = i;
                        }
                    }

                    //report
                    Texture2D bestCaptured = capturedTextureList[bestScoreIndex];
                    ReportTexture(bestCaptured);

                    //show the best!
                    MessageManager.Instance.DebugMessage(string.Format("Best index={0}: Best Score={1}", bestScoreIndex, bestScore));
                    thumbnailImageList[bestScoreIndex].GetComponentInChildren<Image>().color = Color.yellow;
                }

                updateThumbnailCounter++;
            }
        }


        //--------------- calculate score based on the point-cloud ------------------


        public float CalPointCloudScore()
        {
            MessageManager.Instance.DebugMessage(string.Format("Calculating thumbnail score..."));

            return 0;
            // #placenote2lightship BEGIN
            // //1. prepare pointcloud
            // LibPlacenote.PNFeaturePointUnity[] map = LibPlacenote.Instance.GetMap();
            // if (map == null || map.Length == 0)
            // {
            //     MessageManager.Instance.DebugMessage(string.Format("Score = 0, since map is empty"));
            //     return 0;
            // }
            //
            // Vector3[] points = new Vector3[map.Length];
            // float[] weights = new float[map.Length];
            // for (int i = 0; i < map.Length; ++i)
            // {
            //     points[i].x = map[i].point.x;
            //     points[i].y = map[i].point.y;
            //     points[i].z = -map[i].point.z;
            //     weights[i] = 0.2f + 1.6f * (map[i].measCount / 10f);
            // }
            //
            // //2. calculate how many points are in the frustum
            // float score = 0;
            // for (int i = 0; i < points.Length; i++)
            // {
            //     if (IsPointInCameraFrustum(points[i]))
            //     {
            //         score += weights[i];
            //     }
            // }
            //
            // return score;
            // #placenote2lightship END
        }


        // private bool IsPointInCameraFrustum(Vector3 point)
        // {
        //     Plane[] planes = GeometryUtility.CalculateFrustumPlanes(arCamera);
        //
        //     for (int i = 0, iMax = planes.Length; i < iMax; ++i)
        //     {
        //         //check if the point is on the positive side of the plane(判断一个点是否在平面的正方向上)
        //         if (!planes[i].GetSide(point))
        //         {
        //             return false;
        //         }
        //     }
        //     return true;
        // }

        public void LoadInitialThumbnail(string url)
        {
            print($"thumbnail URL={url}");
            StartCoroutine(DownloadThumbnail(url));
        }

        IEnumerator DownloadThumbnail(string url)
        {
            using (var www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    print("Thumbnail download failed.");
                    MessageManager.Instance.DebugMessage("Thumbnail retrieving from SB server failed");
                }
                else
                {
                    print("Thumbnail download successful.");
                    MessageManager.Instance.DebugMessage("Thumbnail retrieving from SB server successfully");
                    Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    ReportTexture(texture);
                }
            }
        }


        //---------------------- Capture image - Approach 2: Retrieving CpuImage method (GPU->CPU) ----------------------

        //public void GetImageAsync()
        //{
        //    // Get information about the camera image
        //    XRCameraImage image;
        //    if (cameraManager.TryGetLatestImage(out image))
        //    {
        //        // If successful, launch a coroutine that waits for the image
        //        // to be ready, then apply it to a texture.
        //        StartCoroutine(ProcessImage(image));

        //        // It is safe to dispose the image before the async operation completes.
        //        image.Dispose();
        //    }
        //}


        //IEnumerator ProcessImage(XRCameraImage image)
        //{
        //    // Create the async conversion request
        //    var request = image.ConvertAsync(new XRCameraImageConversionParams
        //    {
        //        // Use the full image
        //        inputRect = new RectInt(0, 0, image.width, image.height),

        //        // Downsample by 2
        //        outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

        //        // Color image format
        //        outputFormat = TextureFormat.RGB24,

        //        // Flip across the Y axis
        //        transformation = CameraImageTransformation.MirrorY
        //    });

        //    // Wait for it to complete
        //    while (!request.status.IsDone())
        //        yield return null;

        //    // Check status to see if it completed successfully.
        //    if (request.status != AsyncCameraImageConversionStatus.Ready)
        //    {
        //        // Something went wrong
        //        Debug.LogErrorFormat("Request failed with status {0}", request.status);

        //        // Dispose even if there is an error.
        //        request.Dispose();
        //        yield break;
        //    }

        //    // Image data is ready. Let's apply it to a Texture2D.
        //    var rawData = request.GetData<byte>();

        //    // Create a texture if necessary
        //    Texture2D capturedTexture = new Texture2D(
        //        request.conversionParams.outputDimensions.x,
        //        request.conversionParams.outputDimensions.y,
        //        request.conversionParams.outputFormat,
        //        false);

        //    // Copy the image data into the texture
        //    capturedTexture.LoadRawTextureData(rawData);
        //    capturedTexture.Apply();

        //    //set to show the thumbnail!
        //    UpdateThumbnail(capturedTexture);

        //    // Need to dispose the request to delete resources associated
        //    // with the request, including the raw data.
        //    request.Dispose();
        //}


    }


}
