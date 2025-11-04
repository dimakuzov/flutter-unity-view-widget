
using System.Collections.Generic;
using System.Linq;

namespace SocialBeeAR
{
    public class PhotoVideoActivityInfo : ActivityInfo
    {        
        /// <summary>
        /// If true then the activity was created from the Quick Create button.
        /// </summary>
        public bool IsQuickPhotoVideo { get; set; }
        /// <summary>
        /// The local path of the photo or video.
        /// </summary>
        public string ContentPath { get; set; }
        /// <summary>
        /// If this is a video, then this is the thumbnail for it.
        /// </summary>
        public string Thumbnail { get; set; }
        /// <summary>
        /// The URL of the photo or video. This is the location of the <see cref="ContentPath"/>
        /// once uploaded to Social Bee's blob storage.
        /// </summary>
        public string ContentURL { get; set; }
        /// <summary>
        /// If this is a photo, this is the selected validation keywords.
        /// </summary>
        public IEnumerable<string> Keywords { get; set; }
        /// <summary>
        /// If this is a photo, this is all the validation keywords for the photo.
        /// </summary>
        public IEnumerable<string> AlternateKeywords { get; set; }
        /// <summary>
        /// Determines if this activity is a photo or a video.
        /// </summary>
        public bool IsVideo { get; set; }
        /// <summary>
        /// Helper property that returns the <see cref="ContentURL"/> when <see cref="ContentPath"/> is empty.
        /// </summary>
        public string UrlToUseForValidation
        {
            get
            {
                return ContentPath.IsNullOrWhiteSpace() ? ContentURL : ContentPath;
            }
        }        
        /// <summary>
        /// Helper method for returning the keywords that can be used for selection.
        /// </summary>
        public IEnumerable<string> KeywordsForSelection
        {
            get
            {
                return AlternateKeywords.Any() ? AlternateKeywords : Keywords;
            }
        }

        public bool IsContentInLocalPath
        {
            get { return !ContentPath.IsNullOrWhiteSpace(); }
        }
        public PhotoVideoActivityInfo()
        {
            Keywords = new List<string>();
            AlternateKeywords = new List<string>();
            Type = ActivityType.PhotoVideo;
        }
        public PhotoVideoActivityInfo MarkAsAChallenge(bool flag)
        {
            SetIsChallenge(flag);
            return this;
        }
        public override IActivityInfo Clone()
        {
            // Let's not do the MemoryStream technique + serialization and deserialization
            // as it's very expensive and unnecessary.
            var info = new PhotoVideoActivityInfo
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
                Keywords = Keywords == null ? new List<string>() : new List<string>(Keywords),                
                AlternateKeywords = AlternateKeywords == null ? new List<string>() : new List<string>(AlternateKeywords),
                Status = Status,
                Thumbnail = Thumbnail,
                IsQuickPhotoVideo = IsQuickPhotoVideo
            };
            info.SetIsChallenge(IsChallenge);

            return info;
        }

        public override string ToString()
        {
            return $"{base.ToString()} | keywords={string.Join(",",Keywords)} | content localPath={ContentPath} | content publicURL={ContentURL} | thumbnail={Thumbnail} | altKeywords={string.Join(",", AlternateKeywords)} | IsVideo={IsVideo}";
        }

        public string GetContentExtensionName()
        {            
            var parts = UrlToUseForValidation.Split('.');
            if (parts.Length < 2)
                return "jpeg";

            return $".{parts[1]}";
        }

        /// <summary>
        /// Get the proper Id for uploading the photo/video to the blob storage.
        /// </summary>
        /// <returns></returns>
        public string GetBlobIdForUpload()
        {
            // The Id is the combination of the acitivity Id + the Id of the currently logged-in user
            return $"{Id}{SBContextManager.Instance.context.userId}";
        }
    }
}