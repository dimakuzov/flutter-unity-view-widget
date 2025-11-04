using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialBeeAR
{
    /// <summary>
    /// The input model for consuming a photo activity POI (not a challenge).
    /// </summary>
    [Serializable]
    public class PhotoPoiConsumeInput : ConsumeActivityInput    
    {
        public static PhotoPoiConsumeInput CreateFrom(string experienceId)
        {
            return new PhotoPoiConsumeInput
            {
                ExperienceId = experienceId,                
            };
        }
    }

    /// <summary>
    /// The input model for completing a photo challenge.
    /// </summary>
    [Serializable]
    public class PhotoChallengeConsumeInput : ConsumeActivityInput
    {
        public PhotoChallengeConsumeInput()
        {
            Keywords = new List<string>();
        }
        /// <summary>
        /// The caption of the photo.
        /// </summary>
        public string Caption { get; set; }
        /// <summary>
        /// If true, the activity will be marked as completed but with a wrong submission.
        /// </summary>
        public bool TakeLoss { get; set; }
        /// <summary>
        /// If true, the completed activity will be submitted for review.
        /// </summary>
        /// <remarks>
        /// This can be set to true if the keywords of the submitted photo
        /// does not match the keywords of the photo that needs to be completed.
        /// </remarks>
        public bool ForReview { get; set; }
        /// <summary>
        /// This should be the same extension name of the photo that is being completed.
        /// This will be used on the server to validate that the submitted photo exists on our server.
        /// </summary>
        public string Extension { get; set; }
        /// <summary>
        /// The keywords that are generated for the photo being submitted.
        /// </summary>
        public IEnumerable<string> Keywords { get; set; }
        /// <summary>
        /// Validates the model.
        /// </summary>
        public new void Validate()
        {
            base.Validate();

            if (!Keywords.Any())
            {
                throw new ArgumentException("Keywords cannot be null or empty.");
            }           
        }

        public static PhotoChallengeConsumeInput CreateFrom(PhotoVideoActivityInfo info, string experienceId)
        {
            return new PhotoChallengeConsumeInput
            {                
                ExperienceId = experienceId,
                Caption = info.Title,
                Extension = info.GetContentExtensionName(),
                Keywords = info.Keywords
            };
        }

        public override string ToString()
        {
            return $"ActivityId={ActivityId} | Extension={Extension} | ExperienceId={ExperienceId}";
        }
    }

    /// <summary>
    /// The input model for consuming a trivia challenge.
    /// </summary>
    [Serializable]
    public class TriviaConsumeInput : ConsumeActivityInput
    {
        /// <summary>
        /// The index of the selected answer.
        /// </summary>
        public int Answer { get; set; }
        /// <summary>
        /// The number of hints the user had used before answering the trivia.
        /// </summary>
        public int Hints { get; set; }
        /// <summary>
        /// Create an instance of <see cref="TriviaConsumeInput"/>.
        /// </summary>
        /// <param name="activityId">The Id of the activity being completed.</param>
        /// <param name="experienceId">The Id of the experience where the activity belongs to.</param>
        /// <param name="answer">The index of the selected answer.</param>
        /// <param name="hints">The number of hints the user had used before answering the trivia.</param>
        /// <returns></returns>
        public static TriviaConsumeInput CreateFrom(string activityId, string experienceId, int answer, int hints)
        {
            return new TriviaConsumeInput
            {
                ActivityId = activityId,
                ExperienceId = experienceId,
                Answer = answer,
                Hints = hints
            };
        }
    }
}
