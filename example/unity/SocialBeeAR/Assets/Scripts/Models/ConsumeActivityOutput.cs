using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace SocialBeeAR
{
    /// <summary>
    /// The base definition of consumed activities.
    /// </summary>
    public interface IConsumedActivityOutput : IConsumeActivityInput
    {
        /// <summary>
        /// The type of this activity output.
        /// </summary>
        ActivityType Type { get; }
        /// <summary>
        /// The Id of the "submitted/completed activity" generated from the server.
        /// This is not the Id of the consumed "created activity".
        /// </summary>            
        string UniqueId { get; set; }
        /// <summary>
        /// The points earned from consuming or completing the activity.
        /// </summary>
        int Points { get; set; }
        /// <summary>
        /// The date the activity was completed.
        /// </summary>
        DateTime DateCompleted { get; set; }
        /// <summary>
        /// The status of the completed activity.
        /// </summary>
        ActivityStatus Status { get; set; }
        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        string ToJson();
    }

    /// <summary>
    /// The base class for consumed activities.
    /// </summary>
    [Serializable]
    public abstract class ConsumedActivityOutput<T> : ConsumeActivityInput, IConsumedActivityOutput
        where T : ConsumedActivityOutput<T>
    {
        /// <summary>
        /// The type of this activity output.
        /// </summary>
        public ActivityType Type { get; }
        /// <summary>
        /// The Id of the "submitted/completed activity" generated from the server.
        /// This is not the Id of the consumed "created activity".
        /// </summary>             
        public string UniqueId { get; set; }
        /// <summary>
        /// The points earned from consuming or completing the activity.
        /// </summary>
        public int Points { get; set; }
        /// <summary>
        /// The date the activity was completed.
        /// </summary>
        public DateTime DateCompleted { get; set; }
        /// <summary>
        /// The status of the completed activity.
        /// </summary>
        public ActivityStatus Status { get; set; }

        //public static T Create<U>(string json) where U : IConsumedActivityOutput
        //{
        //    try
        //    {
        //        var data = JsonConvert.DeserializeObject<T>(json);

        //        return data;
        //    }
        //    catch
        //    {
        //        // ToDo: let's log this error and notify ourselves.
        //        print($"Create: Cannot deserialize json to create an instance of {typeof(T).ToString()}.");
        //        return null;
        //    }
        //}
        public static GenericConsumedOutput Create(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<GenericConsumedOutput>(json);

                return data;
            }
            catch
            {
                // ToDo: let's log this error and notify ourselves.
                MessageManager.Instance.DebugMessage($"Create: Cannot deserialize json to create an instance of {typeof(T).ToString()}.");
                return null;
            }
        }

        public static GenericConsumedOutput CreateFromData(string json)
        {            
            try
            {                
                var result = JsonConvert.DeserializeObject<ResultData>(json);
                if (result.data == null)
                    return null;

                //var o = default(T);                
                //print($"o is null? {o == null}");
                //print($"data is null? {result.data == null}");

                //print($"setting uniqueId");
                //o.UniqueId = result.data.uniqueId;
                //print($"setting points");
                //o.Points = result.data.points;
                //print($"setting dateCompleted");
                //o.DateCompleted = result.data.dateCompleted;
                //print($"setting status");
                //o.Status = (ActivityStatus)result.data.status;

                //return o;

                return result.data;
            }
            catch(Exception ex)
            {
                // ToDo: let's log this error and notify ourselves.
                MessageManager.Instance.DebugMessage($"CreateFromData:1 Cannot deserialize json to create an instance of {typeof(T).ToString()}.");
                MessageManager.Instance.DebugMessage($"{ex.ToString()}");
                //return null;
            }

            try
            {
                var x = JsonUtility.FromJson<ResultData>(json);
                return null;
            }
            catch (Exception ex)
            {
                // ToDo: let's log this error and notify ourselves.
                MessageManager.Instance.DebugMessage($"CreateFromData:1 Cannot deserialize json using JsonUtility.");
                MessageManager.Instance.DebugMessage($"{ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }

        public override string ToString()
        {
            return $"ID={UniqueId} | DateCompleted={DateCompleted} | Status={Status} | Points={Points}";
        }
    }

    /// <summary>
    /// Represents a completed trivia challenge.
    /// </summary>
    [Serializable]
    public class ConsumedTriviaOutput : ConsumedActivityOutput<ConsumedTriviaOutput>
    {
        /// <summary>
        /// The answer selected by the user from a set of options.
        /// </summary>
        public int Answer { get; set; }        
        /// <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {
                return ActivityType.Trivia;
            }
        }
        
        public override string ToString()
        {
            return $" | Answer={Answer}";
        }
    }

    /// <summary>
    /// Represents a consumed post activity.
    /// </summary>
    [Serializable]
    public class ConsumedPostOutput : ConsumedActivityOutput<ConsumedPostOutput>
    {
        // <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {
                return ActivityType.Post;
            }
        }        
    }

    /// <summary>
    /// Represents a consumed check-in activity.
    /// </summary>
    [Serializable]
    public class ConsumedCheckInOutput : ActivityOutput<ConsumedCheckInOutput>
    {
        // <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {                
                return ActivityType.CheckIn;
            }
        }
    }

    /// <summary>
    /// Represents a consumed or compelted photo activity.
    /// </summary>
    [Serializable]
    public class ConsumedPhotoOutput : ConsumedActivityOutput<ConsumedPhotoOutput>
    {
        /// <summary>
        /// The URLs of the sample photos.
        /// </summary>
        /// <remarks>
        /// Multiple photos in a single API call are already supported by the API.
        /// In AR, for now, we will allow a single photo upload but we will already use a list.
        /// </remarks>
        public IEnumerable<string> SamplePhotos { get; set; }
        /// <summary>
        /// An identifier which persistently identifier the object on a given device.
        /// </summary>
        /// <remarks>
        /// We will use this to try and load resources from the user's device, if present.
        /// If it fails, then the resource will be loaded via the <see cref="ResourceURL"/>.
        /// </remarks>
        public IEnumerable<string> ResourceLocalIdentifiers { get; set; }
        /// <summary>
        /// If this activity is a challenge, then these are the photos submitted to match the created photos.
        /// </summary>
        public IEnumerable<string> SubmittedPhotos { get; set; }
        /// <summary>
        /// If this activity is a challenge, this is the list of words associated with the submitted photos.
        /// </summary>
        public IEnumerable<string> Keywords { get; set; }         
        // <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {
                return ActivityType.PhotoVideo;
            }
        }
    }

    /// <summary>
    /// Represents a consumed video activity.
    /// </summary>
    [Serializable]
    public class ConsumedVideoOutput : ConsumedActivityOutput<ConsumedVideoOutput>
    {
        // <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {
                return ActivityType.PhotoVideo;
            }
        }
    }

    /// <summary>
    /// Represents a consumed video activity.
    /// </summary>
    [Serializable]
    public class ConsumedAudioOutput : ConsumedActivityOutput<ConsumedAudioOutput>
    {
        // <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {
                return ActivityType.Audio;
            }
        }
    }
}
