using System;
using Newtonsoft.Json;

namespace SocialBeeAR
{
    public interface IConsumeActivityInput : IRestSerializable
    {
        /// <summary>
        /// The Id of the activity.
        /// </summary>
        string ActivityId { get; set; }
        /// <summary>
        /// The Id of the experience that this activity belongs to.
        /// </summary>
        string ExperienceId { get; set; }
        /// <summary>
        /// Validates if the input has the minimum required values: ExperienceId.
        /// </summary>
        void Validate();
    }

    /// <summary>
    /// The base class for consuming activities.
    /// </summary>
    [Serializable]
    public abstract class ConsumeActivityInput : RestSerializable, IConsumeActivityInput
    {
        /// <summary>
        /// The Id of the activity.
        /// </summary>
        [JsonIgnore]
        public string ActivityId { get; set; }
        /// <summary>
        /// The Id of the experience that this activity belongs to.
        /// </summary>
        public string ExperienceId { get; set; }
        /// <summary>
        /// Validates if the input has the minimum required values: ExperienceId.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ExperienceId))
                throw new ArgumentException("ExperienceId cannot be null or empty.");
        }

        public override string ToString()
        {
            var info = $"ActivityId={ActivityId} | ExperienceId={ExperienceId}";
 
            return info;
        }
    }
}