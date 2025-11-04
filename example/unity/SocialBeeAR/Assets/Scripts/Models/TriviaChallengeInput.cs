using System;
using System.Collections.Generic;

namespace SocialBeeAR
{
    [Serializable]
    public class TriviaChallengeInput : ActivityInput
    {
        public TriviaChallengeInput()
        {
            Options = new List<string>();
            Hints = new List<string>();
        }

        public IList<string> Options { get; set; }
        public int Answer { get; set; }
        public IList<string> Hints { get; set; }

        public new void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Text))
                throw new ArgumentException("The trivia question cannot be null or empty.");
            if (Options.Count < 3)
                throw new ArgumentException("The number of Options cannot be less than three,");
            if (Answer < 0 || Answer > Options.Count - 1)
                throw new ArgumentException("Invalid value for Answer: index out of bound.");
        }

        public override string ToString()
        {
            var val = base.ToString();

            val += $" | Options={string.Join(",", Options)} | Answer={Answer} | Hints={string.Join(",", Hints)}";

            return val;
        }
        /// <summary>
        /// Creates an instance of this class from an instance of <see cref="TriviaActivityInfo"/>.
        /// </summary>
        /// <param name="info">The information about the trivia.</param>
        /// <param name="experienceId">The Id of the experience.</param>
        /// <param name="location">The user's current location.</param>
        /// <returns></returns>
        /// <remarks>
        /// It's not a responsibility of this class to validate the PostActivityInfo instance.
        /// Also, we want to lessen the dependency of this class
        /// so we will not call any singleton classes from this method.
        /// </remarks>        
        public static TriviaChallengeInput CreateFrom(TriviaActivityInfo info, string experienceId, string collectionId, Location location, bool isPlanning = false)
        {            
            return new TriviaChallengeInput
            {
                Text = info.Title,
                ExperienceId = experienceId,
                BucketId = collectionId,
                Location = location,
                ARInfo = ARDefinition.CreateFrom(info.Pose),

                ParentId = info.ParentId,
                PlacenoteMapID = info.MapId,
                AnchorPayload = info.AnchorPayload,
                
                Options = info.OptionList,
                Answer = info.AnswerIndex,
                Hints = info.Hints,
                IsPlanning = isPlanning
            };
        }
    }
}
