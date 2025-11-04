using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SocialBeeAR
{
    /// <summary>
    /// The base definition of created activities.
    /// </summary>
    public interface IActivityOutput : IActivityInput
    {
        /// <summary>
        /// True, if the activity is created by the consumer.
        /// </summary>
        bool IsOwnContent { get; set; }
        /// <summary>
        /// The type of this activity output.
        /// </summary>
        ActivityType Type { get; }
        /// <summary>
        /// The Id of the activity that was generated in this client app.
        /// </summary>
        /// <remarks>
        /// The Id generation of an activity is handled by the API server.
        /// But an instance of an SBActivity is already associated with a panel
        /// and is assigned a generated ID when a panel is created.
        /// After the activity is saved, we need to update the ID that came from the server.
        /// We need the updated ID for succeeding actions, i.e. editing the activity or linking the Placenote MapID to it.
        /// </remarks>
        string ReferenceId { get; set; }
        /// <summary>
        /// The Id of the created activity generated from the server.
        /// </summary>               
        string UniqueId { get; set; }
        /// <summary>
        /// The points earned from creating the activity.
        /// </summary>
        int PointsCreation { get; set; }
        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        string ToJson();
    }

    /// <summary>
    /// The base class for creating activities whose member fields are used to pass params to the API.
    /// </summary>
    [Serializable]
    public abstract class ActivityOutput<T> : ActivityInput, IActivityOutput        
        where T: ActivityOutput<T>
    {
        /// <summary>
        /// True, if the activity is created by the consumer.
        /// </summary>
        public bool IsOwnContent { get; set; }
        /// <summary>
        /// The type of this activity output.
        /// </summary>
        public ActivityType Type { get; }
        /// <summary>
        /// The Id of the activity that was generated in this client app.
        /// </summary>
        /// <remarks>
        /// The Id generation of an activity is handled by the API server.
        /// But an instance of an SBActivity is already associated with a panel
        /// and is assigned a generated ID when a panel is created.
        /// After the activity is saved, we need to update the ID that came from the server.
        /// We need the updated ID for succeeding actions, i.e. editing the activity or linking the Placenote MapID to it.
        /// </remarks>
        public string ReferenceId { get; set; }
        /// <summary>
        /// The Id of the created activity. This will be generated from the server.
        /// </summary>               
        public string UniqueId { get; set; }
        /// <summary>
        /// The points earned from creating the activity.
        /// </summary>
        public int PointsCreation { get; set; }

        public static T Create(string json)
        {         
            return Create<T>(json);
        }

        /// <summary>
        /// Returns a serialized version of this instance.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
    }

    /// <summary>
    /// Represents a created (recorded) trivia challenge.
    /// </summary>
    [Serializable]
    public class TriviaOutput : ActivityOutput<TriviaOutput>, IActivityOutput
    {
        public IList<string> Options { get; set; }
        public int Answer { get; set; }
        public IList<string> Hints { get; set; }
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
        
        public static TriviaOutput CreateFrom(string id, TriviaChallengeInput input)
        {
            return new TriviaOutput
            {
                UniqueId = id,
                ExperienceId = input.ExperienceId,
                Options = input.Options,
                Answer = input.Answer,
                Hints = input.Hints,
                IsPlanning = input.IsPlanning
            };
        }
    }

    /// <summary>
    /// Represents a created (recorded) post activity.
    /// </summary>
    [Serializable]
    public class PostOutput : ActivityOutput<PostOutput>, IActivityOutput
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
        /// <summary>
        /// The Id of the photo/video that was created via the Quick PhotoVideo functionality.
        /// </summary>
        [Obsolete("Use ChildrenIds")]
        public string PhotoVideoId { get; set; }
        /// <summary>
        /// The Ids of the activities that were created before this post was saved.
        /// </summary>
        public IEnumerable<string> ChildrenIds { get; set; }
        /// <summary>
        /// Additional information about the post activity.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type of created content, i.e. a link to a video, photo or a website page.
        /// </summary>
        public PostType SourceType { get; set; }
        /// <summary>
        /// If the post contains a previewed URL and the URL contains a video
        /// then this will be the actual URL of the video.
        /// If the resource is an image then this is a link to that image.
        /// </summary>
        public string ResourceURL { get; set; }
        /// <summary>
        /// An identifier which persistently identifier the object on a given device.
        /// </summary>
        /// <remarks>
        /// We will use this to try and load resources from the user's device, if present.
        /// If it fails, then the resource will be loaded via the <see cref="ResourceURL"/>.
        /// </remarks>
        public string ResourceLocalIdentifier { get; set; }
        /// <summary>
        /// The information about the location selected from the Google Map.
        /// </summary>
        public MapLocationInfo MapLocation { get; set; }
        public static PostOutput CreateFrom(string id, PostActivityInput input)
        {
            return new PostOutput
            {
                UniqueId = id,
                ExperienceId = input.ExperienceId,
                PhotoVideoId = input.PhotoVideoId,
                ChildrenIds = input.ChildrenIds,
                Description = input.Description,
                SourceType = input.SourceType,
                // ResourceURL = input.ResourceURL,
                ResourceLocalIdentifier = input.ResourceLocalIdentifier,
                IsPlanning = input.IsPlanning
            };
        }
    }

    /// <summary>
    /// Represents a created (recorded) check-in activity.
    /// </summary>
    [Serializable]
    public class CheckInOutput : ActivityOutput<CheckInOutput>, IActivityOutput
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
        
        public static CheckInOutput CreateFrom(string id, CheckInInput input)
        {
            return new CheckInOutput
            {
                UniqueId = id,
                ExperienceId = input.ExperienceId,
                IsPlanning = input.IsPlanning
            };
        }
    }

    /// <summary>
    /// Represents a created (recorded) photo activity.
    /// </summary>
    [Serializable]
    public class PhotoOutput : ActivityOutput<PhotoOutput>, IActivityOutput
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
        /// The list of user IDs of the tagged SB users.
        /// </summary>
        /// <remarks>
        /// This list will be populated every time the user tag a user by typing with the "@" character.
        /// </remarks>
        public IEnumerable<string> TaggedFriends { get; set; }
        /// <summary>
        /// The list of words that will be used to auto-validate the submitted photos
        /// when this activity is consumed.
        /// </summary>
        public IEnumerable<string> Keywords { get; set; }
        /// <summary>
        /// The list of additional words that can be used to further validate the submitted photos
        /// when this activity is consumed.
        /// </summary>
        public IEnumerable<string> AlternateKeywords { get; set; }
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
        
        public static PhotoOutput CreateFrom(string id, PhotoActivityInput input)
        {
            return new PhotoOutput
            {
                UniqueId = id,
                ExperienceId = input.ExperienceId,
                // SamplePhotos = input.SamplePhotos,
                ResourceLocalIdentifiers = input.ResourceLocalIdentifiers,
                TaggedFriends = input.TaggedFriends,
                Keywords = input.Keywords,
                AlternateKeywords = input.AlternateKeywords,
                IsPlanning = input.IsPlanning
            };
        }
    }

    /// <summary>
    /// Represents a created (recorded) video activity.
    /// </summary>
    [Serializable]
    public class VideoOutput : ActivityOutput<VideoOutput>, IActivityOutput
    {
        // <summary>
        /// The type of this activity output.
        /// </summary>
        public new ActivityType Type
        {
            get
            {
                return ActivityType.Video;
            }
        }
        /// <summary>
        /// The preview image for the video.
        /// </summary>
        public string Thumbnail { get; set; }
        /// <summary>
        /// This streaming URL of the video
        /// </summary>
        /// <remarks>       
        /// </remarks>
        public string Preview { get; set; }
        /// <summary>
        /// An identifier which persistently identifier the object on a given device.
        /// </summary>
        /// <remarks>
        /// We will use this to try and load resources from the user's device, if present.
        /// If it fails, then the resource will be loaded via the <see cref="ResourceURL"/>.
        /// </remarks>
        public string ResourceLocalIdentifier { get; set; }
        
        public static VideoOutput CreateFrom(string id, VideoActivityInput input)
        {
            return new VideoOutput
            {
                UniqueId = id,
                ExperienceId = input.ExperienceId,
                // Preview = input.SampleVideo,
                ResourceLocalIdentifier = input.ResourceLocalIdentifier,
                IsPlanning = input.IsPlanning
            };
        }
    }

    /// <summary>
    /// Represents a created (recorded) audio activity.
    /// </summary>
    [Serializable]
    public class AudioOutput : ActivityOutput<AudioOutput>, IActivityOutput
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
        /// <summary>
        /// The URL of the sample audio.
        /// </summary>
        /// <remarks>       
        /// </remarks>
        public string SampleAudio { get; set; }
        /// <summary>
        /// An identifier which persistently identifier the object on a given device.
        /// </summary>
        /// <remarks>
        /// We will use this to try and load resources from the user's device, if present.
        /// If it fails, then the resource will be loaded via the <see cref="ResourceURL"/>.
        /// </remarks>
        public string ResourceLocalIdentifier { get; set; }
        
        public static AudioOutput CreateFrom(string id, AudioActivityInput input)
        {
            return new AudioOutput
            {
                UniqueId = id,
                ExperienceId = input.ExperienceId,
                // SampleAudio = input.SampleAudio,
                ResourceLocalIdentifier = input.ResourceLocalIdentifier,
                IsPlanning = input.IsPlanning
            };
        }
        
    }
}
