using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocialBeeAR
{
    public class ActivityDto
    {
        public string id { get; set; }
        public string text { get; set; }
        public string description { get; set; }
        public bool isChallenge { get; set; }
        public bool isCheckInOnly { get; set; }
        public int type { get; set; }        
        public int points { get; set; }
        public string parentId { get; set; }
        public string options { get; set; }
        public int answer { get; set; }
        public string hints { get; set; }
        public int sourceType { get; set; }
        /// The URL of the photo or the video either on our server or on external servers.
        public string resourceURL { get; set; }
        /// The local path of the resource in the user's device. This can be empty.
        public string localResourceIdentifier { get; set; }
        /// If this activity is a video, then this is the thumbnail of that video.
        public string thumbnail { get; set; }

        public int status { get; set; }
        public int pointsEarned { get; set; }
        public string completedId { get; set; }
        public DateTime dateCompleted { get; set; }

        public IEnumerable<string> triviaOptions
        {
            get
            {
                if (options.IsNullOrWhiteSpace()) return new List<string>();

                return options.Split(',');
            }
        }

        public IEnumerable<string> triviaHints
        {
            get
            {
                if (hints.IsNullOrWhiteSpace()) return new List<string>();

                return hints.Split(',');
            }
        }

        public string keywords { get; set; }
        public IEnumerable<string> imageKeywords
        {
            get
            {
                //MessageManager.Instance.DebugMessage($"keywords={alternateKeywords}");
                if (keywords.IsNullOrWhiteSpace()) return new List<string>();

                return keywords.Split(',');
            }
        }

        public string alternateKeywords { get; set; }
        public IEnumerable<string> imageAlternateKeywords
        {
            get
            {
                //MessageManager.Instance.DebugMessage($"alternateKeywords={alternateKeywords}"); 
                if (alternateKeywords.IsNullOrWhiteSpace()) return new List<string>();

                return alternateKeywords.Split(',');
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="ActivityInfo"/> from this instance.
        /// </summary>
        /// <param name="anchorPose"></param>
        /// <remarks>
        /// It's a DTO and should not have a behavior but let's do the mapping here anyway.
        /// </remarks>
        /// <returns></returns>
        public IActivityInfo ToActivityInfo(Pose anchorPose, string experienceId, string mapId)
        {
            if (anchorPose == null)
                throw new ArgumentNullException("anchorPose cannot be null or empty.");

            var activityType = (ActivityType)type;
            if (activityType == ActivityType.Trivia)
            {
                return new TriviaActivityInfo
                {
                    Id = id,
                    Pose = anchorPose,
                    Points = points,
                    ParentId = parentId,                    
                    ExperienceId = experienceId,
                    MapId = mapId,

                    Title = text,
                    OptionList = triviaOptions.ToList(),
                    Hints = triviaHints.ToList(),
                    AnswerIndex = answer,
                    UserAnswerIndex = answer,

                    Status = (ActivityStatus)status,
                    PointsEarned = pointsEarned,
                    CompletedId = completedId,
                    DateCompleted = dateCompleted
                };
            }

            if (activityType == ActivityType.PhotoVideo || activityType == ActivityType.Video)
            {
                return new PhotoVideoActivityInfo
                {
                    Id = id,
                    Pose = anchorPose,
                    Points = points,
                    ParentId = parentId,                    
                    ExperienceId = experienceId,
                    MapId = mapId,

                    Title = text,
                    ContentPath = localResourceIdentifier,
                    ContentURL = resourceURL,
                    Thumbnail = thumbnail,
                    Keywords = imageKeywords,
                    AlternateKeywords = imageAlternateKeywords,
                    IsVideo = activityType == ActivityType.Video,

                    Status = (ActivityStatus)status,
                    PointsEarned = pointsEarned,
                    CompletedId = completedId,
                    DateCompleted = dateCompleted
                }.MarkAsAChallenge(isChallenge);
            }

            if (activityType == ActivityType.Post)
            {
                return new PostActivityInfo
                {
                    Id = id,
                    Pose = anchorPose,
                    Points = points,
                    ParentId = parentId,                    
                    ExperienceId = experienceId,
                    MapId = mapId,

                    Title = text,
                    Description = description,
                    SourceType = (PostType)sourceType,
                    HasCheckIn = isCheckInOnly,

                    Status = (ActivityStatus)status,
                    PointsEarned = pointsEarned,
                    CompletedId = completedId,
                    DateCompleted = dateCompleted
                };
            }

            if (activityType == ActivityType.Audio)
            {
                return new AudioActivityInfo
                {
                    Id = id,
                    Pose = anchorPose,
                    Points = points,
                    ParentId = parentId,                    
                    ExperienceId = experienceId,
                    MapId = mapId,

                    Title = text,
                    ContentPath = localResourceIdentifier,
                    ContentURL = resourceURL,

                    Status = (ActivityStatus)status,
                    PointsEarned = pointsEarned,
                    CompletedId = completedId,
                    DateCompleted = dateCompleted
                };
            }

            return null;
        }

    }
}