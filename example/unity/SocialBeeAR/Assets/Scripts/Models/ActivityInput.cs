using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SocialBeeAR
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRestSerializable
    {
        /// <summary>
        /// Converts this instance to a <see cref="StringContent"/>.
        /// </summary>        
        StringContent ToStringContent();
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public abstract class RestSerializable : IRestSerializable
    {
        /// <summary>
        /// Converts this instance to a <see cref="StringContent"/>.
        /// </summary>
        /// <returns></returns>
        public StringContent ToStringContent()
        {
            return new StringContent(JsonConvert.SerializeObject(this,
                new JsonSerializerSettings {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }), Encoding.UTF8, "application/json");
        }
    }

    /// <summary>
    /// The base definition for creating activities.
    /// </summary>
    public interface IActivityInput : IRestSerializable
    {
        /// <summary>
        /// The Id of the experience that this activity belongs to.
        /// </summary>
        string ExperienceId { get; set; }
        /// <summary>
        /// The Id of the parent activity.
        /// </summary>
        string ParentId { get; set; }
        /// <summary>
        /// The Id of the cloud map where this activity is saved.
        /// </summary>
        string PlacenoteMapID { get; set; }
        /// <summary>
        /// The Niantic-specific information of the anchor to which this post belongs to.
        /// </summary>
        string AnchorPayload { get; set; }
        /// <summary>
        ///  The Id of the anchor in the AR session.
        /// </summary>
        string ARAnchorId { get; set; }
        /// <summary>
        /// The title or text of the activity.
        /// For a Post, this is the title.
        /// For a Trivia, this is the question.
        /// For a Photo or Video, this is the caption.
        /// For a Check-In, this can be the caption or the location name.
        /// For an Audio, we can use the date creation.
        /// </summary>
        string Text { get; set; }
        /// <summary>
        /// The location where the activity was created.
        /// </summary>
        Location Location { get; set; }
        /// <summary>
        /// The AR information of the activity.
        /// </summary>
        ARDefinition ARInfo { get; set; }
        /// <summary>
        /// If the activity is created in planning mode.
        /// </summary>
        bool IsPlanning { get; set; }
        /// <summary>
        /// Validates if the input has the minimum required values: ExperienceId, Text, and Location.
        /// </summary>
        void Validate();        
    }

    /// <summary>
    /// The base class for creating activities whose member fields are used to pass params to the API.
    /// </summary>
    [Serializable]
    public abstract class ActivityInput : RestSerializable, IActivityInput
    {
        /// <summary>
        /// If the activity is created in planning mode.
        /// </summary>
        public bool IsPlanning { get; set; }
        /// <summary>
        /// The Id of the experience that this activity belongs to.
        /// </summary>
        public string ExperienceId { get; set; }
        /// <summary>
        /// The Id of the collection that this activity belongs to.
        /// </summary>
        public string BucketId { get; set; }
        /// <summary>
        /// The Id of the parent activity.
        /// </summary>
        public string ParentId { get; set; }
        /// <summary>
        /// The Id of the cloud map (or the VPS Location) where this activity is saved.
        /// </summary>
        public string PlacenoteMapID { get; set; }
        /// <summary>
        /// The Niantic-specific information of the anchor to which this post belongs to.
        /// </summary>
        public string AnchorPayload { get; set; }
        /// <summary>
        ///  The Id of the anchor in the AR session.
        /// </summary>
        public string ARAnchorId { get; set; }
        /// <summary>
        /// The title or text of the activity.
        /// For a Post, this is the title.
        /// For a Trivia, this is the question.
        /// For a Photo or Video, this is the caption.
        /// For a Check-In, this is the location name.
        /// For an Audio, we can use the date creation.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The location where the activity was created.
        /// </summary>
        public Location Location { get; set; }
        /// <summary>
        /// The AR information of the activity.
        /// </summary>
        public ARDefinition ARInfo { get; set; }
        /// <summary>
        /// The sort order of the activity. This controls the arrangement of activities on the UI.
        /// </summary>
        /// <remarks>
        /// We should just use the Ticks value of the current date+time.
        /// </remarks>
        public long SortOrder { get; set; }       
        /// <summary>
        /// Validates if the input has the minimum required values: ExperienceId, Text, and Location.
        /// </summary>
        /// <remarks>
        /// Let's throw an error to inform/remind devs of the required and valid params.        
        /// </remarks>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ExperienceId))
                throw new ArgumentException("ExperienceId cannot be null or empty.");

            if (Location == null || !Location.IsValid())
            {
                // Let's try and reassign the location first.
                Location = SBContextManager.Instance.context.GetProperLocation();
            }

            if (Location == null)
                throw new ArgumentException("Location cannot be null or empty.");

            if (Location.Latitude == 0 && Location.Longitude == 0)
                throw new ArgumentException("Location has an invalid coordinates: 0,0");
            //throw new ArgumentException("Location should have: a name, city, state and the lat and lon cannot be both zero.");

            if (Location.Name.IsNullOrWhiteSpace() || Location.City.IsNullOrWhiteSpace() || Location.State.IsNullOrWhiteSpace())
                MessageManager.Instance.DebugMessage($"Location: {Location}");            
        }

        public override string ToString()
        {
            var val = $"IsPlanning={IsPlanning} | Text={Text} | ExperienceId={ExperienceId} | ParentId={ParentId} | CollectionId={BucketId} | MapID={PlacenoteMapID} | AnchorPayload={AnchorPayload}";

            if (Location != null)
            {
                val += $@" | Latitude={Location.Latitude} | Longitude={Location.Longitude} | City={Location.City}
| State = {Location.State} | Name={Location.Name}";
            }

            return val;
        }
        
        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Create<T>(string json) where T : ActivityInput
        {
            try
            {
                var error = JsonConvert.DeserializeObject<T>(json);

                return error;
            }
            catch
            {
                // ToDo: let's log this error and notify ourselves.
                MessageManager.Instance.DebugMessage($"Cannot deserialize json to create an instance of {typeof(T).ToString()}.");
                return null;
            }
        }
    }
}