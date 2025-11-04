using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlutterUnityIntegration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SocialBeeAR;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID
public class NativeAPI
{
    [DllImport("libARWrapper.so")]
    public static extern void onClearMap(string mapId);
    [DllImport("libARWrapper.so")]
    public static extern void onDeleteMultiple(string activityIds);
    [DllImport("libARWrapper.so")]
    public static extern void onWillUpdatePost(string activityId, string isCheckIn);
    [DllImport("libARWrapper.so")]
    public static extern void onWillUpdateActivitiesMapId(string mapId, string activityIds);
    [DllImport("libARWrapper.so")]
    public static extern void onActivityDeleted(string activityId);
    [DllImport("libARWrapper.so")]
    public static extern void onAnchorDeleted(string anchorId);
    [DllImport("libARWrapper.so")]
    public static extern void onActivitySubmitted(string json, string activityType);
    [DllImport("libARWrapper.so")]
    public static extern void onActivityCompleted(string json, string activityType);
    [DllImport("libARWrapper.so")]
    public static extern void debugMessage(string message);
    [DllImport("libARWrapper.so")]
    public static extern void getCurrentUserAuthToken(string identifier, string callback);
    [DllImport("libARWrapper.so")]
    public static extern void showNativePage(string page);    
    [DllImport("libARWrapper.so")]
    public static extern void openGallery(string activityId);    
    [DllImport("libARWrapper.so")]
    public static extern void saveContentPath(string contentPath);
    [DllImport("libARWrapper.so")]
    public static extern void onWillGetImageKeywords(string contentPath, string getAll);
    [DllImport("libARWrapper.so")]
    public static extern void onWillGetLocationInfo(string latitude, string longitude);
    [DllImport("libARWrapper.so")]
    public static extern void onSceneLoaded(string reference);
    [DllImport("libARWrapper.so")]
    public static extern void onLoadActivityFeed(string json);
    [DllImport("libARWrapper.so")]
    public static extern void endAR();
    [DllImport("libARWrapper.so")]
    public static extern void OnUnityMessage(string message);
}
#elif UNITY_IOS  
public class NativeAPI
{
    [DllImport("__Internal")]
    public static extern void onClearMap(string mapId);
    [DllImport("__Internal")]
    public static extern void onDeleteMultiple(string activityIds);
    [DllImport("__Internal")]
    public static extern void onWillUpdatePost(string activityId, string isCheckIn);
    [DllImport("__Internal")]
    public static extern void onWillUpdateActivitiesMapId(string mapId, string activityIds);
    [DllImport("__Internal")]
    public static extern void onActivityDeleted(string activityId);
    [DllImport("__Internal")]
    public static extern void onAnchorDeleted(string anchorId);
    [DllImport("__Internal")]
    public static extern void onActivitySubmitted(string json, string activityType);
    [DllImport("__Internal")]
    public static extern void onActivityCompleted(string json, string activityType);
    [DllImport("__Internal")]
    public static extern void debugMessage(string message);
    [DllImport("__Internal")]
    public static extern void getCurrentUserAuthToken(string identifier, string callback);
    [DllImport("__Internal")]
    public static extern void showNativePage(string page);    
    [DllImport("__Internal")]
    public static extern void openGallery(string activityId);    
    [DllImport("__Internal")]
    public static extern void saveContentPath(string contentPath);
    [DllImport("__Internal")]
    public static extern void onWillGetImageKeywords(string contentPath, string getAll);
    [DllImport("__Internal")]
    public static extern void onWillGetLocationInfo(string latitude, string longitude);
    [DllImport("__Internal")]
    public static extern void onSceneLoaded(string reference);
    [DllImport("__Internal")]
    public static extern void onLoadActivityFeed(string json);
    [DllImport("__Internal")]
    public static extern void endAR();
    
    [DllImport("__Internal")]
    public static extern void OnUnityMessage(string message);
}
#endif
public class NativeCall : BaseSingletonClass<NativeCall>
{
    // Refreshing repo commit...
    
    public Text firstText;
    public Text secondText;
    
    public ExperienceData expData;
    //private SBContextManager sbContextManager;
    ////[HideInInspector] public TriviaContentFacade triviaContentFacade;
    //public void Start()
    //{
    //    sbContextManager = gameObject.GetComponent<SBContextManager>();
    //    expData = new ExperienceData();
    //}

    public void OnSceneLoaded(string reference)
    {
        NativeAPI.onSceneLoaded(reference);
    }

    /// <summary>
    /// Sends a message to the native app that will link the cloud map Id to the created activities.
    /// </summary>
    /// <param name="mapId"></param>
    /// <param name="activityIds"></param>
    public void OnWillUpdateActivitiesMapId(string mapId, string[] activityIds)
    {        
        NativeAPI.onWillUpdateActivitiesMapId(mapId, activityIds == null ? "" : string.Join(",", activityIds));
    }

    /// <summary>
    /// Sends a message to the native app updating the post activity as "having a checkin only" or not. 
    /// </summary>
    /// <param name="activityId">The Id of the activity.</param>
    /// <param name="isCheckIn">The flag.</param>
    public void OnWillUpdatePost(string activityId, bool isCheckIn)
    {
        NativeAPI.onWillUpdatePost(activityId, isCheckIn.ToString().ToLower());
    }

    /// <summary>
    /// Sends a message to the native app when an anchor is successfully deleted.
    /// </summary>
    /// <param name="id">The Id of the anchor that was deleted.</param>
    public void OnAnchorDeleted(string id)
    {
        NativeAPI.onAnchorDeleted(id);
    }

    /// <summary>
    /// Sends a message to the native app when an activity is successfully deleted.
    /// </summary>
    /// <param name="id">The Id of the activity that was deleted.</param>
    public void OnActivityDeleted(string id)
    {
        NativeAPI.onActivityDeleted(id);
    }

    /// <summary>
    /// Sends a message to the native app when an activity is created or updated.
    /// </summary>    
    /// <param name="json">The serialized value of the activity that was created or updated.</param>
    public void OnActivitySubmitted(string json, string activityType)
    {
        NativeAPI.onActivitySubmitted(json, activityType);
    }

    /// <summary>
    /// Sends a message to the native app when an activity is completed (consumed).
    /// </summary>    
    /// <param name="json">The serialized value of the activity that was created or updated.</param>
    public void OnActivityCompleted(string json, string activityType)
    {
        NativeAPI.onActivityCompleted(json, activityType);
        
        //return JsonConvert.SerializeObject(this, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        
        var messageManager = GetComponent<UnityMessageManager>();
        messageManager.SendMessageToFlutter(new UnityMessage
        {
            name = "CONSUME_ACTIVITY",
            data = JObject.FromObject(new
            {
                payload = json
                // We don't need the activityType type here as we will use the one defined in the json data. 
            })
        });
    }
    
    // <summary>
    /// Sends a message to the native app when an activity is completed (consumed).
    /// </summary>    
    /// <param name="json">The serialized value of the activity that was created or updated.</param>
    public void OnActivityCompleted(string json, ActivityType activityType)
    {
        OnActivityCompleted(json, ((int)activityType).ToString());
    }

    /// <summary>
    /// Sends a message to the native app that multiple activities will be deleted.
    /// </summary>    
    /// <param name="json">The serialized value of the activity Ids that will be deleted.</param>
    public void OnDeleteMultiple(string activityIds)
    {
        NativeAPI.onDeleteMultiple(activityIds);
    }

    /// <summary>
    /// Sends a message to the native app that the created activities will be deleted.
    /// </summary>    
    /// <param name="json">The serialized value of the activity that was created or updated.</param>
    public void OnClearMap(string mapId)
    {
        NativeAPI.onClearMap(mapId);
    }

    public void DebugMessage(string message)
    {
        NativeAPI.debugMessage(message);
    }
    public void GetCurrentUserAuthToken(string identifier, string callback)
    {
        NativeAPI.getCurrentUserAuthToken(identifier, callback);
    }
    //public void ReceiveSelectedMedia(byte[] media)
    //{

    //}
     
    public void OpenGallery(string activityId)
    {
        NativeAPI.openGallery(activityId);
    }
    public void ShowNative(string page)
    {
        NativeAPI.showNativePage(page);
    }

    /// <summary>
    /// Sends a message to the native app to get the location information based on the latitude and longitude.
    /// </summary>    
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    public void OnWillGetLocationInfo(double latitude, double longitude)
    {
        NativeAPI.onWillGetLocationInfo(latitude.ToString(), longitude.ToString());
    }

    /// <summary>
    /// Sends a message to the native app to get the keywords of an image from a path.
    /// </summary>    
    /// <param name="contentPath">The local path of the image.</param>
    public void OnWillGetImageKeywords(string contentPath, bool getAll)
    {
        NativeAPI.onWillGetImageKeywords(contentPath, getAll.ToString());
    }

    /// <summary>
    /// Loads the Activity Feed view from native.
    /// </summary>    
    /// <param name="json">The serialized value of the created activities that the Activity Feed view needs.</param>
    public void OnLoadActivityFeed(string json)
    {
        NativeAPI.onLoadActivityFeed(json);
    }
    
    int first = 0;
    int second = 0;
    
    public void SetFirstText(String message)
    {
        print($"Unity SetFirstText = {message}");
        first++;
        firstText.text = $"Was pressed {first} times.";
    }
    
    public void SetSecondText(String message)
    {
        print($"Unity SetFirstText = {message}");
        second++;
        secondText.text = $"Was pressed {second} times.";
    }
    
    
    public void OnUnityMessage(string message)
    {
        NativeAPI.OnUnityMessage(message);
    }
    
    public void EndAR()
    {
        print("END_AR");
        NativeAPI.endAR();

        var messageManager = GetComponent<UnityMessageManager>();
        messageManager.SendMessageToFlutter(new UnityMessage
        {
            name = "END_AR",
            data = JObject.FromObject(new
            {
                payload = JsonConvert.SerializeObject(
                    new PostActivityInfo
                    {
                        Id = "123",
                        ExperienceId = "exp12",
                        Title = "test post",
                        Pose = new Pose(new Vector3(0.1f,0.2f,0.3f), new Quaternion(1.0f,1.2f,1.3f, 1.4f))
                    }, 
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                ActivityType.Post
            })
        });
    }

    public void SaveContentPath(string path) {
        NativeAPI.saveContentPath(path);       
    }
     
    void GetTriviaInfo(string triviaInfo)
    {
        //// Convert info from native to TriviaQuestion
        //char[] charSeparators = new char[] { ',' };
        //string[] result = triviaInfo.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
        //TriviaQuestion triviaQuestion = new TriviaQuestion();
        //triviaQuestion.question = result[0];
        //triviaQuestion.optionList = new List<string>();
        //triviaQuestion.optionList.Add(result[1]);
        //triviaQuestion.optionList.Add(result[2]);
        //triviaQuestion.optionList.Add(result[3]);
        //triviaQuestion.optionList.Add(result[4]);
        //triviaQuestion.selectedIndex = Int32.Parse(result[5]);
        //triviaQuestion.hints = result[6];
        //if (triviaContentFacade == null)
        //{
        //    Debug.Log("**** triviaContentFacade == null");
        //    triviaContentFacade = FindObjectOfType<TriviaContentFacade>();
        //}
        ////triviaContentFacade.OnEditTriviaDone(triviaQuestion);
    }
    void GetPhotoPath(string photoPath)
    {
        PhotoContentFacade photoContentFacade = FindObjectOfType<PhotoContentFacade>();
        //photoContentFacade.OnEditPhotoDone(photoPath);
    }
    void GetVideoPath(string videoPath)
    {
        VideoContentFacade videoContentFacade = FindObjectOfType<VideoContentFacade>();
        //videoContentFacade.OnEditVideoDone(videoPath);
    }
    public void SetExperienceData(string expId, string expName, string actGroupId, string actGroupName)
    {
        expData.experienceId = expId;
        expData.experienceName = expName;
        expData.activityGroupId = actGroupId;
        expData.activityGroupName = actGroupName;
    }
}
[SerializeField]
public class ExperienceData
{
    public string experienceId;
    public string experienceName;
    public string activityGroupId;
    public string activityGroupName;
}