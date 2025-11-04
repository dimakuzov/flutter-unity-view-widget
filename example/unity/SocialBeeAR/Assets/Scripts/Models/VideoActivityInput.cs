using System;

namespace SocialBeeAR
{
    [Serializable]
    public class VideoActivityInput : ActivityInput
    {
        /// <summary>
        /// ctor
        /// </summary>
        public VideoActivityInput()
        {
            //TaggedFriends = new List<string>();            
            SortOrder = DateTime.UtcNow.Ticks;
        }

        // <summary>
        /// The URL of the sample video.
        /// </summary>        
        /// </remarks>
        public string SampleVideo { get; set; }
        /// <summary>
        /// An identifier which persistently identifier the object on a given device.
        /// </summary>
        /// <remarks>
        /// We will use this to try and load resources from the user's device, if present.
        /// If it fails, then the resource will be loaded via the <see cref="ResourceURL"/>.
        /// </remarks>
        public string ResourceLocalIdentifier { get; set; }
        /// <summary>
        /// Validates the model.
        /// </summary>
        public new void Validate()
        {
            base.Validate();

            if (SampleVideo.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("SampleVideo cannot be null or empty.");
            }
        }

        public override string ToString()
        {
            var val = base.ToString();

            val += $" | SampleVideo={SampleVideo}";

            return val;
        }
        /// <summary>
        /// Creates an instance of this class from an instance of <see cref="PhotoVideoActivityInfo"/>.
        /// </summary>
        /// <param name="info">The information about the photo/videp.</param>
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="location">The user's current location.</param>
        /// <returns></returns>
        /// <remarks>
        /// It's not a responsibility of this class to validate the PhotoVideoActivityInfo instance.
        /// Also, we want to lessen the dependency of this class
        /// so we will not call any singleton classes from this method.
        /// </remarks>        
        public static VideoActivityInput CreateFrom(PhotoVideoActivityInfo info, string experienceId, string collectionId, Location location, bool isPlanning = false)
        {
            MessageManager.Instance.DebugMessage($"SBActivity.SubmitVideo: submitting a Post with ID = {info.Id}");
            return new VideoActivityInput
            {
                Text = info.Title,
                ExperienceId = experienceId,
                BucketId = collectionId,
                Location = location,
                ARInfo = ARDefinition.CreateFrom(info.Pose),

                ParentId = info.ParentId,
                PlacenoteMapID = info.MapId,
                AnchorPayload = info.AnchorPayload,
                
                ResourceLocalIdentifier = info.ContentPath,
                SampleVideo = info.ContentURL,
                IsPlanning = isPlanning
            };
        }
    }
}