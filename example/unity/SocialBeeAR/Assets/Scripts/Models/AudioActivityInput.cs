using System;

namespace SocialBeeAR
{
    [Serializable]
    public class AudioActivityInput : ActivityInput
    {
        /// <summary>
        /// ctor
        /// </summary>
        public AudioActivityInput()
        {            
            SortOrder = DateTime.UtcNow.Ticks;
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
        /// </remarks>
        public string ResourceLocalIdentifier { get; set; }          
        /// <summary>
        /// Validates the model.
        /// </summary>
        public new void Validate()
        {
            base.Validate();

            if (SampleAudio.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("SampleAudio cannot be null or empty.");
            }             
        }

        public override string ToString()
        {
            var val = base.ToString();

            val += $" | SampleAudio={SampleAudio}";

            return val;
        }
        /// <summary>
        /// Creates an instance of this class from an instance of <see cref="AudioActivityInfo"/>.
        /// </summary>
        /// <param name="info">The information about the photo/videp.</param>
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="location">The user's current location.</param>
        /// <returns></returns>
        /// <remarks>
        /// It's not a responsibility of this class to validate the AudioActivityInfo instance.
        /// Also, we want to lessen the dependency of this class
        /// so we will not call any singleton classes from this method.
        /// </remarks>        
        public static AudioActivityInput CreateFrom(AudioActivityInfo info, string experienceId, string collectionId, Location location, bool isPlanning = false)
        {
            return new AudioActivityInput
            {
                Text = info.Title,
                ExperienceId = experienceId,
                BucketId = collectionId,
                Location = location,
                ARInfo = ARDefinition.CreateFrom(info.Pose),

                ParentId = info.ParentId,
                PlacenoteMapID = info.MapId,
                AnchorPayload = info.AnchorPayload,
                
                SampleAudio = info.ContentURL,
                ResourceLocalIdentifier = info.ContentPath,
                IsPlanning = isPlanning
            };
        }
    }     
}

