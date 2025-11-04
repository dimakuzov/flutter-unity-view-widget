using UnityEngine;

namespace SocialBeeAR
{

    /// <summary>
    /// This is for definition of const variables
    /// </summary>
    public class Const
    {
        public static readonly string OnBoardingScene = "BeeBotScene";
        public static readonly string MainScene = "MainScene";
        public static readonly string SceneLoader = "SceneLoader";
        public static readonly string TestScene = "TestSwitch";

        public static int THUMBNAIL_SCALE = 7;
        public static int THUMBNAIL_DEPTH = 16;
        public static float THUMBNAIL_PREVIEW_SCALE = 1.5f;

        public static Color FEATURE_POINT_WEAK = new Color32(180, 180, 180, 64);
        public static Color FEATURE_POINT_STRONG = new Color32(87, 0, 181, 255);
        public static Color FLY_TO_PANEL_PATH = new Color32(150, 0, 255, 255);

        //public static Vector3 ANCHOR_Y_OFFSET = new Vector3(0, 0.3f, 0);
        public static Vector3 ANCHOR_SCALE = new Vector3(0.001f, 0.001f, 0.001f);

        public static int MIN_MAP_SIZE = 250; //200 is the default value from placenote sample

        public static readonly string ANCHOR_DATA_JSON_ROOT = "SBAnchorList";

        public static readonly float DISTANCE_TO_ENGAGE_GPS_ANCHOR = 6.09f; //about 20 feet
        public static readonly float DISTANCE_TO_ALERT_USER = 20f; //3/10
        public static readonly float ANCHOR_ROTATION_SPEED = 24f;

        //anchor's distance control
        public static readonly float DISTANCE_TO_ENGAGE_ANCHOR = 2.5f; //2.5
        public static readonly float DISTANCE_TO_SWAP_MARKER_AND_ANCHOR = 2.5f; //2.5
        public static readonly float DISTANCE_4_TOO_FAR_INDICATOR = 5f;//5m
        public static readonly float DISTANCE_4_SUPER_FAR_INDICATOR = 30f;//30m

        public static readonly float DISTANCE_4_FLYOVER = 3f; //50m, when > this distance, fly the anchor to user //new: 3m


        public static Vector3 PULSE_INDICATOR_SIZE_MIN = new Vector3(0.7f, 0.7f, 0.7f); //the minimal size of pulse indicator
        public static Vector3 PULSE_INDICATOR_SIZE_MAX = new Vector3(1.2f, 1.2f, 1.2f); //the max size of pulse indicator


        public static readonly string MAP_PREFIX = "SBMap";

        public const string ANIMATION_FADE_OFF = "FadeOff";
        public const string ANIMATION_FADE_ON = "FadeOn";

        public const string ANCHOR_OBJ_PREFIX_COVER = "Cover";
        public const string ANCHOR_OBJ_COMPLETION_OBJ_NAME = "CompletionObj";
        
        public const float PhonePoseAngle = 45;

        public const float PANEL_ANIMATION_TIME = 0.35f;

        //-------------------- activity properties ------------------------
        // DO NOT CHANGE THIS DEFAULT ID as we are using it in the SB API.
        public const string ACTIVITY_DEFAULT_ID = "DefaultPoIPanel";

        //PoI
        public const string ActProp_Post_Name = "PoIName";
        public const string ActProp_Post_Description = "PoIDescription";

        //Trivia
        public const string ActProp_Trivia_Question = "questions";
        public const string ActProp_Trivia_Option_Prefix = "option";
        public const string ActProp_Trivia_displaySequence = "displaySequence";
        public const string ActProp_Trivia_hint = "hint";
        public const string ActProp_Trivia_isRandomeEnabled = "isRandomEnabled";
        public const string ActProp_Trivia_answerIndex = "answerIndex";
        public const string ActProp_Trivia_userAnswerIndex = "userAnswerIndex";

        //Photo
        public const string ActProp_Photo_Title = "photoTitle";
        public const string ActProp_Photo_Url = "photoUrl"; // this is the local path of the photo
        public const string ActProp_Photo_External_Url = "photoPublicUrl"; // this is the URL of the photo
        public const string ActProp_Photo_Keywords = "photoKeywords";
        public const string ActProp_Photo_AltKeywords = "photoAltKeywords";

        //Video
        public const string ActProp_Video_Title = "videoTitle";
        public const string ActProp_Video_Url = "videoUrl";
        public const string ActProp_Video_Thumbnail = "thumbnail";

        //Audio
        public const string ActProp_Audio_Title = "audioTitle";
        public const string ActProp_Audio_Url = "audioUrl";

		// All types of activities have these properties.
        public const string ACTIVITY_PROP_ISCHALLENGE = "IsChallenge";
        public const string ACTIVITY_PROP_STATUS = "Status";
        public const string ACTIVITY_PROP_POINTS = "Points";


        //----------------- GPS anchor object-------------------------
        public static Color32 OUTER_GOOD = new Color32(0, 255, 92, 255);
        public static Color32 MIDDLE_GOOD = new Color32(0, 87, 31, 255);
        public static Color32 OUTER_MEDIUM = new Color32(255, 159, 0, 255);
        public static Color32 MIDDLE_MEDIUM = new Color32(207, 37, 0, 255);
        public static Color32 OUTER_BAD = new Color32(255, 20, 0, 255);
        public static Color32 MIDDLE_BAD = new Color32(106, 0, 7, 255);
        public static Color32 OUTER_DEFAULT = new Color32(83, 35, 255, 255);
        public static Color32 MIDDLE_DEFAULT = new Color32(15, 0, 79, 255);
        public static float meter2feetFactor = 3.2808398950131f;
        public static float STRENGTH_WEAK = 0.33f;
        public static float STRENGTH_MEDIUM = 0.66f;

    }

}
