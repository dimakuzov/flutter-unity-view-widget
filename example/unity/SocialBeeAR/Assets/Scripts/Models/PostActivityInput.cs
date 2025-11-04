using System;
using System.Collections.Generic;
using Niantic.Lightship.AR.PersistentAnchors;

namespace SocialBeeAR
{
    [Serializable]
    public class PostActivityInput : ActivityInput
    {
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
        /// The URL of the thumbnail that will be used for relocalization.
        /// </summary>
        public string RelocalizationThumbnail { get; set; }
        /// <summary>
        /// The list of user IDs of the tagged SB users.
        /// </summary>
        /// <remarks>
        /// This list will be populated every time the user tag a user by typing with the "@" character.
        /// </remarks>
        public IEnumerable<string> TaggedFriends { get; set; }
        /// <summary>
        /// The information about the location selected from the Google Map.
        /// </summary>
        public MapLocationInfo MapLocation { get; set; }
        /// <summary>
        /// Validates the model.
        /// </summary>
        public new void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Text))
                throw new ArgumentException("The post title cannot be null or empty.");
            if (ResourceURL.IsNullOrWhiteSpace() && (SourceType == PostType.ExternalImage ||
                SourceType == PostType.ExternalVideo || SourceType == PostType.Video ||
                SourceType == PostType.Image))
            {
                throw new ArgumentException($"ResourceURL cannot be null or empty is Type is {SourceType}.");
            }
        }
        public override string ToString()
        {
            var val = $"{base.ToString()} | Description={Description} | SourceType={SourceType} | PhotoVideoId={PhotoVideoId} | ResourceLocalIdentifier={ResourceLocalIdentifier} | ResourceURL={ResourceURL}";

            if (MapLocation == null)
                return $"{val} | MapLocation is null";

            val += $" | MapLocation.Name={MapLocation.Name} | MapLocation.PlaceId={MapLocation.PlaceId} | MapLocation.FormattedAddress={MapLocation.FormattedAddress} | MapLocation.Map3DLocalIdentifier={MapLocation.Map3DLocalIdentifier}";

            return val;
        }
        /// <summary>
        /// Creates an instance of this class from an instance of <see cref="PostActivityInfo"/>.
        /// </summary>
        /// <param name="info">The information about the post.</param>
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="collectionId">The Id of the collection.</param>
        /// <param name="location">The user's current location.</param>
        /// <param name="isPlanning"></param>
        /// <returns></returns>
        /// <remarks>
        /// It's not a responsibility of this class to validate the PostActivityInfo instance.
        /// Also, we want to lessen the dependency of this class
        /// so we will not call any singleton classes from this method.
        /// </remarks>        
        public static PostActivityInput CreateFrom(PostActivityInfo info, string experienceId, string collectionId, Location location, bool isPlanning = false)
        {
            MessageManager.Instance.DebugMessage($"SBActivity.SubmitPost - CreateFrom: {info}");
            return new PostActivityInput
            {
                Text = info.Title,
                ExperienceId = experienceId,
                BucketId = collectionId,
                Location = location,
                ARInfo = ARDefinition.CreateFrom(info.Pose),

                Description = info.Description,
                SourceType = info.SourceType,
                RelocalizationThumbnail = info.RelocalizationThumbnail,
                IsPlanning = isPlanning,
                MapLocation = info.MapLocation,
                PlacenoteMapID = info.MapId,
                AnchorPayload = info.AnchorPayload
            };
        }
    }

    /// <summary>
    /// The type of content that was included in a post activity.
    /// </summary>
    /// <remarks>
    /// Do not change the order of this enum: Image, Link, Video, ExternalImage, ExternalVideo, and Text.
    /// </remarks>
    public enum PostType
    {
        /// <summary>
        /// The image is located in our own BLOB storage.
        /// </summary>
        Image,
        /// <summary>
        /// The post activity contains a link to a website (i.e. a page, article, etc.)
        /// </summary>
        Link,
        /// <summary>
        /// The video that is submitted to our own BLOB storage.
        /// </summary>
        Video,
        /// <summary>
        /// The image is located outside of our domain.
        /// </summary>        
        ExternalImage,
        /// <summary>
        /// The video is hosted outside of our domain. This is mostly YouTube videos
        /// but can also be from any other video streaming services.
        /// </summary>
        ExternalVideo,
        /// <summary>
        /// The activity only contains text.
        /// </summary>
        Text
    }
}
