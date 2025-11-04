using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


namespace SocialBeeAR
{

    public class Utilities
    {
        
        /// <summary>
        /// Generate anchor id.
        /// </summary>        
        /// <returns></returns>
        public static string GenerateAnchorId()
        {
            //Todo: call the native method here?
            return System.Guid.NewGuid().ToString("N");
        }
        

        /// <summary>
        /// Generate activity Id
        /// </summary>
        /// <returns></returns>
        public static string GenerateActivityId()
        {
            //Todo: call the native method here?
            return "" + new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds();
        }
        

        public static string GenerateMapId()
        {
            //Todo: call the native method here?
            return Const.MAP_PREFIX + "-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        }
        

        public static Dictionary<string, string> CopyActivitySpecificProperties(Dictionary<string, string> sourceDic)
        {
            Dictionary<string, string> targetDic = new Dictionary<string, string>();
            
            if (sourceDic != null)
            {
                foreach(string key in sourceDic.Keys)
                {
                    string value = sourceDic[key];
                    targetDic.Add(key, value);
                }
            }
            
            return targetDic;   
        }
        
        
        public static List<T> RandomSortList<T>(List<T> ListT)
        {
            System.Random random = new System.Random();
            List<T> newList = new List<T>();
            foreach (T item in ListT)
            {
                newList.Insert(random.Next(newList.Count + 1), item);
            }
            return newList;
        }


        public static List<string> CutOffStringToList(string inputStr, int maxLength)
        {
            if (maxLength <= 0)
                return null;
        
            List<string> resultList = new List<string>();
            while (inputStr.Length >= maxLength)
            {
                string takenOutStr = inputStr.Substring(0, maxLength);
                resultList.Add(takenOutStr);

                inputStr = inputStr.Substring(maxLength);
            }
            resultList.Add(inputStr); //adding the last one

            return resultList;
        }


        public static void SetCanvasGroupInteractable(GameObject parentObj, bool interactable)
        {
            if (parentObj == null)
                return;

            CanvasGroup[] canvasGroups = parentObj.GetComponentsInChildren<CanvasGroup>();
            foreach (var canvasGroup in canvasGroups)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = (interactable ? 1 : 0.65f);
                    canvasGroup.interactable = (interactable ? true : false);    
                }
            }
        }


        public static string ARTrackingStateToString(ARSessionState state)
        {
            string finalStr = "";
            switch (state)
            {
                case ARSessionState.None:
                    finalStr = "None";
                    break;
                case ARSessionState.CheckingAvailability:
                    finalStr = "Checking Availability";
                    break;
                case ARSessionState.Installing:
                    finalStr = "Installing";
                    break;
                case ARSessionState.NeedsInstall:
                    finalStr = "Needs Install";
                    break;
                case ARSessionState.Ready:
                    finalStr = "Ready";
                    break;
                case ARSessionState.SessionInitializing:
                    finalStr = "Session Initializing";
                    break;
                case ARSessionState.SessionTracking:
                    finalStr = "Session Tracking";
                    break;
                case ARSessionState.Unsupported:
                    finalStr = "Unsupported";
                    break;
            }

            return finalStr;
        }
        
        
        public static string ARTrackingLostReasonToString(NotTrackingReason reason)
        {
            string finalStr = "";
            switch (reason)
            {
                case NotTrackingReason.None:
                    finalStr = "None";
                    break;
                case NotTrackingReason.Initializing:
                    finalStr = "Initializing";
                    break;
                case NotTrackingReason.Relocalizing:
                    finalStr = "Relocalizing";
                    break;
                case NotTrackingReason.InsufficientLight:
                    finalStr = "Insufficient Light";
                    break;
                case NotTrackingReason.InsufficientFeatures:
                    finalStr = "Insufficient Features";
                    break;
                case NotTrackingReason.ExcessiveMotion:
                    finalStr = "Excessive Motion";
                    break;
                case NotTrackingReason.Unsupported:
                    finalStr = "Unsupported";
                    break;
            }
            
            return finalStr;
        }
        
        
        public static string ARTrackingLostReasonToDescString(NotTrackingReason reason)
        {
            string finalStr = "";
            switch (reason)
            {
                case NotTrackingReason.None:
                    finalStr = "None";
                    break;
                case NotTrackingReason.Initializing:
                    finalStr = "Initializing";
                    break;
                case NotTrackingReason.Relocalizing:
                    finalStr = "Relocalizing";
                    break;
                case NotTrackingReason.InsufficientLight:
                    finalStr = "Please make sure to have enough light.";
                    break;
                case NotTrackingReason.InsufficientFeatures:
                    finalStr = "Please make sure to point your phone to somewhere with rich enough feature.";
                    break;
                case NotTrackingReason.ExcessiveMotion:
                    finalStr = "Please move slower";
                    break;
                case NotTrackingReason.Unsupported:
                    finalStr = "Unsupported";
                    break;
            }
            
            return finalStr;
        }


        public static Vector3 RoundVector(Vector3 inputVector, int digit)
        {
            return new Vector3((float)Math.Round(inputVector.x, digit), (float)Math.Round(inputVector.y, digit),
                (float)Math.Round(inputVector.z, digit));
        }


        public static void UpdateDebugThumbnail(RawImage debugThumbnailImage, Texture2D capturedTexture)
        {
            //update the size of RawImage according to the size of the image
            RectTransform thumbnailImageRect = debugThumbnailImage.rectTransform;

            float width = capturedTexture.width * Const.THUMBNAIL_PREVIEW_SCALE;
            float height = capturedTexture.height * Const.THUMBNAIL_PREVIEW_SCALE;

            if (width > height)
            {
                float temp = width;
                width = height;
                height = temp;
            }

            thumbnailImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            thumbnailImageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            thumbnailImageRect.ForceUpdateRectTransforms();

            debugThumbnailImage.texture = capturedTexture;
            MessageManager.Instance.DebugMessage(string.Format("Thumbnail size = width:'{0}' x height:'{1}'", width, height));
        }


        public static List<int> FindDupIndex(List<string> optionList)
        {
            List<List<int>> dupIndexList = new List<List<int>>();
            for (int i = 0; i < optionList.Count; i++)
            {
                List<int> dupIndex = new List<int>();
                //check from previous elements
                for (int p = 0; p < i; p++)
                {
                    if (!String.IsNullOrWhiteSpace(optionList[i]) && optionList[p] == optionList[i])
                    {
                        dupIndex.Add(p);
                        if (!IsIndexExisting(dupIndex, i))
                            dupIndex.Add(i);
                    }
                }
                if (dupIndex.Count >= 2)
                    dupIndexList.Add(dupIndex);
            }

            //find the longest indexlist
            List<int> longestIndexList = new List<int>();
            for (int i = 0; i < dupIndexList.Count; i++)
            {
                if (dupIndexList[i].Count > longestIndexList.Count)
                    longestIndexList = dupIndexList[i];
            }
            return longestIndexList;
        }

        private static bool IsIndexExisting(List<int> targetList, int index)
        {
            bool isExisting = false;
            for (int i = 0; i < targetList.Count; i++)
            {
                if (targetList[i] == index)
                    isExisting = true;
            }

            return isExisting;
        }


        /// <summary>
        /// Write Texture2D(image) to a image file
        /// </summary>
        public static string Texture2DToPNGFile(Texture2D texture2D, string fileName)
        {
            var bytes = texture2D.EncodeToPNG();
            var path = string.Format("{0}/{1}.png", Application.persistentDataPath, fileName);
            File.WriteAllBytes(path, bytes);

            return path;
        }
        
        
        public static Texture2D ScaleTextureCutOut(Texture2D originalTexture, float startX, float startY, float originalWidth, float originalHeight)
        {
            originalWidth = Mathf.Clamp(originalWidth, 0, Mathf.Max(originalTexture.width - startX, 0));
            originalHeight = Mathf.Clamp(originalHeight, 0, Mathf.Max(originalTexture.height - startY, 0));
            Texture2D newTexture = new Texture2D(Mathf.CeilToInt(originalWidth), Mathf.CeilToInt(originalHeight));
            int maxX = originalTexture.width - 1;
            int maxY = originalTexture.height - 1;
            for (int y = 0; y < newTexture.height; y++)
            {
                for (int x = 0; x < newTexture.width; x++)
                {
                    float targetX = x + startX;
                    float targetY = y + startY;
                    int x1 = Mathf.Min(maxX, Mathf.FloorToInt(targetX));
                    int y1 = Mathf.Min(maxY, Mathf.FloorToInt(targetY));
                    int x2 = Mathf.Min(maxX, x1 + 1);
                    int y2 = Mathf.Min(maxY, y1 + 1);

                    float u = targetX - x1;
                    float v = targetY - y1;
                    float w1 = (1 - u) * (1 - v);
                    float w2 = u * (1 - v);
                    float w3 = (1 - u) * v;
                    float w4 = u * v;
                    Color color1 = originalTexture.GetPixel(x1, y1);
                    Color color2 = originalTexture.GetPixel(x2, y1);
                    Color color3 = originalTexture.GetPixel(x1, y2);
                    Color color4 = originalTexture.GetPixel(x2, y2);
                    Color color = new Color(Mathf.Clamp01(color1.r * w1 + color2.r * w2 + color3.r * w3 + color4.r * w4),
                                            Mathf.Clamp01(color1.g * w1 + color2.g * w2 + color3.g * w3 + color4.g * w4),
                                            Mathf.Clamp01(color1.b * w1 + color2.b * w2 + color3.b * w3 + color4.b * w4),
                                            Mathf.Clamp01(color1.a * w1 + color2.a * w2 + color3.a * w3 + color4.a * w4)
                                            );
                    newTexture.SetPixel(x, y, color);
                }
            }
            newTexture.anisoLevel = 2;
            newTexture.Apply();
            return newTexture;
        }
        
        /// <summary>
        /// Display rules
        /// 1) <1000 yd (>0 && <9144m): Unit 'yd', 1 digit. e.g. "999.9 yd"
        /// 2) >=1000 yd && <1000mi (>= 9144m && <1609344m): Unit 'mi', 1 digit. e.g. "999.9 mi"
        /// 3) >=1000mi (>= 1609344m): Just show ">1000 mi"
        /// </summary>
        /// <param name="valueMeter"></param>
        /// <returns></returns>
        public static void TrimDistanceNumber4DisplayYardMiles(float valueMeter, Distance4Display distance4Dispaly)
        {
            //rule for displaying 'yard -- miles'
            if (valueMeter > 0 && valueMeter < 9144f) //use unit 'yd'
            {
                double valueYard = valueMeter * (1 / 0.9144);
                distance4Dispaly.value = valueYard.ToString("F1");
                distance4Dispaly.unit = "yd";
            }
            else if (valueMeter >= 9144f && valueMeter < 1609344f) //use unit 'mi'
            {
                double valueMiles = valueMeter / 1000 * 0.621371;
                distance4Dispaly.value = valueMiles.ToString("F1");
                distance4Dispaly.unit = "mi";
            }
            else if (valueMeter >= 1609344f)
            {
                distance4Dispaly.value = ">1000";
                distance4Dispaly.unit = "mi";
            }
        }




    }
    
    
    public class Distance4Display
    {
        public string value;
        public string unit;
    }
    
    
    

}

