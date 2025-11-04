using System;
using System.Collections.Generic;
using Niantic.Lightship.AR.PersistentAnchors;

namespace SocialBeeAR
{
    public class PostActivityInfo : ActivityInfo
    {
        /// <summary>
        /// The image used for relocalization.
        /// </summary>
        public string RelocalizationThumbnail { get; set; }
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
        /// The
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type of created content, i.e. a link to a video, photo or a website page.
        /// </summary>
        public PostType SourceType { get; set; }
        /// <summary>
        /// True, if the activity has a check-in only. No other type of activities are (should be) present.
        /// </summary>
        public bool HasCheckIn { get; set; }
        /// <summary>
        /// The information about the location selected from the Google Map.
        /// </summary>
        public MapLocationInfo MapLocation { get; set; }
        /// <summary>
        /// ctor
        /// </summary>
        public PostActivityInfo()
        {
            //SetType(ActivityType.Post);
            Type = ActivityType.Post;
        }

        public override string ToString()
        {
            var info = $"{base.ToString()} | SourceType={SourceType} | Description={Description}";

            if (MapLocation != null)
            {
                info += $" | MapLocation.Name={MapLocation.Name} | MapLocation.PlaceId={MapLocation.PlaceId} | MapLocation.FormattedAddress={MapLocation.FormattedAddress} | MapLocation.Map3DLocalIdentifier={MapLocation.Map3DLocalIdentifier}";
            }

            return info;
        }
        public override IActivityInfo Clone()
        {
            // Let's not do the MemoryStream technique + serialization and deserialization
            // as it's very expensive and unnecessary.
            return new PostActivityInfo
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
                SourceType = SourceType,
                Description = Description,
                Status = Status,
                HasCheckIn = HasCheckIn,
                RelocalizationThumbnail = RelocalizationThumbnail
            };
        }
    }
}
