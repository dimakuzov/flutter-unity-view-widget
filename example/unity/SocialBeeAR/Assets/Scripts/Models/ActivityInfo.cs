using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace SocialBeeAR
{
    public interface IActivityInfo
    {
        /// <summary>
        /// The Id of the experience that this activity belongs to.
        /// </summary>
        string ExperienceId { get; set; }
        /// <summary>
        /// The Id of the activity.
        /// </summary>
        string Id { get; set; }
        /// <summary>
        /// For a Post type, this is the title and is required.
        /// For a Trivia, this is the question and is required.
        /// For a Photo, Video, and Audio, this is the caption and is optional.
        /// For a Check-In (POI), this is the location name and is optional.
        /// </summary>
        string Title { get; set; }
        /// <summary>
        /// The Id of the parent activity of this activity.
        /// </summary>
        string ParentId { get; set; }
        /// <summary>
        /// The Id of the cloud map where this activity is created.
        /// </summary>
        string MapId { get; set; }
        /// <summary>
        /// The Niantic-specific information of the anchor to which this post belongs to.
        /// </summary>
        string AnchorPayload { get; set; }
        /// <summary>
        /// Flag if the activity is a challenge.
        /// </summary>
        bool IsChallenge { get; }
        /// <summary>
        /// When creating this is the points awarded to the creator.
        /// During consume, this is the points that the consumer will get upon successful completion of the activity. 
        /// </summary>
        int Points { get; set; }        
        /// <summary>
        /// The position and rotation info of this activity.
        /// </summary>
        Pose Pose { get; set; }
        /// <summary>
        /// Flag that indicates if this activity has already been submitted to the API.        
        /// </summary>
        /// <remarks>
        /// We need to have this flag because of the way the <see cref="activityId"/> has been initially designed.
        /// A new instance of an activity is assiged a temporary activityId.
        /// We use that to search the activity in an anchor when the Unity app communicates to the native app.
        /// When the activity is successfully submitted to the API for the first time, it is give a permanent ID, which comes from the API.
        /// This permanent Id is also present when the activity is retrieved back from the API.
        /// </remarks>
        bool IsEditing { get; set; }       
        /// <summary>
        /// The status of the activity.
        /// </summary>
        ActivityStatus Status { get; set; }
        /// <summary>
        /// The Id of the "completed activity".
        /// </summary>
        /// <remarks>
        /// When activity is completed, a new object based on the activity is created.
        /// CompletedId is the Id of that object.
        /// </remarks>
        string CompletedId { get; set; }
        /// <summary>
        /// The points earned from completing this activity.        
        /// </summary>
        int PointsEarned { get; set; }
        /// <summary>
        /// The date this activity was completed.
        /// </summary>
        DateTime DateCompleted { get; set; }
        /// <summary>
        /// 
        /// </summary>
        ActivityType Type { get; }
        /// <summary>
        /// Resets the model values.
        /// </summary>
        void Reset();
        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        string ToJson();
        /// <summary>
        /// Returns a new instance of this class.
        /// </summary>
        /// <returns></returns>
        IActivityInfo Clone();
        /// <summary>
        /// Returns true if the activity has been consumed.
        /// </summary>
        /// <returns></returns>
        bool IsCompleted();
    }

    public abstract class ActivityInfo : IActivityInfo
    {
        public ActivityType Type { get; protected set; }
        /// <summary>
        /// The Id of the experience that this activity belongs to.
        /// </summary>
        public string ExperienceId { get; set; }
        //public ActivityType Type { get; private set; }
        /// <summary>
        /// The Id of the activity.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// For a Post type, this is the title and is required.
        /// For a Trivia, this is the question and is required.
        /// For a Photo, Video, and Audio, this is the caption and is optional.
        /// For a Check-In (POI), this is the location name and is optional.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The Id of the parent activity of this activity.
        /// </summary>
        public string ParentId { get; set; }
        /// <summary>
        /// The Id of the cloud map (or the VPS Location ID) where this activity is created.
        /// </summary>
        public string MapId { get; set; }
        /// <summary>
        /// The Niantic-specific information of the anchor to which this post belongs to.
        /// </summary>
        public string AnchorPayload { get; set; }
        /// <summary>
        /// Flag if the activity is a challenge.
        /// </summary>
        public bool IsChallenge { get; private set; }
        /// <summary>
        /// When creating this is the points awarded to the creator.
        /// During consume, this is the points that the consumer will get upon successful completion of the activity. 
        /// </summary>
        public int Points { get; set; }       
        /// <summary>
        /// The position and rotation info of this activity.
        /// </summary>
        public Pose Pose { get; set; }
        /// <summary>
        /// Flag that indicates if this activity has already been submitted to the API.        
        /// </summary>
        /// <remarks>
        /// We need to have this flag because of the way the <see cref="activityId"/> has been initially designed.
        /// A new instance of an activity is assiged a temporary activityId.
        /// We use that to search the activity in an anchor when the Unity app communicates to the native app.
        /// When the activity is successfully submitted to the API for the first time, it is give a permanent ID, which comes from the API.
        /// This permanent Id is also present when the activity is retrieved back from the API.
        /// </remarks>
        public bool IsEditing { get; set; }
        /// <summary>
        /// The status of the activity.
        /// </summary>
        public ActivityStatus Status { get; set; }
        /// <summary>
        /// The Id of the "completed activity".
        /// </summary>
        /// <remarks>
        /// When activity is completed, a new object based on the activity is created.
        /// CompletedId is the Id of that object.
        /// </remarks>
        public string CompletedId { get; set; }
        /// <summary>
        /// The points earned from completing this activity.        
        /// </summary>
        public int PointsEarned { get; set; }
        /// <summary>
        /// The date this activity was completed.
        /// </summary>
        public DateTime DateCompleted { get; set; }
        /// <summary>
        /// Resets the model values.
        /// </summary>
        public void Reset()
        {
            Title = "";
        }
        /// <summary>
        /// Helper method that sets the <see cref="IsChallenge"/> property.
        /// We need this because we want IsChallenge to be immutable.
        /// Also, immutable properties are not supported by Unity as of this writing with v2019.4.4f1.
        /// </summary>
        /// <param name="value"></param>
        protected void SetIsChallenge(bool value)
        {
            IsChallenge = value;
        }

        public bool IsCompleted()
        {
            return Status != ActivityStatus.New && Status != ActivityStatus.Undefined;
        }

        public override string ToString()
        {
            return $"id={Id} | parentId={ParentId} | title={Title} | isChallenge={IsChallenge} | mapId={MapId} | status={Status} | completedId={CompletedId} | Type={Type} | anchorPayload={AnchorPayload}";
        }
        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
        /// <summary>
        /// Returns a new instance of this class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract IActivityInfo Clone();
    }

    public class SimpleFlagModel
    {
        public string Text { get; set; }
        public bool IsActive { get; set; }
    }
}