namespace SocialBeeAR
{
    public class AudioActivityInfo : ActivityInfo
    {
        public AudioActivityInfo()
        {
            Type = ActivityType.Audio;
        }
        /// <summary>
        /// The local path of the audio.
        /// </summary>
        public string ContentPath { get; set; }
        /// <summary>
        /// The URL of the audio. This is the location of the <see cref="ContentPath"/>
        /// once uploaded to Social Bee's blob storage.
        /// </summary>
        public string ContentURL { get; set; }
            
        public bool IsContentInLocalPath
        {
            get { return !ContentPath.IsNullOrWhiteSpace(); }
        }
         
        public override IActivityInfo Clone()
        {
            // Let's not do the MemoryStream technique + serialization and deserialization
            // as it's very expensive and unnecessary.
            var info = new AudioActivityInfo
            {
                Id = Id,
                ExperienceId = ExperienceId,
                Title = Title,
                ParentId = ParentId,
                MapId = MapId,
                AnchorPayload = AnchorPayload,
                Points = Points,
                Pose = Pose,
                IsEditing = IsEditing,
                ContentPath = ContentPath,
                ContentURL = ContentURL,                
                Status = Status,                
            };
            info.SetIsChallenge(false);

            return info;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | contentPath={ContentPath} | contentURL={ContentURL}";
        }
         
        /// <summary>
        /// Get the proper Id for uploading the audio to the blob storage.
        /// </summary>
        /// <returns></returns>
        public string GetBlobIdForUpload()
        {
            // The Id is the combination of the acitivity Id + the Id of the currently logged-in user
            return $"{Id}{SBContextManager.Instance.context.userId}";
        }
    }
}